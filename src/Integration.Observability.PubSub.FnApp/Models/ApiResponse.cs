using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.PubSub.FnApp.Models
{
    /// <summary>
    /// To represent an API response, based on the Azure API Management error response. 
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
