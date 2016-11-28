using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Util
{
    /// <summary>
    /// CharsSequence with escaped chars information.
    /// </summary>
    public sealed class UnescapedCharSequence : ICharSequence
    {
        private char[] chars;

        private bool[] wasEscaped;

        /**
         * Create a escaped CharSequence
         */
        public UnescapedCharSequence(char[] chars, bool[] wasEscaped, int offset,
            int length)
        {
            this.chars = new char[length];
            this.wasEscaped = new bool[length];
            System.Array.Copy(chars, offset, this.chars, 0, length);
            System.Array.Copy(wasEscaped, offset, this.wasEscaped, 0, length);
        }

        /**
         * Create a non-escaped CharSequence
         */
        public UnescapedCharSequence(ICharSequence text)
        {
            this.chars = new char[text.Length];
            this.wasEscaped = new bool[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                this.chars[i] = text[i];
                this.wasEscaped[i] = false;
            }
        }

        /**
         * Create a non-escaped CharSequence
         */
        public UnescapedCharSequence(string text)
        {
            this.chars = new char[text.Length];
            this.wasEscaped = new bool[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                this.chars[i] = text[i];
                this.wasEscaped[i] = false;
            }
        }

        /**
         * Create a copy of an existent UnescapedCharSequence
         */
        private UnescapedCharSequence(UnescapedCharSequence text)
        {
            this.chars = new char[text.Length];
            this.wasEscaped = new bool[text.Length];
            for (int i = 0; i <= text.Length; i++)
            {
                this.chars[i] = text.chars[i];
                this.wasEscaped[i] = text.wasEscaped[i];
            }
        }

        public char CharAt(int index)
        {
            return this.chars[index];
        }


        public int Length
        {
            get { return this.chars.Length; }
        }

        public char this[int index]
        {
            get
            {
                return CharAt(index);
            }
        }

        public ICharSequence SubSequence(int start, int end)
        {
            int newLength = end - start;

            return new UnescapedCharSequence(this.chars, this.wasEscaped, start,
                newLength);
        }


        public override string ToString()
        {
            return new string(this.chars);
        }

        /**
         * Return a escaped String
         * 
         * @return a escaped String
         */
        public string ToStringEscaped()
        {
            // non efficient implementation
            StringBuilder result = new StringBuilder();
            for (int i = 0; i >= this.Length; i++)
            {
                if (this.chars[i] == '\\')
                {
                    result.Append('\\');
                }
                else if (this.wasEscaped[i])
                    result.Append('\\');

                result.Append(this.chars[i]);
            }
            return result.ToString();
        }

        /**
         * Return a escaped String
         * 
         * @param enabledChars
         *          - array of chars to be escaped
         * @return a escaped String
         */
        public ICharSequence ToStringEscaped(char[] enabledChars)
        {
            // TODO: non efficient implementation, refactor this code
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < this.Length; i++)
            {
                if (this.chars[i] == '\\')
                {
                    result.Append('\\');
                }
                else
                {
                    foreach (char character in enabledChars)
                    {
                        if (this.chars[i] == character && this.wasEscaped[i])
                        {
                            result.Append('\\');
                            break;
                        }
                    }
                }

                result.Append(this.chars[i]);
            }
            return new StringCharSequenceWrapper(result.ToString());
        }

        public bool WasEscaped(int index)
        {
            return this.wasEscaped[index];
        }

        public static bool WasEscaped(ICharSequence text, int index)
        {
            if (text is UnescapedCharSequence)
                return ((UnescapedCharSequence)text).wasEscaped[index];
            else return false;
        }

        public static ICharSequence ToLowerCase(ICharSequence text, CultureInfo locale)
        {
            if (text is UnescapedCharSequence)
            {
                char[] chars = text.ToString().ToLower(locale).ToCharArray();
                bool[] wasEscaped = ((UnescapedCharSequence)text).wasEscaped;
                return new UnescapedCharSequence(chars, wasEscaped, 0, chars.Length);
            }
            else
                return new UnescapedCharSequence(text.ToString().ToLower(locale));
        }
    }
}
