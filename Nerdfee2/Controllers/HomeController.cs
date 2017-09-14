using Microsoft.AspNet.Identity;
using Nerdfee2.Contracts;
using Nerdfee2.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Nerdfee2.Controllers
{
    public class HomeController : Controller
    {
        private readonly NerdfeeContext _context;
        private string _token = null;

        public HomeController()
        {
            _context = new NerdfeeContext();
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

        public async Task<ActionResult> Sync(bool truncate)
        {
            var items = new List<Article>();

            if (truncate)
            {
                _context.Articles.RemoveRange(_context.Articles);
                _context.ArticleImages.RemoveRange(_context.ArticleImages);
            }

            var feed = GetData<FeedResponse>(string.Format("https://graph.facebook.com/v2.9/nerdfee/feed?from=Nerdfee&access_token={0}&format=json&method=get", Token));
            while (feed != null && feed.Data.Count > 0)
            {
                items.AddRange(feed.Data);
                if (feed.Paging == null || string.IsNullOrEmpty(feed.Paging.Next))
                    break;
                feed = GetData<FeedResponse>(feed.Paging.Next);
            }

            var newItems = new List<Article>();
            foreach (var item in items)
            {
                if (item.Text == null) continue;
                if (_context.Articles.Any(x => x.FacebookId == item.FacebookId))
                    continue;
                if (item.Text.Length > 200)
                {
                    var parts = item.Text.Split('\n');
                    item.Titel = parts[0];
                    var i = 1;
                    var teaser = "";
                    while (teaser.Length < 150)
                    {
                        if (parts.Length <= i)
                            break;
                        teaser += parts[i++] + "\n";
                    }
                    item.Teaser = teaser;
                    item.Veroeffentlicht = item.Erstellt;
                    var update = _context.Articles.FirstOrDefault(x => x.FacebookId == item.FacebookId);
                    if (update != null)
                    {
                        update.Titel = item.Titel;
                        update.Teaser = item.Teaser;
                        update.Veroeffentlicht = item.Erstellt;
                    }
                    else
                    {
                        _context.Articles.Add(item);
                        newItems.Add(item);
                    }
                }
            }
            _context.SaveChanges();

            foreach (var article in newItems)
            {
                //if (_context.ArticleImages.Any(x => x.ArticleId == article.Id))
                //    continue;
                string url = GetPictureUrl(article);
                if (!string.IsNullOrEmpty(url))
                {
                    try
                    {
                        var img = new ArticleImage();
                        img.ArticleId = article.Id;
                        img.OrderBy = 0;
                        img.Data = DownloadImage(url);
                        _context.ArticleImages.Add(img);
                    }
                    catch (Exception ex) { }
                }
            }

            _context.SaveChanges();

            return null;
        }

        private byte[] DownloadImage(string url)
        {
            var httpClient = new HttpClient();
            var r = httpClient.GetStreamAsync(url);
            r.Wait();
            var memoryStream = new MemoryStream();
            using (memoryStream)
            {
                var t = r.Result.CopyToAsync(memoryStream);
                t.Wait();
                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream.ToArray();
            }
        }

        private T GetData<T>(string url)
        {
            var r = WebRequest.Create(string.Format(url, Token));
            var re = r.GetResponseAsync();
            re.Wait();
            var s = re.Result.GetResponseStream();
            var serializor = JsonSerializer.Create();
            var feed = serializor.Deserialize<T>(new JsonTextReader(new StreamReader(s)));
            return feed;
        }

        private string GetPictureUrl(Article item)
        {
            try
            {
                var feed = GetData<PictureResponse>("https://graph.facebook.com/v2.9/" + item.FacebookId + "?fields=picture&access_token={0}&format=json&method=get");
                if (feed != null && feed.PictureUrl != null)
                {
                    return string.Format("https://graph.facebook.com/v2.9/{1}/picture?type=normal&access_token={0}", Token, feed.Id.Split('_')[1]);
                }
            }
            catch (Exception ex)
            {
            }
            return null;
        }

        public ActionResult Index()
        {
            ViewBag.Title = "Nerdfee";
            if (User.Identity.IsAuthenticated)
                return View(_context.Articles.OrderByDescending(x => x.Erstellt).Take(5));
            else
                return View(_context.Articles.Where(x => x.Veroeffentlicht.HasValue).OrderByDescending(x => x.Erstellt).Take(5));
        }

        public ActionResult About()
        {
            ViewBag.Title = "Nerdfee - Über mich";

            return View();
        }

        public ActionResult Impressum()
        {
            ViewBag.Title = "Nerdfee - Impressum";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Title = "Nerdfee - Kontakt";

            return View();
        }
    }
}