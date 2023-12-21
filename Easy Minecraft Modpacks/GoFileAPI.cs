using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Easy_Minecraft_Modpacks;

public class GoFileAPI
{
    public class BestServerResponse
    {
        public string status { get; set; }
        public BestServerData data { get; set; }
    }

    public class BestServerData
    {
        public string server { get; set; }
    }

    private static async Task<string> GetBestServer()
    {
        var response = await client.GetAsync("https://api.gofile.io/getServer");
        return JsonConvert.DeserializeObject<BestServerResponse>(await response.Content.ReadAsStringAsync()).data.server;
    }

    public class UploadFileResponse
    {
        public string status { get; set; }
        public UploadFileData data { get; set; }
    }

    public class UploadFileData
    {
        public string downloadPage { get; set; }
        public string code { get; set; }
        public string parentFolder { get; set; }
        public string fileId { get; set; }
        public string fileName { get; set; }
        public string md5 { get; set; }
    }

    private static HttpClient client = new HttpClient();
    
    public static async Task<(UploadFileResponse, string)> UploadFile(string filePath)
    {
        var bestServer = await GetBestServer();
        var url = $"https://{bestServer}.gofile.io/uploadFile";

        var content = new MultipartFormDataContent();

        var fileStream = new FileStream(filePath, FileMode.Open);
        content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

        var response = await client.PostAsync(url, content);
        fileStream.Dispose();

        var uploadData = JsonConvert.DeserializeObject<UploadFileResponse>(await response.Content.ReadAsStringAsync());
        
        return (uploadData, $"https://{bestServer}.gofile.io/download/{uploadData.data.fileId}/{UrlEncode(uploadData.data.fileName)}");
    }
    
    public static string UrlEncode(string input)
    {
        return Uri.EscapeDataString(input);
    }
}