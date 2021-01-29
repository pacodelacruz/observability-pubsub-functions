using System;
using System.IO;
using System.Net.Http;
using Integration.Observability.PubSub.FnApp.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
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

        [FunctionName(nameof(TrafficGenerator))]
        public void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogDebug(GetPayload());
        }

        private string GetPayload()
        {
            return GetDataFromFile("TrafficGeneratorPayloads", "0.json");
        }

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
