using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.PubSub.FnApp.Models
{
    /// <summary>
    /// Model to represent Cloud Events as per the specification (https://github.com/cloudevents/spec)
    /// </summary>
    public class CloudEvent
    {
        public string SpecVersion { get; set; }
        public string Type { get; set; }
        public string Subject { get; set; }
        public string Id { get; set; }
        public DateTime Time { get; set; }
        public string DataContentType { get; set; }
        public object Data { get; set; }
    }
}
