using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Lucene.Net.Support
{
    /// <summary>
    /// A generic convenience wrapper for WeakReference.
    /// </summary>
    /// <typeparam name="T">Type of object to create a WeakReference to</typeparam>
    internal class WeakReference<T> : WeakReference
    {
        public WeakReference(T target) : base(target)
        { }

        public WeakReference(T target, bool trackResurrection) : base(target, trackResurrection)
        { }

        public new T Target
        {
            get
            {
                return (T)base.Target;
            }
            set
            {
                base.Target = value;
            }
        }
    }
}
