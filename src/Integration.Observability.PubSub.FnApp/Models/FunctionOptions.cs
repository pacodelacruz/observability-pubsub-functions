using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.PubSub.FnApp.Models
{
    /// <summary>
    /// Class to implement the Options Pattern described here
    /// https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-2.2#reload-configuration-data-with-ioptionssnapshot
    /// And particularly on Azure Functions here
    /// https://docs.microsoft.com/en-us/azure/architecture/serverless/code
    /// </summary>
    class FunctionOptions
    {
        public string ServiceBusUserUpdateQueueName { get; set; } = "userupdate-evt";
        public int ServiceBusUserUpdateQueueMaxDeliveryCount { get; set; } = 2;
    }
}
