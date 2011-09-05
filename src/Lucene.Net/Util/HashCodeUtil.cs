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
    /// TODO: Update summary.
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
        /// <typeparam name="T"></typeparam>
        /// <param name="slices">The slices.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="length">The length.</param>
        /// <param name="prime">The prime.</param>
        /// <param name="code">The code.</param>
        /// <returns>An instance of <see cref="Int32"/>.</returns>
        public static int GenerateHashCode<T>(T[] slices, int offset = 0, int length = 0, int prime = 31, int code = 0) where T : IConvertible
        {
            if (length == 0)
                length = slices.Length;

            int end = offset + length;

            for (int i = offset; i < end; i++)
            {
                code = (code * prime) + slices[i].ToInt32(null);
            }

            return code;
        }
    }
}
