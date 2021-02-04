using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;
using Azure.Storage.Blobs;
using System.IO;

namespace YOURCOMPANY.Function
{
    public static class TimerTrigger
    {
        [FunctionName("TimerTrigger")]
        public static void Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, ILogger log)
        {
            string ConnectionString = Environment.GetEnvironmentVariable("YOURSTORAGENAME_STORAGE");
            BlobServiceClient blobServiceClient = new BlobServiceClient(ConnectionString);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("pictures");

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var endpoint = Environment.GetEnvironmentVariable("CAMERA_IMAGE_URL");
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Authentication-Token", Environment.GetEnvironmentVariable("AUTHTOKEN"));
            //request.Headers.Add("Content-Type", "application/json");

            var res = client.SendAsync(request).Result;
            var jsonstring = res.Content.ReadAsStringAsync().Result;
            List<ImageInfo> imageInfos = JsonConvert.DeserializeObject<List<ImageInfo>>(jsonstring);
            foreach (var info in imageInfos)
            {
                log.LogInformation(info.url);
                log.LogInformation(info.datetime);
                var imageRequest = new HttpRequestMessage(HttpMethod.Get, info.url);
                var imageRes = client.SendAsync(imageRequest).Result;
                Stream st = imageRes.Content.ReadAsStreamAsync().Result;
                BlobClient blobClient = containerClient.GetBlobClient(info.datetime + ".jpg");
                try
                {
                    blobClient.Upload(st);
                }
                catch
                {
                    log.LogInformation(info.datetime+"の保存に失敗しました");
                }

            }
        }
    }

    public class ImageInfo
    {
        public string datetime { get; set; }
        public string url { get; set; }
    }
}
