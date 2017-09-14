using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nerdfee2.Contracts
{
    public class PagePostResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string PageId { get; set; }
    }
}