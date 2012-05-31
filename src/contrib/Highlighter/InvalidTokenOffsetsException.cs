using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Lucene.Net.Search.Highlight
{
    [Serializable]
    public class InvalidTokenOffsetsException : Exception
    {
        public InvalidTokenOffsetsException()
        {
        }

        public InvalidTokenOffsetsException(string message) : base(message)
        {
        }

        public InvalidTokenOffsetsException(string message, Exception inner) : base(message, inner)
        {
        }

        protected InvalidTokenOffsetsException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
