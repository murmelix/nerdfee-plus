using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Nerdfee2.Contracts
{
    public class ArticleTag
    {
        public ArticleTag()
        {
            Id = Guid.NewGuid();
        }
        [Key]
        public Guid Id { get; set; }
        public Guid ArticleId { get; set; }
        public string Tag { get; set; }
    }
}