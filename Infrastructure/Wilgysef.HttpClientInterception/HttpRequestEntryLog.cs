using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Wilgysef.HttpClientInterception
{
    public class HttpRequestEntryLog
    {
        public IList<HttpRequestEntry> Entries => _entries;

        private readonly List<HttpRequestEntry> _entries = new List<HttpRequestEntry>();

        public void AddEntry(HttpRequestEntry entry)
        {
            _entries.Add(entry);
        }

        public bool SetEntryResponse(HttpRequestMessage request, HttpResponseMessage response, DateTimeOffset responseTime)
        {
            var found = false;

            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                var entry = _entries[i];
                if (entry.Request == request)
                {
                    entry.SetResponse(response, responseTime);
                    found = true;
                    break;
                }
            }

            return found;
        }
    }
}
