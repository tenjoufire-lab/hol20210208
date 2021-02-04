using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Globalization;

namespace YOURCOMPANY.Function
{
    public static class BlobTrigger
    {
        [FunctionName("BlobTrigger")]
        public static void Run([BlobTrigger("pictures/{name}", Connection = "YOURSTORAGE_CONNECTION")] Stream myBlob, string name,
        [CosmosDB(
        databaseName: "custom_vision",
        collectionName:"prediction_result",
        ConnectionStringSetting="CosmosDBConnection"
        )]out dynamic document,
        ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            string predectionKey = Environment.GetEnvironmentVariable("PREDICTION_KEY");
            string endpoint = Environment.GetEnvironmentVariable("ENDPOINT");
            double th = double.Parse(Environment.GetEnvironmentVariable("THRESHOLD"));
            var client = new HttpClient();
            // Request headers - replace this example key with your valid Prediction-Key.
            client.DefaultRequestHeaders.Add("Prediction-Key", predectionKey);
            int peopleCount = 0;
            string url = endpoint;
            HttpResponseMessage response;
            byte[] byteData;

            string dstring = name.Split('+')[0];
            DateTime dt = DateTime.ParseExact(dstring, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buf = new byte[32768];
                while (true)
                {
                    int read = myBlob.Read(buf, 0, buf.Length);
                    if (read > 0)
                    {
                        ms.Write(buf, 0, read);
                    }
                    else
                    {
                        break;
                    }
                }
                byteData = ms.ToArray();
            }


            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = client.PostAsync(url, content).Result;
                //Console.WriteLine(await response.Content.ReadAsStringAsync());
                var jsonString = response.Content.ReadAsStringAsync().Result;
                peopleCount = CountNumOfPeople(jsonString, th);
            }
            log.LogInformation($"{peopleCount}人検出されました");

            var culture = CultureInfo.CreateSpecificCulture("ja-JP");
            document = new
            {
                id = Guid.NewGuid(),
                Timestring = dt.ToString("u", culture),
                Date = dt.ToString("d", culture),
                Time = dt.ToString("t", culture),
                PeopleCount = peopleCount,
                Place = "TEST"
            };
        }

        private static int CountNumOfPeople(string jsonString, double th)
        {
            var peopleCount = 0;
            var jsonobject = JsonConvert.DeserializeObject<PredictionResult>(jsonString);

            foreach (var detectedObject in jsonobject.predictions)
            {
                //確信度がある閾値よりも高い場合、人と認定
                if (detectedObject.probability > th)
                {
                    peopleCount++;
                }
            }

            return peopleCount;
        }
    }

    public class PredictionResult
    {
        public string id { get; set; }
        public string project { get; set; }
        public string iteration { get; set; }
        public string created { get; set; }
        public PredictionObject[] predictions { get; set; }
    }

    public class PredictionObject
    {
        public string tagId { get; set; }
        public string tagName { get; set; }
        public double probability { get; set; }
    }

    public class NumOfPeople
    {
        public string Id { get; set; }
        public string TimeString { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public int PeopleCount { get; set; }
        public string Place { get; set; }
    }

}
