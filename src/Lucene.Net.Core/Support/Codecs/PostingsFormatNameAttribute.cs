using Lucene.Net.Util;
using System;

namespace Lucene.Net.Codecs
{
    /// <summary>
    /// Represents an attribute that is used to name a <see cref="PostingsFormat"/>, if a name
    /// other than the default <see cref="PostingsFormat"/> naming convention is desired.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class PostingsFormatNameAttribute : ServiceNameAttribute
    {
        public PostingsFormatNameAttribute(string name)
            : base(name)
        {
        }
    }
}
