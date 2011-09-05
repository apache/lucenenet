// -----------------------------------------------------------------------
// <copyright file="Tokenizer.cs" company="Microsoft">
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
    using Lucene.Net.Util;

    /// <summary>
    /// The abstract class which will perform an lexical analysis that will transform sequence of 
    /// characters into a sequence of tokens.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The abstract <c>Tokenizer</c> class in Lucene.Net is essentially a <see cref="TokenStream"/>
    ///         that has an internal <see cref="StreamReader"/>.
    ///     </para>
    ///     <note>
    ///         Subclasses must override <see cref="TokenStream.IncrementToken"/> and 
    ///         <see cref="TokenStream.IncrementToken"/> must call <see cref="AttributeSource.ClearAttributes"/>
    ///         before setting attributes.
    ///     </note>
    /// </remarks>
    public abstract class Tokenizer : TokenStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        protected Tokenizer()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        protected Tokenizer(StreamReader reader)
        {
            this.Reader = CharReader.CastOrCreate(reader);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        protected Tokenizer(AttributeFactory factory)
            : base(factory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tokenizer"/> class.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="reader">The reader.</param>
        protected Tokenizer(AttributeFactory factory, StreamReader reader)
            : base(factory)
        {
            this.Reader = CharReader.CastOrCreate(reader);
        }

        /// <summary>
        /// Gets or sets the reader.
        /// </summary>
        /// <value>The reader.</value>
        protected TextReader Reader { get; set; }

        /// <summary>
        /// Resets the specified reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public void Reset(StreamReader reader)
        {
            this.Reader = reader;
        }

        /// <summary>
        /// Corrects and returns the corrected offset.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     If the <see cref="Reader"/> of the Tokenizer is an instance of <see cref="CharStream"/>
        ///     then this method will call <see cref="CharStream.CorrectOffset"/> otherwise it will
        ///     return the value of the parameter <paramref name="offset"/>.
        ///     </para>
        /// </remarks>
        /// <param name="offset">The offset as seen in the output.</param>
        /// <returns>The corrected offset based on the input.</returns>
        protected int CorrectOffset(int offset)
        {
            var charStream = this.Reader as CharStream;

            if (charStream != null)
                return charStream.CorrectOffset(offset);

            return offset;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="release"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool release)
        {
            //// LUCENE-2387: don't hold onto Reader after close, so
            //// GC can reclaim
            if (this.Reader != null)
            {
                this.Reader.Dispose();
                this.Reader = null;
            }
        }
    }
}
