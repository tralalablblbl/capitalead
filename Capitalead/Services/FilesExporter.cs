using System.Text.Json.Nodes;
using Capitalead.Data;
using DocumentFormat.OpenXml;
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
        var saveFolder = configuration["files_store_folder"] ?? throw new ArgumentNullException("files_store_folder");
        var filePath = Path.Combine(saveFolder, fileForExport.FileName);

        using (var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
        {
            var workbookPart = spreadsheetDocument.WorkbookPart ??
                               throw new ArgumentException("spreadsheetDocument.WorkbookPart");
            IEnumerable<Sheet>? sheets = workbookPart?.Workbook?.Sheets?.Elements<Sheet>();
            if (sheets == null)
            {
                logger.LogInformation("File {FileName} sheets is empty!", fileName);
                return;
            }
            foreach (var sheet in sheets)
            {
                var sheetName = GetSheetName(sheet);
                Worksheet worksheet = (workbookPart.GetPartById(sheet.Id.Value) as WorksheetPart).Worksheet;
                IEnumerable<Row> rows = worksheet.GetFirstChild<SheetData>().Descendants<Row>();
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

                var headers = new JsonArray();
                var headersRow = rows.FirstOrDefault();
                if (headersRow == default)
                    continue;
                var headersList = GetCleanRowData(spreadsheetDocument, headersRow);
                foreach (var h in headersList)
                    headers.Add(h);
                if (headers.Count == 0 || headers.All(h => string.IsNullOrEmpty(h.ToString())))
                    continue;

                var content = new JsonArray();
                content.Add(headers.DeepClone());

                long count = 0;
                foreach (var r in rows.Skip(1))
                {
                    if (count < sheetForExport.ProcessedCount)
                    {
                        count++;
                        continue;
                    }

                    var prospect = new JsonArray();
                    var data = GetCleanRowData(spreadsheetDocument, r);
                    foreach (var text in data)
                    {
                        prospect.Add(text);
                    }
                    content.Add(prospect);
                    count++;
                    if (content.Count == 5000)
                    {
                        await SaveSheet(content, sheetForExport);
                        content = new JsonArray();
                        content.Add(headers.DeepClone());
                    }
                }
                await SaveSheet(content, sheetForExport);
            }
        }

        fileForExport.CompletedDate = DateTime.UtcNow;
        fileForExport.Exported = true;
        database.FilesForExport.Update(fileForExport);
        await database.SaveChangesAsync();

        logger.LogInformation("Export file {FileName} script finished....", fileName);

        async Task SaveSheet(JsonArray content, SheetFromFile sheet)
        {
            if (content.Count < 2)
                return;
            var name = Path.GetFileNameWithoutExtension(fileName);
            var index = (sheet.ProcessedCount + content.Count - 1) / 4999;
            if (index == 0)
                index = 1;
            var noCrmSheet = await CreateNewProspectingList($"{name} {sheet.SheetName} {index:00000}", new[] { fileName, sheet.SheetName }, content, userEmail);
            sheet.ProcessedCount += content.Count - 1;
            database.Update(sheet);
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

    private static string? GetCellValue(SpreadsheetDocument doc, Cell cell)
    {
        string? value = cell.CellValue?.InnerText;
        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
        {
            return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements[int.Parse(value)].InnerText;
        }
        return value;
    }

    private static List<string> GetCleanRowData(SpreadsheetDocument doc, Row row)
    {
        var list = new List<string?>();
        foreach (var c in row.Descendants<Cell>())
        {
            var text = GetCellValue(doc, c);
            list.Add(text);
        }

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
    
    private async Task<NoCrmSpreadsheet> CreateNewProspectingList(string listTitle, string[] tags, JsonArray content, string userEmail)
    {
        logger.LogInformation("Creating new prospecting list {ListTitle}", listTitle);
        var body = Helper.BuildJsonBodyForCreatingProspList(listTitle, tags, userEmail, content);
        return await noCrmService.CreateProspectingList(body);
        // return new NoCrmSpreadsheet(1999999, body["tags"].AsArray().Select(t => t.ToString()).ToArray(),
        //     body["title"]!.ToString(), null,
        //     new[] { "Neighborhood", "Parsing Date", "Type", "Téléphone", "Rooms", "Size", "Energy" }, null, 0);

    }
}