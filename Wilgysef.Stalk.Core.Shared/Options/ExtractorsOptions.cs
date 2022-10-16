using System.Collections.Generic;

namespace Wilgysef.Stalk.Core.Shared.Options
{
    public class ExtractorsOptions : IOptionSection
    {
        public string Label => "Extractors";

        /// <summary>
        /// Extractor assembly path.
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Extractor assembly paths.
        /// </summary>
        public IEnumerable<string>? Paths { get; set; }
    }
}
