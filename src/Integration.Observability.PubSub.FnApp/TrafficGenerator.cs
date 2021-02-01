using System;
using System.IO;
using System.Net.Http;
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
        public void Run([TimerTrigger("%TrafficGeneratorCron%")]TimerInfo myTimer, ILogger log)
        {
            try
            {
                PostPayloadsAsync(log).ConfigureAwait(true);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Failed to generate traffic");
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
            int numberOfRequests = randomGenerator.Next(0, 10);

            log.LogInformation($"Generating traffic with '{numberOfRequests}' requests.");

            for (int i = 0; i < numberOfRequests; i++)
            {
                int payloadIndex = randomGenerator.Next(0, 10);
                string payload = GetPayload(payloadIndex);
                await _httpClient.PostAsync($"{_options.Value.BaseUrl }/userupdated", 
                                            new StringContent(payload, Encoding.UTF8, "application/json"));
            }
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
            payload = payload.Replace("{timestamp}", $"{DateTime.Now:yyyy-MM-ddTHH:mm:ssZ}");
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
            // Gets the file path depending on the operating system
            string path = Path.Combine(subfolder, fileName);

            if (!File.Exists(path))
            {
                throw new ArgumentException($"Could not find file at path: {path}");
            }

            return File.ReadAllText(path);
        }
    }
}
