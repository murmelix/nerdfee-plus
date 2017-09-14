using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nerdfee2.Contracts
{
    public class PageAccess
    {
        [JsonProperty(PropertyName = "access_token")]
        public string AccessToken { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string PageId { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}