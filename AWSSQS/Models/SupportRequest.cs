using System;
using System.Collections.Generic;
using System.Text;

namespace AWSSQS.Models
{
    public class SupportRequest
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Topic { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
