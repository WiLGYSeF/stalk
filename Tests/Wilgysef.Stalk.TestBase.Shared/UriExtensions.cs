using System;
using System.Collections.Specialized;
using System.Web;

namespace Wilgysef.Stalk.TestBase.Shared
{
    public static class UriExtensions
    {
        public static NameValueCollection? GetQueryParameters(this Uri? uri)
        {
            return uri != null
                ? HttpUtility.ParseQueryString(uri.Query)
                : null;
        }
    }
}
