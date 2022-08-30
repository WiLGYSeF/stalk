namespace Wilgysef.Stalk.Core.MetadataObjects;

public class MetadataObjectConsts
{
    public string FilenameTemplateKey => JoinKeyParts("filenameTemplate");

    public string MetadataFilenameTemplateKey => JoinKeyParts("metadataFilenameTemplate");

    public string OriginItemId => JoinKeyParts("origin", "itemId");

    public string OriginUri => JoinKeyParts("origin", "uri");

    public string RetrievedKey => JoinKeyParts("retrieved");

    public char KeySeparator { get; set; }

    public MetadataObjectConsts(char keySeparator)
    {
        KeySeparator = keySeparator;
    }

    private string JoinKeyParts(params string[] parts)
    {
        return string.Join(KeySeparator, parts);
    }
}
