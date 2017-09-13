using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace SophtronAlexaSkill
{
    public class AuthorizationHelper
    {
        public static string GetAuthPhrase(HttpRequestMessage request, string key)
        {
            IEnumerable<string> keys = null;
            if (request.Headers.TryGetValues(key, out keys) && keys != null)
            {
                return keys.First();
            }

            HttpCookie auth;
            if (HttpContext.Current != null &&
                HttpContext.Current.Request != null &&
                HttpContext.Current.Request.Cookies != null &&
                null != (auth = HttpContext.Current.Request.Cookies.Get(key)))
            {
                return HttpUtility.UrlDecode(auth.Value);
            }

            return null;
        }
    }
}