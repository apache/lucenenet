using System.Globalization;
using System.Text;

namespace Lucene.Net.Support
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder Reverse(this StringBuilder text)
        {
            int textLength = text.Length;
            if (textLength > 1)
            {
                // Pull the string out of the StringBuilder so we
                // can work with the various text elements (chars, glyphs, graphemes, etc)
                // and reverse the order of the string without reversing chars that need to be
                // in a specific order to represent the same text as the forward string.
                // Reference: http://stackoverflow.com/a/36310993/181087
                int offset = textLength;
                var enumerator = StringInfo.GetTextElementEnumerator(text.ToString());
                while (enumerator.MoveNext())
                {
                    string element = enumerator.GetTextElement();

                    // Back up the current offset by the length of the element
                    offset -= element.Length;

                    for (int i = 0; i < element.Length; i++)
                    {
                        // Write the chars in forward order from the element
                        // to the StringBuilder based on the offset.
                        text[i + offset] = element[i];
                    }
                }
            }

            return text;
        }

        /// <summary>
        /// Appends the string representation of the <paramref name="codePoint"/>
        /// argument to this sequence.
        /// 
        /// <para>
        /// The argument is appended to the contents of this sequence.
        /// The length of this sequence increases by <see cref="Character.CharCount(int)"/>.
        /// </para>
        /// <para>
        /// The overall effect is exactly as if the argument were
        /// converted to a <see cref="char"/> array by the method
        /// <see cref="Character.ToChars(int)"/> and the character in that array
        /// were then <see cref="StringBuilder.Append(char[])">appended</see> to this 
        /// <see cref="StringBuilder"/>.
        /// </para>
        /// </summary>
        /// <param name="text">This <see cref="StringBuilder"/>.</param>
        /// <param name="codePoint">a Unicode code point</param>
        /// <returns>a reference to this object.</returns>
        public static StringBuilder AppendCodePoint(this StringBuilder text, int codePoint)
        {
            text.Append(Character.ToChars(codePoint));
            return text;
        }
    }
}