using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Support
{
    public class StringCharacterIterator : CharacterIterator
    {
        private string text;
        private int begin;
        private int end;
        // invariant: begin <= pos <= end
        private int pos;


        public StringCharacterIterator(string text)
            : this(text, 0)
        {
        }

        public StringCharacterIterator(string text, int pos)
            : this(text, 0, text.Length, pos)
        {
        }

        public StringCharacterIterator(string text, int begin, int end, int pos)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            this.text = text;

            if (begin < 0 || begin > end || end > text.Length)
                throw new ArgumentException("Invalid substring range");

            if (pos < begin || pos > end)
                throw new ArgumentException("Invalid position");

            this.begin = begin;
            this.end = end;
            this.pos = pos;
        }

        public void SetText(string text)
        {
            if (text == null)
                throw new ArgumentNullException("text");
            this.text = text;
            this.begin = 0;
            this.end = text.Length;
            this.pos = 0;
        }

        public override char First()
        {
            pos = begin;
            return Current;
        }

        public override char Last()
        {
            if (end != begin)
            {
                pos = end - 1;
            }
            else
            {
                pos = end;
            }
            return Current;
        }

        public override char SetIndex(int position)
        {
            if (position < begin || position > end)
                throw new ArgumentException("Invalid index");
            pos = position;
            return Current;
        }

        public override char Current
        {
            get
            {
                if (pos >= begin && pos < end)
                {
                    return text[pos];
                }
                else
                {
                    return DONE;
                }
            }
        }

        public override char Next()
        {
            if (pos < end - 1)
            {
                pos++;
                return text[pos];
            }
            else
            {
                pos = end;
                return DONE;
            }
        }

        public override char Previous()
        {
            if (pos > begin)
            {
                pos--;
                return text[pos];
            }
            else
            {
                return DONE;
            }
        }


        public override int BeginIndex
        {
            get
            {
                return begin;
            }
        }

        public override int EndIndex
        {
            get
            {
                return end;
            }
        }

        public override int Index
        {
            get
            {
                return pos;
            }
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (!(obj is StringCharacterIterator))
            return false;

            StringCharacterIterator that = (StringCharacterIterator)obj;

            if (GetHashCode() != that.GetHashCode())
                return false;
            if (!text.Equals(that.text))
                return false;
            if (pos != that.pos || begin != that.begin || end != that.end)
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode() ^ pos ^ begin ^ end;
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}
