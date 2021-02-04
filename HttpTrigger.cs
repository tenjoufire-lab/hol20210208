using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;

namespace YOURCOMPANY.Function
{
    public static class HttpTrigger
    {
        [FunctionName("HttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string queryDate = req.Query["date"];
            string responce = "";

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            queryDate = queryDate ?? data?.queryDate;

            CosmosClient client = new CosmosClient(Environment.GetEnvironmentVariable("CosmosDBConnection"));
            Container container = client.GetDatabase("custom_vision").GetContainer("prediction_result");

            var quetyText = $"SELECT * FROM c WHERE c.Date = '{queryDate}' ORDER BY c.Time ASC";
            FeedIterator<NumOfPeople> queryResultSetIterator = container.GetItemQueryIterator<NumOfPeople>(quetyText);
            List<NumOfPeople> people = new List<NumOfPeople>();

            while (queryResultSetIterator.HasMoreResults)
            {
                FeedResponse<NumOfPeople> result = await queryResultSetIterator.ReadNextAsync();
                foreach (var p in result)
                {
                    people.Add(p);
                }
            }

            responce = JsonConvert.SerializeObject(people);


            string responseMessage = string.IsNullOrEmpty(queryDate)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : responce;

            return new OkObjectResult(responseMessage);
        }
    }
}
