using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Properties, methods, or events marked with this attribute can ignore
    /// the numeric naming conventions of "Int16", "Int32", "Int64", and "Single"
    /// that are commonly used in .NET method and property names.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event, AllowMultiple = false)]
    public class ExceptionToNetNumericConventionAttribute : Attribute
    {
    }
}
