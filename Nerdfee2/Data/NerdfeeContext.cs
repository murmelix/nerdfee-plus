using Nerdfee2.Contracts;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace Nerdfee2.Data
{
    public class NerdfeeContext : DbContext
    {
        public NerdfeeContext(): base("DefaultConnection") 
    {
            //Database.SetInitializer<NerdfeeContext>(new CreateDatabaseIfNotExists<NerdfeeContext>());
        }

        public DbSet<Article> Articles { get; set; }
        public DbSet<ArticleImage> ArticleImages { get; set; }
        public DbSet<ArticleTag> ArticleTags { get; set; }

    }
}