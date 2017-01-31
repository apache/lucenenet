using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Attribute to define a property or method as a writable array.
    /// Per MSDN, members should never return arrays because the array contents
    /// can be updated, which makes the behavior confusing. However,
    /// Lucene's design sometimes relies on other classes to update arrays -
    /// both as array fields and as methods that return arrays. So, in these
    /// cases we are making an exception to this rule and marking them with
    /// <see cref="WritableArrayAttribute"/> to signify that this is intentional.
    /// <para/>
    /// For properties that violate this rule, you should also use
    /// the <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>:
    /// <code>
    /// [WritableArray, SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    public class WritableArrayAttribute : Attribute 
    {
    }
}
