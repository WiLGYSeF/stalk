namespace Wilgysef.Stalk.Core.Shared.MetadataObjects
{
    public class MetadataObjectConsts
    {
        public string FileFilenameTemplateKey => JoinKeyParts("file", "filenameTemplate");

        public string FileHashKey => JoinKeyParts("file", "hash");

        public string FileHashAlgorithmKey => JoinKeyParts("file", "hashAlgorithm");

        public string FileSizeKey => JoinKeyParts("file", "size");

        public string MetadataFilenameTemplateKey => JoinKeyParts("metadataFilenameTemplate");

        public string OriginItemIdKey => JoinKeyParts("origin", "itemId");

        public string OriginUriKey => JoinKeyParts("origin", "uri");

        public string RetrievedKey => JoinKeyParts("retrieved");

        private char _keySeparator = '.';

        public char KeySeparator
        {
            get => _keySeparator;
            set
            {
                _keySeparator = value;
                _keySeparatorString = null;
            }
        }

        private string? _keySeparatorString = null;

        private string KeySeparatorString
        {
            get
            {
                _keySeparatorString ??= KeySeparator.ToString();
                return _keySeparatorString;
            }
        }

        public MetadataObjectConsts(char keySeparator)
        {
            KeySeparator = keySeparator;
        }

        private string JoinKeyParts(params string[] parts)
        {
            return string.Join(KeySeparatorString, parts);
        }
    }
}
