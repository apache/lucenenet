// -----------------------------------------------------------------------
// <copyright file="HashCodeUtil.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Utility class for generating hash codes using 31 as the default prime. 
    /// </summary>
    public class HashCodeUtil
    {
        /// <summary>
        /// Generates the hash code.
        /// </summary>
        /// <param name="append">The append.</param>
        /// <param name="initial">The initial.</param>
        /// <param name="prime">The prime.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public static int GenerateHashCode(int append, int initial = 0, int prime = 31)
        {
            int code = initial;
            code = (code * prime) + append;
            return code;
        }

        /// <summary>
        /// Generates the hash code.
        /// </summary>
        /// <typeparam name="T">The type of the array.</typeparam>
        /// <param name="slices">The slices.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="prime">The prime.</param>
        /// <param name="code">The code.</param>
        /// <remarks>
        ///     <para>
        ///     Unfortunately using the <see cref="IConvertible"/> interface to constrain <typeparamref name="T"/>
        ///     to types that could be converted to <see cref="int"/> would cause Lucene.Net to lose 
        ///     CLS-compliance.  And Microsoft will not create another interface to fix the issue:
        ///      http://connect.microsoft.com/VisualStudio/feedback/details/308902/iconvertable-breaks-cls-compliance-for-classes-that-have-generic-constraints-including-iconvertable
        ///     </para>
        /// </remarks>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        /// <exception cref="InvalidCastException">
        ///     Thrown when items of the array of <typeparamref name="T"/> can not be
        ///     converted to Int32. 
        /// </exception>
        public static int GenerateHashCode<T>(T[] slices, int offset = 0, int length = 0, int prime = 31, int code = 0) 
        {
            if (length == 0)
                length = slices.Length;

            int end = offset + length;

            for (int i = offset; i < end; i++)
            {
                code = (code * prime) + Convert.ToInt32(slices[i]);
            }

            return code;
        }
    }
}
