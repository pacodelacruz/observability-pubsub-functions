﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Integration.Observability.PubSub.FnApp.Models
{
    /// <summary>
    /// Represents a User Event 
    /// </summary>
    public class UserEventDto
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string GivenName { get; set; }
        public string FamilyName { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
