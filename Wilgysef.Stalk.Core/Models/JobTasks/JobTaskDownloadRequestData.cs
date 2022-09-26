using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Wilgysef.Stalk.Core.Shared.Downloaders;

namespace Wilgysef.Stalk.Core.Models.JobTasks;

[Owned]
public class JobTaskDownloadRequestData
{
    public virtual string? Method { get; set; }

    public virtual string? Headers { get; set; }

    public virtual byte[]? Data { get; set; }

    [NotMapped]
    public List<KeyValuePair<string, string>> HeadersList => GetHeaders(Headers);

    protected JobTaskDownloadRequestData() { }

    public static JobTaskDownloadRequestData Create(
        string? method = null,
        List<KeyValuePair<string, string>>? headers = null,
        byte[]? data = null)
    {
        return new JobTaskDownloadRequestData
        {
            Method = method,
            Headers = headers != null ? GetHeaders(headers) : null,
            Data = data
        };
    }

    public static implicit operator DownloadRequestData(JobTaskDownloadRequestData requestData)
    {
        return new DownloadRequestData(
            requestData.Method != null ? new HttpMethod(requestData.Method) : null,
            requestData.HeadersList,
            requestData.Data);
    }

    private static string GetHeaders(List<KeyValuePair<string, string>> headers)
    {
        var builder = new StringBuilder();

        foreach (var (header, value) in headers)
        {
            builder.Append(header.Length);
            builder.Append(':');
            builder.Append(header);
            builder.Append(value.Length);
            builder.Append(':');
            builder.Append(value);
        }

        return builder.ToString();
    }

    private static List<KeyValuePair<string, string>> GetHeaders(string? headers)
    {
        var headerList = new List<KeyValuePair<string, string>>();
        if (headers == null)
        {
            return headerList;
        }

        for (var index = 0; index < headers.Length; )
        {
            var endIndex = TryParseInt(headers, index, out var headerLength);
            if (endIndex == index || !headerLength.HasValue)
            {
                throw new ArgumentException("Invalid headers format.");
            }

            endIndex++;
            var header = headers[endIndex..(endIndex + headerLength.Value)];
            index = endIndex + headerLength.Value;

            endIndex = TryParseInt(headers, index, out var valueLength);
            if (endIndex == index || !valueLength.HasValue)
            {
                throw new ArgumentException("Invalid headers format.");
            }

            endIndex++;
            var value = headers[endIndex..(endIndex + valueLength.Value)];
            index = endIndex + valueLength.Value;

            headerList.Add(new KeyValuePair<string, string>(header, value));
        }

        return headerList;
    }

    private static int TryParseInt(string value, int index, out int? intValue)
    {
        var endIndex = index;
        while (endIndex < value.Length && char.IsDigit(value[endIndex]))
        {
            endIndex++;
        }

        if (endIndex > index)
        {
            intValue = int.Parse(value[index..endIndex]);
        }
        else
        {
            intValue = 0;
        }
        return endIndex;
    }
}

/*
 * User-Agent: asfasdf gsf
 * Set-Cookie: asd fgq egedwqtehtrg
 * Accept: atyyjng/fegret
 * Set-Cookie: 565y6ujygffret hg
 */


/*
 * 10:User-Agent11:asfasdf gsf10:Set-Cookie20:asd fgq egedwqtehtrg
 * Accept: atyyjng/fegret
 * Set-Cookie: 565y6ujygffret hg
 */
