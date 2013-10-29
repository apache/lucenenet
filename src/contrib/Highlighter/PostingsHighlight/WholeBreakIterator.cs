using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.PostingsHighlight
{
    public sealed class WholeBreakIterator : BreakIterator
    {
        private string text;
        private int start;
        private int end;
        private int current;

        public override int Current()
        {
            return current;
        }

        public override int First()
        {
            return (current = start);
        }

        public override int Following(int pos)
        {
            if (pos < start || pos > end)
            {
                throw new ArgumentException(@"offset out of bounds");
            }
            else if (pos == end)
            {
                current = end;
                return DONE;
            }
            else
            {
                return Last();
            }
        }

        public override string Text
        {
            get
            {
                return text;
            }
            set
            {
                start = 0;
                end = value.Length - 1;
                text = value;
                current = start;
            }
        }

        public override int Last()
        {
            return (current = end);
        }

        public override int Next()
        {
            if (current == end)
            {
                return DONE;
            }
            else
            {
                return Last();
            }
        }

        public override int Next(int n)
        {
            if (n < 0)
            {
                for (int i = 0; i < -n; i++)
                {
                    Previous();
                }
            }
            else
            {
                for (int i = 0; i < n; i++)
                {
                    Next();
                }
            }

            return Current();
        }

        public override int Preceding(int pos)
        {
            if (pos < start || pos > end)
            {
                throw new ArgumentException(@"offset out of bounds");
            }
            else if (pos == start)
            {
                current = start;
                return DONE;
            }
            else
            {
                return First();
            }
        }

        public override int Previous()
        {
            if (current == start)
            {
                return DONE;
            }
            else
            {
                return First();
            }
        }
    }
}
