using Capitalead.Data;

namespace Capitalead.Services;

public class FilesExporter
{
    private readonly GoogleDriveService _googleDriveService;
    private readonly ILogger<FilesExporter> _logger;
    private readonly AppDatabase _database;

    public FilesExporter(GoogleDriveService googleDriveService, ILogger<FilesExporter> logger, AppDatabase database)
    {
        _googleDriveService = googleDriveService;
        _logger = logger;
        _database = database;
    }

    public async Task DownloadFiles()
    {
        _logger.LogInformation("Started download files script...");
        
        _logger.LogInformation("Downloading files from Google Drive");
        var files = await _googleDriveService.LoadFiles();
        _logger.LogInformation("Downloaded files from Google Drive");
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
            await _database.FilesForExport.AddAsync(fileForExport);
        }
        await _database.SaveChangesAsync();
        _logger.LogInformation("Saved files to database");

        _logger.LogInformation("Successfully downloaded files!");
    }
}