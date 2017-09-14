using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Nerdfee2.Contracts
{
    public class Article
    {
        public Article()
        {
            Id = Guid.NewGuid();
        }
        [Key]
        public Guid Id { get; set; }
        [JsonProperty(PropertyName = "id")]
        public string FacebookId { get; set; }
        [JsonProperty(PropertyName = "message")]
        public string Text { get; set; }
        [JsonProperty(PropertyName = "created_time")]
        public DateTime Erstellt { get; set; }
        public string Kategorie { get; set; }
        public string Teaser { get; set; }
        public string Titel { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long ThreadId { get; set; }
        public DateTime? Veroeffentlicht { get; set; }
    }
}