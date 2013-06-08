using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support
{
    public class StringCharSequenceWrapper : ICharSequence
    {
        private readonly string value;

        public StringCharSequenceWrapper(string wrappedValue)
        {
            value = wrappedValue;
        }

        public int Length
        {
            get { return value.Length; }
        }

        public char CharAt(int index)
        {
            return value[index];
        }

        public ICharSequence SubSequence(int start, int end)
        {
            return new StringCharSequenceWrapper(value.Substring(start, end));
        }

        public override string ToString()
        {
            return value;
        }
    }
}
