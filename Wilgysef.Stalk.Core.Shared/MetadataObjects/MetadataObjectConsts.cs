namespace Wilgysef.Stalk.Core.Shared.MetadataObjects
{
    public static class MetadataObjectConsts
    {
        public static class File
        {
            public static readonly string[] FilenameTemplateKeys = new[] { "file", "filename_template" };

            public static readonly string[] HashKeys = new[] { "file", "hash" };

            public static readonly string[] HashAlgorithmKeys = new[] { "file", "hash_algorithm" };

            public static readonly string[] SizeKeys = new[] { "file", "size" };
        }

        public static readonly string[] MetadataFilenameTemplateKeys = new[] { "metadata_filename_template" };

        public static class Origin
        {
            public static readonly string[] ItemIdKeys = new[] { "origin", "item_id" };

            public static readonly string[] UriKeys = new[] { "origin", "uri" };
        }

        public static readonly string[] RetrievedKeys = new[] { "retrieved" };
    }
}
