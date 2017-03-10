using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Use this attribute to make an exception to the nullable enum rule.
    /// Some of these cannot be avoided.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Constructor, AllowMultiple = false)]
    public class ExceptionToNullableEnumConvention : Attribute
    {
    }
}
