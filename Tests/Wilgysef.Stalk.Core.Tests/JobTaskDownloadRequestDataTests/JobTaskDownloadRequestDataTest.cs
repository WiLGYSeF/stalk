using Shouldly;
using Wilgysef.Stalk.Core.Models.JobTasks;

namespace Wilgysef.Stalk.Core.Tests.JobTaskDownloadRequestDataTests;

public class JobTaskDownloadRequestDataTest
{
    [Fact]
    public void Get_Headers()
    {
        var headers = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("test", "abc"),
            new KeyValuePair<string, string>("asdf", "12345"),
            new KeyValuePair<string, string>("asdf", "abc"),
        };
        var data = JobTaskDownloadRequestData.Create(headers: headers);

        data.Headers.ShouldBe("4:test3:abc4:asdf5:123454:asdf3:abc");

        var headerList = data.HeadersList;
        headerList.Count.ShouldBe(headers.Count);
        for (var i = 0; i < headerList.Count; i++)
        {
            headerList[i].Key.ShouldBe(headers[i].Key);
            headerList[i].Value.ShouldBe(headers[i].Value);
        }
    }

    [Fact]
    public void Get_Headers_NonAscii()
    {
        var headers = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("test", "天使うと"),
            new KeyValuePair<string, string>("test", "\u0ed2\ua4b1\u00b7\u0020\uff9f"),
        };
        var data = JobTaskDownloadRequestData.Create(headers: headers);

        data.Headers.ShouldBe("4:test4:天使うと4:test5:໒꒱· ﾟ");

        var headerList = data.HeadersList;
        headerList.Count.ShouldBe(headers.Count);
        for (var i = 0; i < headerList.Count; i++)
        {
            headerList[i].Key.ShouldBe(headers[i].Key);
            headerList[i].Value.ShouldBe(headers[i].Value);
        }
    }
}
