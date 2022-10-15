namespace Wilgysef.Stalk.Core.Shared.Options
{
    public class ExternalBinariesOptions : IOptionSection
    {
        public string Label => "ExternalBinaries";

        /// <summary>
        /// External binary path.
        /// </summary>
        public string? Path { get; set; }
    }
}
