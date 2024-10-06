
namespace Capitalead.Services;

public class GoogleDriveService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public GoogleDriveService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<IList<FileInfo>> LoadFiles()
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Referrer = new Uri("https://drive.google.com/");
        var response = await client.GetFromJsonAsync<FileList>(
            "https://www.googleapis.com/drive/v3/files?q='14fVRTt86J9GzKoOfeMlFBaMbApA_idl7'+in+parents&key=AIzaSyC1qbk75NzWBvSaDh6KnsjjA9pIrP4lYIE");

        var saveFolder = _configuration["files_store_folder"] ?? throw new ArgumentNullException("files_store_folder");
        foreach (var fileInfo in response.Files)
        {
            if (fileInfo.Name.StartsWith("~"))
                continue;
            await using var stream = await client.GetStreamAsync($"https://drive.google.com/uc?export=download&id={fileInfo.Id}");
            await using var fileStream = File.OpenWrite(Path.Combine(saveFolder, fileInfo.Name));
            await stream.CopyToAsync(fileStream);
        }
        return response.Files.Where(fileInfo => !fileInfo.Name.StartsWith("~")).ToList();
    }
}

public record FileList(string Kind, bool IncompleteSearch, IList<FileInfo> Files);
public record FileInfo(string Kind, string MimeType, string Id, string Name);