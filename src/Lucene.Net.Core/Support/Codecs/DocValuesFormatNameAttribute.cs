using Lucene.Net.Util;
using System;

namespace Lucene.Net.Codecs
{
    /// <summary>
    /// Represents an attribute that is used to name a <see cref="DocValuesFormat"/>, if a name
    /// other than the default <see cref="DocValuesFormat"/> naming convention is desired.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DocValuesFormatNameAttribute : ServiceNameAttribute
    {
        public DocValuesFormatNameAttribute(string name)
            : base(name)
        {
        }
    }
}
