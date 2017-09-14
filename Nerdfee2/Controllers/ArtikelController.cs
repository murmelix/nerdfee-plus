using Microsoft.AspNet.Identity;
using Nerdfee2.Contracts;
using Nerdfee2.Data;
using Nerdfee2.Models;
using Newtonsoft.Json;
using PagedList;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Nerdfee2.Controllers
{
    public class ArtikelController : Controller
    {

        private readonly NerdfeeContext _context;
        private string _token;

        public ArtikelController()
        {
            _context = new NerdfeeContext();
        }

        public async Task<ActionResult> Index(int? page, string kategorie, int? skip)
        {
            var pageNumber = page ?? 1;
            ViewBag.Title = "Nerdfee - Seite " +pageNumber;
            ViewBag.PageNumber = pageNumber;
            ViewBag.Kategorie = kategorie;
            var pageSize = 10;
            IOrderedQueryable<Article> articles = _context.Articles.OrderByDescending(x => x.Veroeffentlicht);
            if (!string.IsNullOrEmpty(kategorie))
            {
                if (User.Identity.IsAuthenticated)
                {
                    articles = (from t1 in _context.Articles
                                join t2 in _context.ArticleTags on t1.Id equals t2.ArticleId
                                where t2.Tag == kategorie
                                select t1).OrderByDescending(x => x.Erstellt);
                }
                else
                {
                    articles = (from t1 in _context.Articles
                                join t2 in _context.ArticleTags on t1.Id equals t2.ArticleId
                                where t2.Tag == kategorie && t1.Veroeffentlicht.HasValue
                                select t1).OrderByDescending(x => x.Veroeffentlicht);
                }
            }
            else
            {
                if (User.Identity.IsAuthenticated)
                    articles = _context.Articles.OrderByDescending(x => x.Erstellt);
                else
                    articles = _context.Articles.Where(x => x.Veroeffentlicht.HasValue).OrderByDescending(x => x.Veroeffentlicht);
            }
            var pageData = articles.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            if (pageNumber == 1 && pageData.Count() > 5 && skip.HasValue)
                pageData = pageData.Skip(skip.Value);
            ViewBag.Page = pageNumber;
            ViewBag.PageSize = pageSize;
            var list = new StaticPagedList<Article>(pageData, pageNumber, pageSize, articles.Count());
            return View(list);
        }

        public async Task<ActionResult> Show(Guid id)
        {
            var hasImage = _context.ArticleImages.Any(x => x.ArticleId == id);
            ViewBag.HasImage = hasImage;
            var artikel = _context.Articles.First(x => x.Id == id);
            ViewBag.Title = artikel.Titel;
            return View(artikel);
        }

        public ActionResult GetTags()
        {
            return new JsonResult { Data = _context.ArticleTags.Select(x=>x.Tag).Distinct().ToList(), JsonRequestBehavior = JsonRequestBehavior.AllowGet };      
        }

        [Authorize]
        public ActionResult DeleteAsk(Guid id)
        {
            return View(id);
        }

        [Authorize]
        public ActionResult Delete(Guid id)
        {
            ViewBag.Title = "Nerdfee - Löschen";
            var item = _context.Articles.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                _context.Articles.Remove(item);
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        public ActionResult New()
        {
            ViewBag.Title = "Nerdfee - Neu";
            return View();
        }

        [Authorize]
        public ActionResult Publish(Guid id)
        {
            var item = _context.Articles.FirstOrDefault(x => x.Id == id);
            item.Veroeffentlicht = DateTime.Now;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        public string Token
        {
            get
            {
                if (_token == null)
                {
                    var claimsIdentity = HttpContext.User.Identity as ClaimsIdentity;
                    if (claimsIdentity != null)
                    {
                        var claims = claimsIdentity.Claims;
                        _token = claims.First(x => x.Type == "urn:facebook:access_token").Value;
                    }
                }
                return _token;
            }
        }

        [Authorize]
        public async Task<ActionResult> Facebook(Guid id)
        {
            var item = _context.Articles.FirstOrDefault(x => x.Id == id);
            if (!item.Veroeffentlicht.HasValue)
            {
                item.Veroeffentlicht = DateTime.Now;
            }
            
            var key = GetData<PageAccessResponse>(string.Format("https://graph.facebook.com/me/accounts?access_token=" + Token));
            var access = key.Data[0];
            var r = WebRequest.Create(string.Format("https://graph.facebook.com/{2}/feed?message={0}&link={1}&access_token={3}", item.Teaser, "http://nerdfee.de/Artikel/Show/" + id + "?titel=" + Uri.EscapeDataString(item.Titel), access.PageId, access.AccessToken));
            r.Method = "POST";
            var re = await r.GetResponseAsync();
            var s = re.GetResponseStream();
            var serializor = JsonSerializer.Create();
            var feed = serializor.Deserialize<PagePostResponse>(new JsonTextReader(new StreamReader(s)));
            item.FacebookId = feed.PageId;
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        private T GetData<T>(string url)
        {
            var r = WebRequest.Create(url);
            var re = r.GetResponseAsync();
            re.Wait();
            var s = re.Result.GetResponseStream();
            var serializor = JsonSerializer.Create();
            var feed = serializor.Deserialize<T>(new JsonTextReader(new StreamReader(s)));
            return feed;
        }

        [HttpPost]
        [Authorize]
        public ActionResult New(ArticleEditModel article, HttpPostedFileBase image)
        {
            var a = new Article
            {
                Erstellt = DateTime.Now,
                Id = Guid.NewGuid(),
                Teaser = article.Teaser,
                Titel = article.Titel,
                Text = article.Text,
                Kategorie = article.Kategorie,
            };
            _context.Articles.Add(a);
            if (!string.IsNullOrEmpty(article.Kategorie))
            {
                foreach (var k in article.Kategorie.Split(',')) {
                    _context.ArticleTags.Add(new ArticleTag
                    {
                        ArticleId = a.Id,
                        Tag = k
                    });
                }
            }
            if (image != null)
            {
                var i = new ArticleImage();
                i.ArticleId = a.Id;
                i.Data = new byte[image.ContentLength];
                image.InputStream.Read(i.Data, 0, image.ContentLength);
                _context.ArticleImages.Add(i);
            }
            _context.SaveChanges();
            return RedirectToAction("Index");
        }

        [Authorize]
        public ActionResult Edit(Guid id, int? from)
        {
            ViewBag.Title = "Nerdfee - Edit";
            ViewBag.FromPage = from ?? 1;
            var item = _context.Articles.FirstOrDefault(x => x.Id == id);            
            var tags = "";
            foreach (var t in _context.ArticleTags.Where(x => x.ArticleId == id))
                tags += "," + t.Tag;
            return View(new ArticleEditModel
            {
                Id = item.Id,
                Kategorie = tags.Trim(','),
                Teaser = item.Teaser,
                Text = item.Text,
                Titel = item.Titel,
                RedirectPage = from ?? 1
            });
        }

        public ActionResult Kategorien()
        {
            ViewBag.Title = "Nerdfee - Kategorien";
            return View(_context.ArticleTags.Select(x=>x.Tag).Distinct().ToList());            
        }

        [HttpPost]
        [Authorize]
        public ActionResult Edit(Guid id, ArticleEditModel article, HttpPostedFileBase image)
        {
            var item = _context.Articles.FirstOrDefault(x => x.Id == id);
            item.Kategorie = article.Kategorie;
            item.Text = article.Text;
            item.Teaser = article.Teaser;
            item.Titel = article.Titel;            
            _context.ArticleTags.RemoveRange(_context.ArticleTags.Where(x=>x.ArticleId == id));
            if (!string.IsNullOrEmpty(article.Kategorie))
            {
                foreach (var k in article.Kategorie.Split(','))
                {
                    _context.ArticleTags.Add(new ArticleTag
                    {
                        ArticleId = item.Id,
                        Tag = k
                    });
                }
            }
            if (image != null)
            {
                var i = _context.ArticleImages.FirstOrDefault(x => x.ArticleId == id);
                var data = new byte[image.ContentLength];
                image.InputStream.Read(data, 0, image.ContentLength);
                if (i == null)
                {
                    i = new ArticleImage();
                    i.ArticleId = id;
                    i.Data = data;
                    _context.ArticleImages.Add(i);
                }
                else
                {
                    i.Data = data;
                }
            }
            _context.SaveChanges();
            return RedirectToAction("Index", new { page = article.RedirectPage });
        }

        public FileStreamResult GetPicture(Guid id)
        {
            var pic = _context.ArticleImages.FirstOrDefault(x => x.ArticleId == id);
            if (pic == null)
                return null;
            Stream stream = new MemoryStream(pic.Data);
            return new FileStreamResult(stream, "images/png");
        }
    }
}