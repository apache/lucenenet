// -----------------------------------------------------------------------
// <copyright file="CharSequenceExtensions.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Support
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Extension and static methods for the ICharSequence interface. 
    /// </summary>
    internal static class CharSequenceExtensions
    {
        public static bool IsCharSequence(this object value)
        {
            return value is ICharSequence || value is string || value is IEnumerable<char>;
        }

        internal static int CreateHashCode(ICharSequence value)
        {
            int result = 0;
            int end = value.Length;
            for (int i = 0; i < end; i++)
                result = (31 * result) + value.CharAt(i);

            return result;
        }

        internal static bool IsCharSequenceEqual(ICharSequence value, object obj)
        {
            ICharSequence sequence = obj as ICharSequence;
            if (sequence == null)
                return false;

            if (value.Length != sequence.Length)
                return false;

            for (int i = 0; i < value.Length; i++)
            {
                if (value.CharAt(i) != sequence.CharAt(i))
                    return false;
            }

            return true;
        }
    }
}
