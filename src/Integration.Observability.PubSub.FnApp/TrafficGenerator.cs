using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Integration.Observability.PubSub.FnApp.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.Observability.PubSub.FnApp
{
    public class TrafficGenerator
    {
        private readonly HttpClient _httpClient;
        private readonly IOptions<FunctionOptions> _options;

        public TrafficGenerator(IOptions<FunctionOptions> options, HttpClient httpClient)
        {
            _options = options;
            _httpClient = httpClient;
        }

        /// <summary>
        /// Generate periodic traffic to the publisher interface based on the CRON expression in the TrafficGeneratorCron app setting. 
        /// It can be disabled using the TrafficGeneratorDisabled app setting
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [Disable("TrafficGeneratorDisabled")]
        [FunctionName(nameof(TrafficGenerator))]
        public async Task Run([TimerTrigger("%TrafficGeneratorCron%")] TimerInfo myTimer, ILogger log)
        {
            try
            {
                await PostPayloadsAsync(log);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to generate traffic.");
                throw;
            }
        }

        /// <summary>
        /// Post a random number of payloads to generate traffic
        /// </summary>
        /// <param name="log"></param>
        private async Task PostPayloadsAsync(ILogger log)
        {
            // Generate a random number of requests
            Random randomGenerator = new Random();
            int numberOfRequests = randomGenerator.Next(1, 10);
            var httpResponses = new List<string>();

            log.LogInformation($"Generating traffic with '{numberOfRequests}' requests to {_options.Value.BaseUrl }/userupdated.");

            for (int i = 0; i < numberOfRequests; i++)
            {
                int payloadIndex = randomGenerator.Next(0, 10);
                string payload = GetPayload(payloadIndex);
                var response = await _httpClient.PostAsync($"{_options.Value.BaseUrl }/userupdated",
                                            new StringContent(payload, Encoding.UTF8, "application/json"));
                httpResponses.Add(response.StatusCode.ToString());
            }

            log.LogInformation($"Http responses: {string.Join(',', httpResponses)}");
        }

        /// <summary>
        /// Get a payload based from templates
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private string GetPayload(int index)
        {
            string payload = GetDataFromFile("TrafficGeneratorPayloads", $"{index}.json");
            payload = payload.Replace("{batchId}", $"{DateTime.Now:yyyyMMddHHmm}00{index}");
            payload = payload.Replace("{timestamp}", $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss}");
            return payload;

        }

        /// <summary>
        /// Get data as string from a file in a subfolder in the current directory
        /// </summary>
        /// <param name="subfolder"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string GetDataFromFile(string subfolder, string fileName)
        {
            // Get the root file path with support for Azure Functions
            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            string path = Path.Combine(rootDirectory, subfolder, fileName);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find file at path: {path}");
            }

            return File.ReadAllText(path);
        }
    }
}
