using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nerdfee2.Contracts
{

    public class FeedResponse
    {
        public class PagingContainer
        {
            [JsonProperty(PropertyName = "next")]
            public string Next { get; set; }
        }

        public List<Article> Data { get; set; }
        public PagingContainer Paging { get; set; }
    }
}