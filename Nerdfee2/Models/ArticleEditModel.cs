using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Nerdfee2.Models
{
    public class ArticleEditModel
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public string Teaser { get; set; }
        public string Titel { get; set; }
        public string Kategorie { get; set; }
        public byte[] ImageData { get; set; }
        public int RedirectPage { get; set; }
    }
}