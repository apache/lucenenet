using Lucene.Net.Util;
using System;

namespace Lucene.Net.Codecs
{
    /// <summary>
    /// Represents an attribute that is used to name a <see cref="Codec"/>, if a name
    /// other than the default <see cref="Codec"/> naming convention is desired.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CodecNameAttribute : ServiceNameAttribute
    {
        public CodecNameAttribute(string name)
            : base(name)
        {
        }
    }
}
