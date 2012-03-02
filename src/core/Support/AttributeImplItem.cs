using System;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A simple wrapper to allow for the use of the GeneralKeyedCollection.  The
    /// wrapper is required as there can be several keys for an object depending
    /// on how many interfaces it implements.
    /// </summary>
    internal sealed class AttributeImplItem
    {
        internal AttributeImplItem(Type key, Util.AttributeImpl value)
        {
            this.Key = key;
            this.Value = value;
        }
        internal Type Key;
        internal Util.AttributeImpl Value;
    }
}