using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public interface IBytesRefIterator
    {
        BytesRef Next();

        public IComparer<BytesRef> Comparator { get; }
    }

    // .NET Port: in Java, you can have static fields and anonymous classes inside interfaces.
    // Here, we're naming a static class similarly to the Java interface to mimic this behavior.
    public static class BytesRefIterator
    {
        public static readonly IBytesRefIterator EMPTY = new EmptyBytesRefIterator();

        private class EmptyBytesRefIterator : IBytesRefIterator
        {
            public BytesRef Next()
            {
                return null;
            }

            public IComparer<BytesRef> Comparator
            {
                get { return null; }
            }
        }

    }
}
