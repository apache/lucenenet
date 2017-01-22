using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Attribute to define a property as a writable array.
    /// Per MSDN, properties should never return arrays because arrays
    /// can be updated, which makes the behavior confusing. However,
    /// Lucene's design sometimes relies on other classes to update arrays -
    /// both as array fields and as methods that return arrays. So, in these
    /// cases we are making an exception to this rule and marking them with
    /// [WritableArray] to signify that this is intentional.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false)]
    public class WritableArrayAttribute : Attribute
    {
    }
}
