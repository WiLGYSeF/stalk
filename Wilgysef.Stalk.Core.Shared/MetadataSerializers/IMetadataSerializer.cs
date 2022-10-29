using System.IO;

namespace Wilgysef.Stalk.Core.Shared.MetadataSerializers
{
    public interface IMetadataSerializer
    {
        /// <summary>
        /// Serializes metadata.
        /// </summary>
        /// <param name="obj">Metadata.</param>
        /// <returns>Serialized metadata.</returns>
        public string Serialize(object obj);

        /// <summary>
        /// Serializes metadata.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="obj">Metadata.</param>
        public void Serialize(TextWriter stream, object obj);
    }
}
