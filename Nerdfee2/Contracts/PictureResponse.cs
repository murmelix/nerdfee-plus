using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nerdfee2.Contracts
{
    public class PictureResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "picture")]
        public string PictureUrl { get; set; }
    }
}