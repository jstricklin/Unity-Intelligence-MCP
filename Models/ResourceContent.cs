using System;
using System.IO;

namespace UnityIntelligenceMCP.Models
{
    public class ResourceContent
    {
        public Stream DataStream { get; }
        public Type TargetType { get; }

        public ResourceContent(Stream dataStream, Type targetType)
        {
            DataStream = dataStream;
            TargetType = targetType;
        }
    }
}