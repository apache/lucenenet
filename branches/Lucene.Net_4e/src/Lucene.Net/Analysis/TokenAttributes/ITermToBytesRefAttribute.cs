// -----------------------------------------------------------------------
// <copyright company="Apache" file="ITermToBytesRefAttribute.cs">
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



namespace Lucene.Net.Analysis.TokenAttributes
{
    using Lucene.Net.Index;
    using Lucene.Net.Util;

    /// <summary>
    /// This attribute is requested by the <see cref="TermsHashPerField"/> 
    /// to index the contents. It can be used to customize the byte[] encoding of terms.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///     The expected use is to call <see cref="BytesRef"/> then invoke
    ///     <see cref="FillBytesRef"/> for each term. 
    ///     </para>
    ///     <note>
    ///     Experimental: This is a very expert API, please <see cref="ICharTermAttribute"/>
    ///     and its implementation of this method for UTF-8 terms.
    ///     </note>
    ///     <code lang="c#">
    ///         TermToBytesRefAttribute attribute = tokenStream.GetAttribute(typeof(TermToBytesRefAttribute));
    ///         BytesRef bytes = attribute.BytesRef;
    ///         
    ///         while(attribute.IncrementToken())
    ///         {
    ///             int hash attribute.FillBytesRef();
    ///             if (isInteresting(bytes)) 
    ///             {
    ///                 // do something with it.
    ///                 Use(new BytesRef(bytes));
    ///             }
    ///         }
    ///     </code>
    /// </remarks>
    public interface ITermToBytesRefAttribute
    {    
        /// <summary>
        /// Gets the <see cref="BytesRef"/>. The bytes are updated
        /// from the current term when the invoker calls <see cref="FillBytesRef"/>.
        /// </summary>
        /// <value>The bytes ref.</value>
        BytesRef BytesRef { get; }

        /// <summary>
        /// Updates the bytes <see cref="BytesRef"/> to contain 
        /// the term's final encoding. Then it returns its hashcode.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     Implement the following code for performance reasons, if
        ///     the code can calculate the has on-the-fly. If this is not the 
        ///     case, just return the <see cref="BytesRef"/>'s hashcode.
        ///     </para>
        ///     <code lang="c#">
        /// int hash = 0;
        /// for(int i = this.termBytes.Offset; i &lt; this.termBytes.Offset + this.termBytes.Length; i++)
        ///     hash = (31*hash) + this.termBytes.Bytes[i];
        ///     </code>
        /// </remarks>
        /// <returns>The hashcode from the <see cref="BytesRef"/>'s hashcode.</returns>
        int FillBytesRef();
    }
}
