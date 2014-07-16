

namespace Lucene.Net.Support
{
    using System;

#if PORTABLE || K10

    /// <summary> 
    ///  Indicates that a class can be serialized. This class cannot be inherited. 
    ///  This is a placeholder for the SerializableAttribute that is not present in smaller
    ///  version of the .NET framework.
    /// <summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SerializableAttribute : Attribute
    {
    }

#endif 
}
