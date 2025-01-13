using Capitalead.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace Capitalead.Services;

public class DbFilesExporter(
    ILogger<DbFilesExporter> logger,
    AppDatabase database,
    IConfiguration configuration)
{
    public async Task CreateFiles()
    {
        logger.LogInformation("Started Create db files script...");

        logger.LogInformation("Collect files from folder");
        var files = LoadFiles();
        logger.LogInformation("Collected files from folder");
        foreach (var file in files)
        {
            await database.DbFiles.AddAsync(file);
        }

        await database.SaveChangesAsync();
        logger.LogInformation("Saved files to database");

        logger.LogInformation("Successfully created db files!");
    }
    
    public IList<DbFile> LoadFiles()
    {
        var folder = configuration["db_files_store_folder"] ?? throw new ArgumentNullException("db_files_store_folder");
        var files = new List<DbFile>();
        foreach (var fileName in Directory.EnumerateFiles(folder))
        {
            var name = Path.GetFileName(fileName);
            if (name.StartsWith("~"))
                continue;
            files.Add(new DbFile()
            {
                Id = Guid.CreateVersion7(),
                FileName = name,
                Exported = false,
                Created = DateTime.UtcNow,
                ReadyForExport = false
            });
        }
        return files;
    }

    public async Task ExportFiles()
    {
            logger.LogInformation("Started export db files script...");
            var files = await database.DbFiles.Where(f => f.ReadyForExport && !f.Exported).ToListAsync();
            foreach (var file in files)
            {
                try
                {
                    var fileId = file.Id;
                    var saveFolder = configuration["db_files_store_folder"] ??
                                     throw new ArgumentNullException("db_files_store_folder");
                    var filePath = Path.Combine(saveFolder, file.FileName);
                    using var memoryStream = new MemoryStream();
                    await using (var fileStream = File.OpenRead(filePath))
                    {
                        await fileStream.CopyToAsync(memoryStream);
                    }

                    memoryStream.Position = 0;
                    using (var spreadsheetDocument = SpreadsheetDocument.Open(memoryStream, false))
                    {
                        var workbookPart = spreadsheetDocument.WorkbookPart ??
                                           throw new ArgumentException("spreadsheetDocument.WorkbookPart");
                        IEnumerable<Sheet>? sheets = workbookPart?.Workbook?.Sheets?.Elements<Sheet>();
                        if (sheets == null)
                        {
                            logger.LogInformation("File {FileName} sheets is empty!", file.FileName);
                            return;
                        }

                        List<string> sharedStrings = GetSharedString(workbookPart.SharedStringTablePart);
                        logger.LogInformation("sharedStrings loaded");
                        var cellFormats = workbookPart.WorkbookStylesPart.Stylesheet.CellFormats;
                        var numberingFormats = workbookPart.WorkbookStylesPart.Stylesheet.NumberingFormats;

                        foreach (var sheet in sheets)
                        {
                            var sheetName = GetSheetName(sheet);

                            var processed = await database.DbProspects
                                .Where(p => p.FileId == fileId && p.SheetName == sheetName)
                                .MaxAsync(p => (long?)p.RowNumber) ?? 0;
                            logger.LogInformation("Processing {SheetName} in file {FileName} from {Row}", sheetName,
                                file.FileName, processed);

                            var worksheetPart = workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart;

                            int currentRow = 1;
                            long count = 0;
                            List<string> headersList = new List<string>();
                            var data = new List<string?>();
                            bool skipRow = false;
                            var prospects = new List<DbProspect>();

                            //Open Stream
                            OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
                            while (reader.Read())
                            {
                                if (reader.ElementType == typeof(Row) && reader.IsStartElement)
                                {
                                    var newRow = Convert.ToInt32(reader.Attributes[0].Value);
                                    // Headers row
                                    if (newRow == 2 && currentRow == 1)
                                    {
                                        var cleanData = CleanList(data);
                                        if (cleanData.Count == 0)
                                        {
                                            logger.LogInformation("Row {CurrentRow} is empty. Finish sheet {SheetName}",
                                                currentRow, sheetName);
                                            break;
                                        }

                                        headersList.AddRange(cleanData);
                                        data.Clear();
                                        continue;
                                    }

                                    bool afterSkip = false;
                                    if (newRow > 1 && currentRow <= processed)
                                    {
                                        count++;
                                        skipRow = true;
                                        logger.LogInformation("Skip Row {CurrentRow}", currentRow);
                                        currentRow = newRow;
                                        data.Clear();
                                        continue;
                                    }
                                    else if (count > 0 &&
                                             (currentRow == processed + 1))
                                    {
                                        afterSkip = true;
                                        skipRow = false;
                                    }
                                    else
                                    {
                                        skipRow = false;
                                    }

                                    if (currentRow != newRow && afterSkip == false)
                                    {
                                        var cleanData = CleanList(data);
                                        if (cleanData.Count == 0)
                                        {
                                            logger.LogInformation("Row {CurrentRow} is empty. Finish sheet {SheetName}",
                                                currentRow, sheetName);
                                            break;
                                        }

                                        count++;
                                        var prospect = GetProspect(cleanData, file, sheetName, currentRow);
                                        prospects.Add(prospect);
                                        if (prospects.Count >= 5000)
                                        {
                                            logger.LogInformation(
                                                "Save batch for 5000 rows up to {CurrentRow} {SheetName} in file {FileName}",
                                                currentRow, sheetName, file.FileName);
                                            await SaveProspects(prospects);
                                            prospects.Clear();
                                        }

                                        data.Clear();
                                    }

                                    currentRow = newRow;
                                }
                                else if (reader.ElementType == typeof(Cell))
                                {
                                    //If a cell is empty, the cell node doesn't exist in XML.
                                    Cell cellValue = (Cell)reader.LoadCurrentElement(); //this skip the EndElement

                                    var text = GetCellValue(sharedStrings, cellFormats, numberingFormats, cellValue);

                                    data.Add(text);
                                }

                            }

                            reader.Close();
                            await SaveProspects(prospects);
                            logger.LogInformation("Processing {SheetName} in file {FileName} finished", sheetName,
                                file.FileName);

                        }
                    }

                    file.CompletedDate = DateTime.UtcNow;
                    file.Exported = true;
                    database.DbFiles.Update(file);
                    await database.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error while exporting db files for file {FIleName}", file.FileName);
                    throw;
                }
            }

            logger.LogInformation("Export db files script finished....");

        async Task SaveProspects(IList<DbProspect> prospects)
        {
            if (prospects.Count == 0)
                return;
            await database.DbProspects.AddRangeAsync(prospects);
            await database.SaveChangesAsync();
        }

        static DbProspect GetProspect(List<string> content, DbFile file, string sheetName, long rowNumber)
        {
            var dbProspect = new DbProspect()
            {
                Id = Guid.CreateVersion7(),
                FileId = file.Id,
                SheetName = sheetName,
                RowNumber = rowNumber,
            };
            if (file.CiviliteColumn > -1 && content.Count > file.CiviliteColumn)
                dbProspect.Civilite = GetCivilite(content[file.CiviliteColumn]);
            if (file.PhoneColumn > -1 && content.Count > file.PhoneColumn)
                dbProspect.Phone = content[file.PhoneColumn];
            if (file.ZipcodeColumn > -1 && content.Count > file.ZipcodeColumn)
                dbProspect.Zipcode = content[file.ZipcodeColumn]?.Trim('|', ' ');
            if (file.FirstnameColumn > -1 && content.Count > file.FirstnameColumn)
            {
                dbProspect.Name = content[file.FirstnameColumn];
                if (file.LastnameColumn > -1 && content.Count > file.LastnameColumn)
                    dbProspect.Name += " " + content[file.LastnameColumn];
            }

            if (file.NameColumn > -1 && content.Count > file.NameColumn)
            {
                dbProspect.Name = content[file.NameColumn];
            }
            return dbProspect;
        }

        static string GetCivilite(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;
            var lower = data.ToLower();
            if (lower.StartsWith("female") || lower.StartsWith("mme") || lower.StartsWith("madame"))
                return "Mrs";
            if (lower.StartsWith("male") || lower.StartsWith("monsieur") || lower.StartsWith("m"))
                return "Mr";
            return string.Empty;
        }

        static string GetSheetName(OpenXmlElement sheet)
        {
            foreach (OpenXmlAttribute attr in sheet.GetAttributes())
            {
                if (attr.LocalName == "name")
                    return attr.Value ?? string.Empty;
            }

            return string.Empty;
        }
    }

    private static string? GetCellValue(List<string> sharedStringTable, CellFormats? cellFormats,
        NumberingFormats? numberingFormats, Cell cell)
    {
        string? value = cell.CellValue?.InnerText;
        if (string.IsNullOrEmpty(value))
            return value;
        if (cell.DataType != null)
        {
            if (cell.DataType.Value == CellValues.SharedString)
            {
                var content = sharedStringTable.ElementAt(int.Parse(value));
                return content;
            }
            else if (cell.DataType.Value == CellValues.Boolean)
            {
                switch (value)
                {
                    case "0":
                        value = "FALSE";
                        break;
                    default:
                        value = "TRUE";
                        break;
                }

                return value;
            }
            else if (cell.DataType.Value == CellValues.Number)
            {
                value = GetNumberValueWithFormating(value);
            }
        }
        else if (cell.StyleIndex != null && cell.StyleIndex.HasValue && numberingFormats != null && cellFormats != null)
        {
            value = GetNumberValueWithFormating(value);
        }

        return value;

        string GetNumberValueWithFormating(string val)
        {
            // look up the style used for the cell
            int formatStyleIndex = Convert.ToInt32(cell.StyleIndex.Value);
            var cf = (CellFormat)cellFormats.ElementAt(formatStyleIndex);

            if (cf.NumberFormatId != null)
            {
                var numberFormatId = cf.NumberFormatId.Value;
                var numberingFormat = numberingFormats.Cast<NumberingFormat>()
                    .SingleOrDefault(f => f.NumberFormatId.Value == numberFormatId);

                var format = numberingFormat?.FormatCode?.Value;
                if (long.TryParse(val, out var number))
                    val = number.ToString(format ?? string.Empty);
                else if (decimal.TryParse(val, out var decimalNumber))
                    val = decimalNumber.ToString(format ?? string.Empty);
                else if (double.TryParse(val, out var doubleNumber))
                    val = doubleNumber.ToString(format ?? string.Empty);
            }

            return val;
        }
    }

    private static List<string> GetCleanRowData(List<string> sharedStringTable, CellFormats? cellFormats,
        NumberingFormats? numberingFormats, Row row, string? telephoneColumnName)
    {
        var list = new List<string?>();
        foreach (var c in row.Descendants<Cell>())
        {
            var text = GetCellValue(sharedStringTable, cellFormats, numberingFormats, c);
            if (telephoneColumnName != null && telephoneColumnName == text)
                text = "Téléphone";
            list.Add(text);
        }

        return CleanList(list);
    }

    private static List<string> CleanList(List<string?> list)
    {
        if (list.Count == 0)
            return [];
        var last = list.Last();
        while (last == null)
        {
            list.RemoveAt(list.Count - 1);
            if (list.Count == 0)
                return [];
            last = list.Last();
        }

        return list.Cast<string>().ToList();
    }
    
    private static List<string> GetSharedString(SharedStringTablePart sharedStringTablePart)
    {
        List<string> sharedStrings = new List<string>();
        if (sharedStringTablePart != null)
        {
            foreach (SharedStringItem item in sharedStringTablePart.SharedStringTable.Elements<SharedStringItem>())
            {
                sharedStrings.Add(item.InnerText);
            }
        }
        return sharedStrings;
    }
}