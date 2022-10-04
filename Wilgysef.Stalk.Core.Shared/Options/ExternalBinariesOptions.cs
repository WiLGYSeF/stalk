namespace Wilgysef.Stalk.Core.Shared.Options
{
    internal class ExternalBinariesOptions : IOptionSection
    {
        public string Label => "ExternalBinaries";

        public string? Path { get; set; }
    }
}
