// -----------------------------------------------------------------------
// <copyright company="Apache" file="CharStream.cs">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
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
    /// <see cref="CharStream"/> extends <see cref="TextReader"/> in order
    /// to enforce an extra method <see cref="CorrectOffset"/>. All tokenizers
    /// accept a <see cref="CharStream"/> instead of <see cref="TextReader"/> for 
    /// this reason.  
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The <see cref="CharStream"/> enables arbitrary character based 
    ///         filtering before tokenization. 
    ///     </para>
    ///     <note>
    ///         The following methods, <see cref="Mark"/>, <see cref="MarkSupported"/>,
    ///         and <see cref="Reset"/> were added to this abstract class as virtual methods
    ///         in case they were expected on all <see cref="CharStream"/> instances
    ///         due to Java's Reader implementation.  
    ///     </note>
    /// </remarks>
    /// <seealso cref="CorrectOffset"/>
    public abstract class CharStream : System.IO.StreamReader
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharStream"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        protected CharStream(StreamReader reader)
            : base(reader.BaseStream)
        {
            this.InnerReader = reader;
            this.CurrentPosition = -1;
        }

        /// <summary>
        /// Gets or sets the current position.
        /// </summary>
        /// <value>The current position.</value>
        protected long CurrentPosition { get; set; }

        /// <summary>
        /// Gets or sets the inner reader.
        /// </summary>
        /// <value>The inner reader.</value>
        protected StreamReader InnerReader { get; set; }

        /// <summary>
        /// Corrects the offset.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///        <see cref="CorrectOffset"/> fixes offsets to account for
        ///         removal or insertion of characters, so that the offsets
        ///         reported in the tokens match the character offsets of the
        ///         original Reader.
        ///     </para>
        ///     <para>
        ///         <see cref="CorrectOffset"/> is generally invoked by <c>Tokenizer</c> classes
        ///         and <c>CharFilter</c> classes.
        ///     </para>
        /// </remarks>
        /// <param name="offset">The offset for the output.</param>
        /// <returns>The <see cref="Int32"/> offset based on the input.</returns>
        public abstract int CorrectOffset(int offset);


        /// <summary>
        /// Closes this instance.
        /// </summary>
        public virtual void Close()
        {
            //// TODO: call close when the PLT finally supports it.
            this.InnerReader.DiscardBufferedData();
        }

        /// <summary>
        /// Determines if the stream supports Marking/Seeking ahead.
        /// </summary>
        /// <seealso cref="Stream.CanSeek"/>
        /// <returns>An instance of <see cref="Boolean"/>.</returns>
        public virtual bool MarkSupported()
        {
            return this.InnerReader.BaseStream.CanSeek;
        }

        /// <summary>
        /// Marks the seek position in the stream.
        /// </summary>
        /// <param name="readAheadLimit">The limit of characters to read ahead.</param>
        public virtual void Mark(int readAheadLimit)
        {
           this.CurrentPosition = this.InnerReader.BaseStream.Position;
           this.InnerReader.BaseStream.Position = readAheadLimit;
        }

        /// <summary>
        /// Resets the stream's position to the original position after
        /// <see cref="Mark"/> has been called.
        /// </summary>
        public virtual void Reset()
        {
            this.InnerReader.BaseStream.Position = this.CurrentPosition;
        }
    }
}