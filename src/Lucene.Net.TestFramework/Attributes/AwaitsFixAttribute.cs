using System;
using NUnit.Framework;

namespace Lucene.Net.Attributes
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = false)]    
    public class AwaitsFixAttribute : ExplicitAttribute
    {
        public AwaitsFixAttribute() : base("Awaits fix")
        {
            
        }

        /// <summary>
        /// Point to JIRA / GitHub entry.
        /// </summary>
        public string BugUrl { get; set; }
    }
}
