namespace Lucene.Net.Support
{
    public class StringCharSequenceWrapper : ICharSequence
    {
        public static readonly StringCharSequenceWrapper Empty = new StringCharSequenceWrapper(string.Empty);

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

        // LUCENENET specific - added to .NETify
        public char this[int index]
        {
            get { return value[index]; }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            return new StringCharSequenceWrapper(value.Substring(start, end - start));
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            return value.Equals(obj);
        }

        public override string ToString()
        {
            return value;
        }
    }
}