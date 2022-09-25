using System;
using System.Collections.Specialized;
using System.Web;

namespace Wilgysef.Stalk.TestBase.Utilities
{
    public static class UriExtensions
    {
        public static NameValueCollection GetQueryParameters(this Uri uri)
        {
            return HttpUtility.ParseQueryString(uri.Query);
        }
    }
}
