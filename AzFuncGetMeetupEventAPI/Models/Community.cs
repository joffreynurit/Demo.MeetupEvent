using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzFuncGetMeetupEventAPI.Models
{
    public class Community
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
