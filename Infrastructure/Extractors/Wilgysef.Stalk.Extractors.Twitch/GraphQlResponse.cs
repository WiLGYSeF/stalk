using Newtonsoft.Json.Linq;

namespace Wilgysef.Stalk.Extractors.Twitch;

internal class GraphQlResponse
{
    public string Operation { get; }

    public JToken Data { get; }

    public GraphQlResponse(string operation, JToken data)
    {
        Operation = operation;
        Data = data;
    }
}
