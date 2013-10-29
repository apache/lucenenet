using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Support.BreakIterators
{
    /// <summary>
    /// A base implementation of BreakIterator to make some operations easier, particularly for 
    /// english or latin-based languages.
    /// </summary>
    // HACK: someone please improve this!
    public abstract class BreakIteratorBase : BreakIterator
    {
        protected const char ENDINPUT = '\0';

        protected string _text;
        protected int _position = DONE;

        public override int Current()
        {
            if (_position == DONE)
                return First();

            return _position;
        }

        public override int First()
        {
            _position = DONE;

            return Following(DONE);
        }

        public override int Following(int offset)
        {
            _position = offset;

            do
            {
                _position++;

                if (_position == _text.Length)
                    return DONE;
            }
            while (!IsBoundary(_position));

            return _position;
        }

        public override string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
            }
        }

        public override int Last()
        {
            _position = _text.Length;

            return Previous();
        }

        public override int Next()
        {
            if (_position == DONE)
                return First();

            if (_position == _text.Length - 1)
                return DONE;

            return Following(_position);
        }

        public override int Next(int n)
        {
            if (_position == DONE)
                return n == 0 ? First() : Following(n - 1);

            if (n == 0)
                return Current();

            if (_position + n >= _text.Length)
                return DONE;

            _position += (n - 1);
            return Following(_position);
        }

        public override int Previous()
        {
            do
            {
                _position--;

                if (_position == DONE)
                    return DONE;
            }
            while (!IsBoundary(_position));

            return _position;
        }

        public abstract override bool IsBoundary(int offset);

        public virtual char Peek(int offset)
        {
            if (offset < 0 || offset >= _text.Length)
                return ENDINPUT;

            return _text[offset];
        }
    }
}
