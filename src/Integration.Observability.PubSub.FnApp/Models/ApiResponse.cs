using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.PubSub.FnApp.Models
{
    /// <summary>
    /// To represent an API response.
    /// The structure is based on the Azure API Management error response. 
    /// This allows clients to consume response messages from the backend and mediation layer
    /// </summary>
    public class ApiResponse
    {        
        public int StatusCode { get; set; }
        public String Message { get; set; }
        public string ActivityId { get; set; }

        public ApiResponse(int statusCode, string activityId = null, string message = null)
        {
            StatusCode = statusCode;
            ActivityId = activityId;
            Message = message;
        }

    }

}
