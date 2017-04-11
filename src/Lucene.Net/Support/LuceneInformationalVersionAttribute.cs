using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Attribute for assigning and reading InformationalVersion, since <see cref="System.Reflection.AssemblyInformationalVersionAttribute"/> 
    /// is optimized away during compilation and cannot be read from .NET Core.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public class LuceneInformationalVersionAttribute : Attribute
    {
        public LuceneInformationalVersionAttribute(string version)
        {
            this.Version = version;
        }

        public string Version { get; private set; }
    }
}
