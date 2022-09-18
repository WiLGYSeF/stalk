using System.IO;

namespace Wilgysef.Stalk.Core.Shared.MetadataSerializers
{
    public interface IMetadataSerializer
    {
        public string Serialize(object obj);

        public void Serialize(TextWriter stream, object obj);
    }
}
