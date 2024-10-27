using Capitalead.Data;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace Capitalead.Services;

public class FilesExporter(
    GoogleDriveService googleDriveService,
    ILogger<FilesExporter> logger,
    AppDatabase database,
    IConfiguration configuration,
    NoCrmService noCrmService)
{
    private const string TelephoneColumn = "TELEPHONE";

    public async Task DownloadFiles()
    {
        logger.LogInformation("Started download files script...");

        logger.LogInformation("Downloading files from Google Drive");
        var files = await googleDriveService.LoadFiles();
        logger.LogInformation("Downloaded files from Google Drive");
        foreach (var file in files)
        {
            var fileForExport = new FileForExport()
            {
                Id = Guid.NewGuid(),
                FileName = file.Name,
                MimeType = file.MimeType,
                FileId = file.Id,
                Created = DateTime.UtcNow
            };
            await database.FilesForExport.AddAsync(fileForExport);
        }

        await database.SaveChangesAsync();
        logger.LogInformation("Saved files to database");

        logger.LogInformation("Successfully downloaded files!");
    }

    public async Task ExportFile(string fileName)
    {
        try
        {
            logger.LogInformation("Started export file {FileName} script...", fileName);
            var fileForExport = await database.FilesForExport.FirstOrDefaultAsync(f => f.FileName == fileName);
            if (fileForExport == default)
            {
                logger.LogInformation("File {FileName} does not exist!", fileName);
                return;
            }

            var userEmail = configuration["nocrm_user_email"];
            var dbUser = await database.Users.FirstAsync(u => u.Email == userEmail);
            var fileId = fileForExport.Id;
            var saveFolder = configuration["files_store_folder"] ??
                             throw new ArgumentNullException("files_store_folder");
            var filePath = Path.Combine(saveFolder, fileForExport.FileName);
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
                    logger.LogInformation("File {FileName} sheets is empty!", fileName);
                    return;
                }

                List<string> sharedStrings = GetSharedString(workbookPart.SharedStringTablePart);
                logger.LogInformation("sharedStrings loaded");
                var cellFormats = workbookPart.WorkbookStylesPart.Stylesheet.CellFormats;
                var numberingFormats = workbookPart.WorkbookStylesPart.Stylesheet.NumberingFormats;

                foreach (var sheet in sheets)
                {
                    var sheetName = GetSheetName(sheet);
                    var sheetForExport =
                        await database.SheetsFromFiles.FirstOrDefaultAsync(s =>
                            s.FileId == fileId && s.SheetName == sheetName);
                    if (sheetForExport == default)
                    {
                        sheetForExport = new SheetFromFile()
                        {
                            Id = Guid.NewGuid(),
                            FileId = fileId,
                            SheetName = sheetName,
                            ProcessedCount = 0
                        };
                        await database.SheetsFromFiles.AddAsync(sheetForExport);
                        await database.SaveChangesAsync();
                    }
                    logger.LogInformation("Processing {SheetName} from row {RowNumber}", sheetName, sheetForExport.ProcessedCount);

                    var worksheetPart = workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart;

                    int currentRow = 1;
                    long count = 0;
                    List<string>? headersList = null;
                    var data = new List<string?>();

                    var content = new List<List<string>>();
                    bool skipRow = false;

                    //Open Stream
                    OpenXmlReader reader = OpenXmlReader.Create(worksheetPart);
                    while (reader.Read())
                    {
                        if (reader.ElementType == typeof(Row) && reader.IsStartElement)
                        {
                            var newRow = Convert.ToInt32(reader.Attributes[0].Value);
                            if (newRow > 1 && count < sheetForExport.ProcessedCount)
                            {
                                count++;
                                skipRow = true;
                                currentRow = newRow;
                                logger.LogInformation("Skip Row {CurrentRow}", currentRow);
                                continue;
                            }
                            else
                            {
                                skipRow = false;
                            }

                            if (currentRow != newRow)
                            {
                                var cleanData = CleanList(data);
                                if (cleanData.Count == 0)
                                {
                                    logger.LogInformation("Row {CurrentRow} is empty. Finish sheet {SheetName}", currentRow, sheetName);
                                    break;
                                }
                                if (currentRow == 1)
                                {
                                    headersList = cleanData;
                                    content.Add(headersList);
                                }
                                else
                                {
                                    count++;
                                    content.Add(cleanData);
                                    if (content.Count == 5000)
                                    {
                                        await SaveSheet(content, sheetForExport, fileForExport, dbUser);
                                        content.Clear();
                                        content.Add(headersList);
                                    }
                                }

                                data.Clear();
                            }

                            currentRow = newRow;
                        }
                        else if (reader.ElementType == typeof(Cell))
                        {
                            if (skipRow)
                                continue;

                            //If a cell is empty, the cell node dosen't exist in XML.
                            Cell cellValue = (Cell)reader.LoadCurrentElement(); //this skipt the EndElement

                            var text = GetCellValue(sharedStrings, cellFormats, numberingFormats, cellValue);
                            if (currentRow == 1)
                            {
                                if (TelephoneColumn == text)
                                    text = "Téléphone";
                            }

                            data.Add(text);
                        }

                    }

                    reader.Close();
                    await SaveSheet(content, sheetForExport, fileForExport, dbUser);
                    logger.LogInformation("Processing {SheetName} finished", sheetName);
                    // Worksheet worksheet = worksheetPart.Worksheet;
                    // IEnumerable<Row> rows = worksheet.GetFirstChild<SheetData>().Descendants<Row>();
                    // var headersRow = rows.FirstOrDefault();
                    // if (headersRow == default)
                    //     continue;
                    // var headersList = GetCleanRowData(sharedStringTable, cellFormats, numberingFormats, headersRow, "TELEPHONE");
                    // if (headersList.Count == 0 || headersList.All(h => string.IsNullOrEmpty(h.ToString())))
                    //     continue;
                    //
                    //
                    // long count = 0;
                    // foreach (var r in rows.Skip(1))
                    // {
                    //     if (count < sheetForExport.ProcessedCount)
                    //     {
                    //         count++;
                    //         continue;
                    //     }
                    //
                    //     var prospect = GetCleanRowData(sharedStringTable, cellFormats, numberingFormats, r, null);
                    //     content.Add(prospect);
                    //     count++;
                    //     if (content.Count == 5000)
                    //     {
                    //         await SaveSheet(content, sheetForExport);
                    //         content.Clear();
                    //         content.Add(headersList);
                    //     }
                    // }
                    // await SaveSheet(content, sheetForExport);
                }
            }

            fileForExport.CompletedDate = DateTime.UtcNow;
            fileForExport.Exported = true;
            database.FilesForExport.Update(fileForExport);
            await database.SaveChangesAsync();

            logger.LogInformation("Export file {FileName} script finished....", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while exporting file {FileName}", fileName);
            throw;
        }

        async Task SaveSheet(List<List<string>> content, SheetFromFile sheet, FileForExport fileForExport, User dbUser)
        {
            if (content.Count < 2)
                return;
            var name = Path.GetFileNameWithoutExtension(fileName);
            var index = (sheet.ProcessedCount + content.Count - 1) / 4999;
            if (content.Count < 4999)
                index += 1;
            var noCrmSheet = await CreateNewProspectingList($"{name} {sheet.SheetName} {index:00000}",
                new[] { fileName, sheet.SheetName }, content, dbUser.Email);
            sheet.ProcessedCount += content.Count - 1;
            var sheetId = sheet.Id;
            var newCount = sheet.ProcessedCount;
            await database.SheetsFromFiles
                .Where(s => s.Id == sheetId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(b => b.ProcessedCount, newCount));
            var exportedSpreadsheet = new ExportedSpreadsheet()
            {
                SheetId = noCrmSheet.Id,
                Title = noCrmSheet.Title,
                FileId = fileForExport.Id,
                Id = Guid.NewGuid(),
                UserId = dbUser.Id,
            };
            await database.ExportedSpreadsheets.AddAsync(exportedSpreadsheet);
            await database.SaveChangesAsync();
            
            logger.LogInformation("Processed {SheetName}, processed count {Count}", noCrmSheet.Title, newCount);
        }

        string GetSheetName(OpenXmlElement sheet)
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

    private async Task<NoCrmSpreadsheet> CreateNewProspectingList(string listTitle, string[] tags,
        List<List<string>> content, string userEmail)
    {
        logger.LogInformation("Creating new prospecting list {ListTitle}", listTitle);
        var body = Helper.BuildJsonBodyForCreatingProspList(listTitle, tags, userEmail, content);
        return await noCrmService.CreateProspectingList(body);
        // return new NoCrmSpreadsheet(1999999, body.Tags,
        //     body.Title, null,
        //     new[] { "Neighborhood", "Parsing Date", "Type", "Téléphone", "Rooms", "Size", "Energy" }, null, 0);

    }
}