using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Use this attribute to make an exception to the class naming rules (which should not be named like Interfaces).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExceptionToClassNameConventionAttribute : Attribute
    {
    }
}
