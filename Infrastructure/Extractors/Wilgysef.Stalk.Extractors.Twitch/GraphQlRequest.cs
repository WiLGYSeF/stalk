using System.Dynamic;

namespace Wilgysef.Stalk.Extractors.Twitch;

internal class GraphQlRequest
{
    public string Operation { get; }

    public object Variables { get; }

    public string Sha256Hash { get; }

    public int Version { get; }

    public GraphQlRequest(
        string operation,
        object variables,
        string sha256Hash,
        int version = 1)
    {
        Operation = operation;
        Variables = variables;
        Sha256Hash = sha256Hash;
        Version = version;
    }

    public object GetRequest()
    {
        return new
        {
            extensions = new
            {
                persistedQuery = new
                {
                    sha256Hash = Sha256Hash,
                    version = Version
                }
            },
            operationName = Operation,
            variables = Variables
        };
    }

    #region GraphQl Operations

    public static GraphQlRequest FilterableVideoTower_Videos(string channelName, string? cursor)
    {
        dynamic variables = new ExpandoObject();
        variables.broadcastType = (object?)null;
        variables.channelOwnerLogin = channelName;
        variables.limit = 30;
        variables.videoSort = "TIME";

        if (cursor != null)
        {
            variables.cursor = cursor;
        }

        return new GraphQlRequest(
            "FilterableVideoTower_Videos",
            variables,
            "a937f1d22e269e39a03b509f65a7490f9fc247d7f83d6ac1421523e3b68042cb");
    }

    public static GraphQlRequest ChannelVideoCore(string videoId)
    {
        var variables = new
        {
            videoID = videoId,
        };

        return new GraphQlRequest(
            "ChannelVideoCore",
            variables,
            "cf1ccf6f5b94c94d662efec5223dfb260c9f8bf053239a76125a58118769e8e2");
    }

    public static GraphQlRequest VideoMetadata(string channelName, string videoId)
    {
        var variables = new
        {
            channelLogin = channelName,
            videoID = videoId,
        };

        return new GraphQlRequest(
            "VideoMetadata",
            variables,
            "49b5b8f268cdeb259d75b58dcb0c1a748e3b575003448a2333dc5cdafd49adad");
    }

    public static GraphQlRequest ClipsCards__User(string channelName, string? cursor)
    {
        dynamic variables = new ExpandoObject();
        variables.criteria = new
        {
            filter = "ALL_TIME",
        };
        variables.limit = 20;
        variables.login = channelName;

        if (cursor != null)
        {
            variables.cursor = cursor;
        }

        return new GraphQlRequest(
            "ClipsCards__User",
            variables,
            "b73ad2bfaecfd30a9e6c28fada15bd97032c83ec77a0440766a56fe0bd632777");
    }

    public static GraphQlRequest ChannelShell(string channelName)
    {
        var variables = new
        {
            login = channelName,
        };

        return new GraphQlRequest(
            "ChannelShell",
            variables,
            "580ab410bcd0c1ad194224957ae2241e5d252b2c5173d8e0cce9d32d5bb14efe");
    }

    public static GraphQlRequest EmotePicker_EmotePicker_UserSubscriptionProducts(string channelId)
    {
        var variables = new
        {
            channelOwnerID = channelId,
        };

        return new GraphQlRequest(
            "EmotePicker_EmotePicker_UserSubscriptionProducts",
            variables,
            "71b5f829a4576d53b714c01d3176f192cbd0b14973eb1c3d0ee23d5d1b78fd7e");
    }

    public static GraphQlRequest ClipsSocialShare(string clipSlug)
    {
        var variables = new
        {
            slug = clipSlug,
        };

        return new GraphQlRequest(
            "ClipsSocialShare",
            variables,
            "86533e14855999f00b4c700c3a73149f1ddb5a5948453c77defcb8350e8d108d");
    }

    public static GraphQlRequest ComscoreStreamingQueryClip(string clipSlug)
    {
        var variables = new
        {
            channel = "",
            clipSlug,
            isClip = true,
            isLive = false,
            isVodOrCollection = false,
            vodID = ""
        };

        return new GraphQlRequest(
            "ComscoreStreamingQuery",
            variables,
            "e1edae8122517d013405f237ffcc124515dc6ded82480a88daef69c83b53ac01");
    }

    public static GraphQlRequest ClipsBroadcasterInfo(string clipSlug)
    {
        var variables = new
        {
            slug = clipSlug,
        };

        return new GraphQlRequest(
            "ClipsBroadcasterInfo",
            variables,
            "ce258d9536360736605b42db697b3636e750fdb14ff0a7da8c7225bdc2c07e8a");
    }

    public static GraphQlRequest ClipsViewCount(string clipSlug)
    {
        var variables = new
        {
            slug = clipSlug,
        };

        return new GraphQlRequest(
            "ClipsViewCount",
            variables,
            "00209f168e946123d3b911544a57be26391306685e6cae80edf75cdcf55bd979");
    }

    public static GraphQlRequest ClipsCurator(string clipSlug)
    {
        var variables = new
        {
            slug = clipSlug,
        };

        return new GraphQlRequest(
            "ClipsCurator",
            variables,
            "769e99d9ac3f68e53c63dd902807cc9fbea63dace36c81643d776bcb120902e2");
    }

    public static GraphQlRequest VideoAccessToken_Clip(string clipSlug)
    {
        var variables = new
        {
            slug = clipSlug,
        };

        return new GraphQlRequest(
            "VideoAccessToken_Clip",
            variables,
            "36b89d2507fce29e5ca551df756d27c1cfe079e2609642b4390aa4c35796eb11");
    }

    #endregion
}
