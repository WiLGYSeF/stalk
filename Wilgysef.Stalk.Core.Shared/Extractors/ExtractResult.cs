using System;
using Wilgysef.Stalk.Core.Shared.Enums;
using Wilgysef.Stalk.Core.Shared.MetadataObjects;

namespace Wilgysef.Stalk.Core.Shared.Extractors
{
    public class ExtractResult
    {
        public string Name { get; }

        public int? Priority { get; }

        public Uri Uri { get; }

        public string ItemId { get; }

        public string ItemData { get; }

        public IMetadataObject Metadata { get; }

        public JobTaskType Type { get; }
    }
}
