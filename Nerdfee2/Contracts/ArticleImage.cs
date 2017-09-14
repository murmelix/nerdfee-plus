using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Nerdfee2.Contracts
{
    public class ArticleImage
    {
        public ArticleImage()
        {
            Id = Guid.NewGuid();
        }
        [Key]
        public Guid Id { get; set; }
        public Guid ArticleId { get; set; }
        public short OrderBy { get; set; }
        public byte[] Data { get; set; }
    }
}