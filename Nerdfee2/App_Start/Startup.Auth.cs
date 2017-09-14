using System;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;
using Nerdfee2.Models;
using Microsoft.Owin.Security.Facebook;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using System.Web;
using System.Net;

namespace Nerdfee2
{
    public partial class Startup
    {
        // For more information on configuring authentication, please visit https://go.microsoft.com/fwlink/?LinkId=301864
        public void ConfigureAuth(IAppBuilder app)
        {
            // Konfigurieren des db-Kontexts, des Benutzer-Managers und des Anmelde-Managers für die Verwendung einer einzelnen Instanz pro Anforderung.
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Anwendung für die Verwendung eines Cookies zum Speichern von Informationen für den angemeldeten Benutzer aktivieren
            // und ein Cookie zum vorübergehenden Speichern von Informationen zu einem Benutzer zu verwenden, der sich mit dem Anmeldeanbieter eines Drittanbieters anmeldet.
            // Konfigurieren des Anmeldecookies.
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                Provider = new CookieAuthenticationProvider
                {
                    // Aktiviert die Anwendung für die Überprüfung des Sicherheitsstempels, wenn sich der Benutzer anmeldet.
                    // Dies ist eine Sicherheitsfunktion, die verwendet wird, wenn Sie ein Kennwort ändern oder Ihrem Konto eine externe Anmeldung hinzufügen.  
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<ApplicationUserManager, ApplicationUser>(
                        validateInterval: TimeSpan.FromMinutes(30),
                        regenerateIdentity: (manager, user) => user.GenerateUserIdentityAsync(manager))
                }
            });            
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            // Aktiviert die Anwendung für das vorübergehende Speichern von Benutzerinformationen beim Überprüfen der zweiten Stufe im zweistufigen Authentifizierungsvorgang.
            app.UseTwoFactorSignInCookie(DefaultAuthenticationTypes.TwoFactorCookie, TimeSpan.FromMinutes(5));

            // Aktiviert die Anwendung für das Speichern der zweiten Anmeldeüberprüfungsstufe (z. B. Telefon oder E-Mail).
            // Wenn Sie diese Option aktivieren, wird Ihr zweiter Überprüfungsschritt während des Anmeldevorgangs auf dem Gerät gespeichert, von dem aus Sie sich angemeldet haben.
            // Dies ähnelt der RememberMe-Option bei der Anmeldung.
            app.UseTwoFactorRememberBrowserCookie(DefaultAuthenticationTypes.TwoFactorRememberBrowserCookie);

            // Auskommentierung der folgenden Zeilen aufheben, um die Anmeldung mit Anmeldeanbietern von Drittanbietern zu ermöglichen
            //app.UseMicrosoftAccountAuthentication(
            //    clientId: "",
            //    clientSecret: "");

            //app.UseTwitterAuthentication(
            //   consumerKey: "",
            //   consumerSecret: "");

            var fbOptions = new FacebookAuthenticationOptions()
            {
                AppId = "1936473296625718",//Configuration["AppId"],
                AppSecret = "2d0c06ec7a49e94c47a352e03166aa73",// Configuration["AppSecret"],
                Provider = new FacebookAuthenticationProvider()
                {
                    OnAuthenticated = (context) =>
                    {
                        // All data from facebook in this object. 
                        var rawUserObjectFromFacebookAsJson = context.User;

                        // Only some of the basic details from facebook 
                        // like id, username, email etc are added as claims.
                        // But you can retrieve any other details from this
                        // raw Json object from facebook and add it as claims here.
                        // Subsequently adding a claim here will also send this claim
                        // as part of the cookie set on the browser so you can retrieve
                        // on every successive request. 
                        context.Identity.AddClaim(new System.Security.Claims.Claim("urn:facebook:access_token", context.AccessToken));                        
                        return Task.FromResult(0);
                    }
                }
            };
            fbOptions.Scope.Add("email");
            fbOptions.Scope.Add("publish_actions");
            fbOptions.Scope.Add("manage_pages");
            fbOptions.Scope.Add("publish_pages");
            var returnUrl = VirtualPathUtility.ToAbsolute("/signin-facebook");
            fbOptions.CallbackPath = new PathString(returnUrl);

            app.UseFacebookAuthentication(fbOptions);

            //app.UseGoogleAuthentication(new GoogleOAuth2AuthenticationOptions()
            //{
            //    ClientId = "",
            //    ClientSecret = ""
            //});
        }
        private class FacebookOauthResponse
        {
            public string access_token { get; set; }
            public long expires_in { get; set; }
            public string token_type { get; set; }
        }

        public class FacebookBackChannelHandler : HttpClientHandler
        {
            protected override async System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                if (!request.RequestUri.AbsolutePath.Contains("/oauth"))
                {
                    request.RequestUri = new Uri(request.RequestUri.AbsoluteUri.Replace("?access_token", "&access_token"));
                }

                var result = await base.SendAsync(request, cancellationToken);
                if (!request.RequestUri.AbsolutePath.Contains("/oauth"))
                {
                    return result;
                }

                var content = await result.Content.ReadAsStringAsync();
                var facebookOauthResponse = JsonConvert.DeserializeObject<FacebookOauthResponse>(content);

                var outgoingQueryString = HttpUtility.ParseQueryString(string.Empty);
                outgoingQueryString.Add(nameof(facebookOauthResponse.access_token), facebookOauthResponse.access_token);
                outgoingQueryString.Add(nameof(facebookOauthResponse.token_type), facebookOauthResponse.token_type);
                var postdata = outgoingQueryString.ToString();

                var modifiedResult = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(postdata)
                };

                return modifiedResult;
            }
        }
    }
}