namespace Wilgysef.Stalk.Core.Shared.Options
{
    public class ExtractorsOptions : IOptionSection
    {
        public string Label => "Extractors";

        public string? Path { get; set; }
    }
}
