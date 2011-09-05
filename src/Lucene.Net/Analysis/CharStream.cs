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

using System.IO;

namespace Lucene.Net.Analysis
{
    using System;
    using System.Collections.Generic;
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
    /// </remarks>
    /// <seealso cref="CorrectOffset"/>
    public abstract class CharStream : System.IO.TextReader
    {

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
    }
}