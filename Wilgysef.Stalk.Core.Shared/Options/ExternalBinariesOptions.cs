using System.Collections.Generic;

namespace Wilgysef.Stalk.Core.Shared.Options
{
    public class ExternalBinariesOptions : IOptionSection
    {
        public string Label => "ExternalBinaries";

        public string? Path { get; set; }
    }
}
