using Wilgysef.Stalk.Core.Shared.Dependencies;
using Wilgysef.Stalk.Core.Shared.MetadataSerializers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Wilgysef.Stalk.Core.MetadataSerializers;

public class MetadataSerializer : IMetadataSerializer, ITransientDependency
{
    public string Serialize(object obj)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Serialize(obj);
    }

    public void Serialize(TextWriter stream, object obj)
    {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        serializer.Serialize(stream, obj);
    }
}
