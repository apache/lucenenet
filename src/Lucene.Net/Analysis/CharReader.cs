// -----------------------------------------------------------------------
// <copyright file="CharReader.cs" company="Microsoft">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Analysis
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
 

    /// <summary>
    /// <see cref="CharReader"/> is wrapper for <see cref="StreamReader"/> that
    /// reads in <c>char[]</c> and outputs a <see cref="CharStream"/>.
    /// </summary>
    /// <seealso cref="CharStream"/>
    public sealed class CharReader : CharStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        private CharReader(StreamReader reader)
            : base(reader)
        { 
        }

        /// <summary>
        /// Casts or creates a new CharReader
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>
        /// An instance of <see cref="CharReader"/>.
        /// </returns>
        public static CharReader CastOrCreate(StreamReader reader)
        {
            if (reader is CharReader)
                return (CharReader)reader;

            return new CharReader(reader);
        }
      
        /// <summary>
        /// Corrects the offset.
        /// </summary>
        /// <param name="offset">The offset for the output.</param>
        /// <returns>
        /// The <see cref="Int32"/> offset based on the input.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         <see cref="CorrectOffset"/> fixes offsets to account for
        ///         removal or insertion of characters, so that the offsets
        ///         reported in the tokens match the character offsets of the
        ///         original Reader.
        ///     </para>
        ///     <para>
        ///         <see cref="CorrectOffset"/> is generally invoked by <c>Tokenizer</c> classes
        ///         and <c>CharFilter</c> classes.
        ///     </para>
        /// </remarks>
        public override int CorrectOffset(int offset)
        {
            return offset;
        }

        /// <summary>
        /// When it is called by trusted applications, reads a maximum of <paramref name="count"/> characters from the current stream into <paramref name="buffer"/>, beginning at <paramref name="index"/>.
        /// </summary>
        /// <param name="buffer">When this method returns, contains the specified character array with the values between <paramref name="index"/> and (<paramref name="index "/>+ <paramref name="count"/> - 1) replaced by the characters read from the current source.</param>
        /// <param name="index">The index of <paramref name="buffer"/> at which to begin writing.</param>
        /// <param name="count">The maximum number of characters to read.</param>
        /// <returns>
        /// The number of characters that have been read, or 0 if at the end of the stream and no data was read. The number will be less than or equal to the <paramref name="count"/> parameter, depending on whether the data is available within the stream.
        /// </returns>
        /// <exception cref="T:System.ArgumentException">The buffer length minus <paramref name="index"/> is less than <paramref name="count"/>. </exception>
        /// <exception cref="T:System.ArgumentNullException">
        ///     <paramref name="buffer"/> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        ///     <paramref name="index"/> or <paramref name="count"/> is negative. </exception>
        /// <exception cref="T:System.IO.IOException">An I/O error occurs, such as the stream is closed. </exception>
        public override int Read(char[] buffer, int index, int count)
        {
            return this.InnerReader.Read(buffer, index, count);
        }
    }
}
