using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Wilgysef.Stalk.Core.Shared.Downloaders
{
    public class DownloadRequestData
    {
        /// <summary>
        /// Download method.
        /// </summary>
        public HttpMethod? Method { get; set; }

        /// <summary>
        /// Headers.
        /// </summary>
        public List<KeyValuePair<string, string>>? Headers { get; set; }

        /// <summary>
        /// Request data.
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// Download request data.
        /// </summary>
        /// <param name="method">Download method.</param>
        /// <param name="headers">Headers.</param>
        /// <param name="data">Request data.</param>
        public DownloadRequestData(
            HttpMethod? method = null,
            List<KeyValuePair<string, string>>? headers = null,
            byte[]? data = null)
        {
            Method = method;
            Headers = headers;
            Data = data;
        }

        /// <summary>
        /// Creates an HTTP request message from the download request data.
        /// </summary>
        /// <param name="uri">URI.</param>
        /// <returns>HTTP request message.</returns>
        public HttpRequestMessage CreateRequest(Uri uri)
        {
            var request = new HttpRequestMessage(Method ?? HttpMethod.Get, uri);

            if (Headers != null)
            {
                foreach (var (header, value) in Headers)
                {
                    request.Headers.Add(header, value);
                }
            }

            if (Data != null)
            {
                request.Content = new ByteArrayContent(Data);
            }

            return request;
        }
    }
}
