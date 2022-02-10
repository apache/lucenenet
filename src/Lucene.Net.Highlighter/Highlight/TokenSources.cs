using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Highlight
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    ///<summary>
    /// Hides implementation issues associated with obtaining a <see cref="TokenStream"/> for use with 
    /// the <see cref="Highlighter"/> - can obtain from
    /// term vectors with offsets and positions or from an Analyzer re-parsing the stored content.
    /// see TokenStreamFromTermVector
    ///</summary>
    public static class TokenSources // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        private class TokenComparer : IComparer<Token>
        {
            private TokenComparer() { } // LUCENENET: Made into singleton

            public static IComparer<Token> Default { get; } = new TokenComparer();

            public int Compare(Token t1, Token t2)
            {
                if (t1.StartOffset == t2.StartOffset)
                {
                    return t1.EndOffset - t2.EndOffset;
                }
                else
                {
                    return t1.StartOffset - t2.StartOffset;
                }
            }
        }

        internal sealed class StoredTokenStream : TokenStream
        {
            internal Token[] tokens;
            internal int currentToken = 0;
            internal ICharTermAttribute termAtt;
            internal IOffsetAttribute offsetAtt;
            internal IPositionIncrementAttribute posincAtt;
            internal IPayloadAttribute payloadAtt; 

            internal StoredTokenStream(Token[] tokens)
            {
                this.tokens = tokens;
                termAtt = AddAttribute<ICharTermAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
                posincAtt = AddAttribute<IPositionIncrementAttribute>();
                payloadAtt = AddAttribute<IPayloadAttribute>();
            }

            public override bool IncrementToken()
            {
                if (currentToken >= tokens.Length)
                {
                    return false;
                }
                Token token = tokens[currentToken++];
                ClearAttributes();
                termAtt.SetEmpty().Append(token);
                offsetAtt.SetOffset(token.StartOffset, token.EndOffset);
                BytesRef payload = token.Payload;
                if (payload != null)
                {
                    payloadAtt.Payload = payload;
                }
                posincAtt.PositionIncrement = 
                    (currentToken <= 1 || 
                    tokens[currentToken - 1].StartOffset > tokens[currentToken - 2].StartOffset 
                    ? 1 : 0);
                return true;
            }
        }

        /// <summary>
        /// A convenience method that tries to first get a TermPositionVector for the specified docId, then, falls back to
        /// using the passed in <see cref="Document"/> to retrieve the <see cref="TokenStream"/>.  This is useful when
        /// you already have the document, but would prefer to use the vector first.
        /// </summary>
        /// <param name="reader">The <see cref="IndexReader"/> to use to try and get the vector from</param>
        /// <param name="docId">The docId to retrieve.</param>
        /// <param name="field">The field to retrieve on the document</param>
        /// <param name="doc">The document to fall back on</param>
        /// <param name="analyzer">The analyzer to use for creating the TokenStream if the vector doesn't exist</param>
        /// <returns>The <see cref="TokenStream"/> for the <see cref="IIndexableField"/> on the <see cref="Document"/></returns>
        /// <exception cref="IOException">if there was an error loading</exception>
        public static TokenStream GetAnyTokenStream(IndexReader reader, int docId,
            string field, Document doc, Analyzer analyzer)
        {
            TokenStream ts = null;

            Fields vectors = reader.GetTermVectors(docId);
            Terms vector = vectors?.GetTerms(field);
            if (vector != null)
            {
                ts = GetTokenStream(vector);
            }

            // No token info stored so fall back to analyzing raw content
            ts = ts ?? GetTokenStream(doc, field, analyzer);
            return ts;
        }

        /// <summary>
        /// A convenience method that tries a number of approaches to getting a token stream.
        /// The cost of finding there are no termVectors in the index is minimal (1000 invocations still 
        /// registers 0 ms). So this "lazy" (flexible?) approach to coding is probably acceptable
        /// </summary>
        /// <returns>null if field not stored correctly</returns>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public static TokenStream GetAnyTokenStream(IndexReader reader, int docId, string field, Analyzer analyzer)
        {
            TokenStream ts = null;

            Fields vectors = reader.GetTermVectors(docId);
            Terms vector = vectors?.GetTerms(field);
            if (vector != null)
            {
                ts = GetTokenStream(vector);
            }

            // No token info stored so fall back to analyzing raw content
            ts = ts ?? GetTokenStream(reader, docId, field, analyzer);
            return ts;
        }

        public static TokenStream GetTokenStream(Terms vector)
        {
            //assumes the worst and makes no assumptions about token position sequences.
            return GetTokenStream(vector, false);
        }

        /// <summary>
        /// Low level api. Returns a token stream generated from a <see cref="Terms"/>. This
        /// can be used to feed the highlighter with a pre-parsed token
        /// stream.  The <see cref="Terms"/> must have offsets available.
        /// <para/>
        /// In my tests the speeds to recreate 1000 token streams using this method are:
        /// <list type="bullet">
        ///     <item><description>
        ///     with TermVector offset only data stored - 420  milliseconds 
        ///     </description></item>
        ///     <item><description>
        ///     with TermVector offset AND position data stored - 271 milliseconds
        ///     (nb timings for TermVector with position data are based on a tokenizer with contiguous
        ///     positions - no overlaps or gaps)
        ///     </description></item>
        ///     <item><description>
        ///     The cost of not using TermPositionVector to store
        ///     pre-parsed content and using an analyzer to re-parse the original content:
        ///     - reanalyzing the original content - 980 milliseconds
        ///     </description></item>
        /// </list>
        /// 
        /// The re-analyze timings will typically vary depending on -
        /// <list type="number">
        ///     <item><description>
        ///     The complexity of the analyzer code (timings above were using a
        ///     stemmer/lowercaser/stopword combo)
        ///     </description></item>
        ///     <item><description>
        ///     The  number of other fields (Lucene reads ALL fields off the disk 
        ///     when accessing just one document field - can cost dear!)
        ///     </description></item>
        ///     <item><description>
        ///     Use of compression on field storage - could be faster due to compression (less disk IO)
        ///     or slower (more CPU burn) depending on the content.
        ///     </description></item>
        /// </list>
        /// </summary>
        /// <param name="tpv"></param>
        /// <param name="tokenPositionsGuaranteedContiguous">true if the token position numbers have no overlaps or gaps. If looking
        /// to eek out the last drops of performance, set to true. If in doubt, set to false.</param>
        /// <exception cref="ArgumentException">if no offsets are available</exception>
        public static TokenStream GetTokenStream(Terms tpv,
              bool tokenPositionsGuaranteedContiguous)
        {
            if (!tpv.HasOffsets)
            {
                throw new ArgumentException("Cannot create TokenStream from Terms without offsets");
            }

            if (!tokenPositionsGuaranteedContiguous && tpv.HasPositions)
            {
                return new TokenStreamFromTermPositionVector(tpv);
            }

            bool hasPayloads = tpv.HasPayloads;

            // code to reconstruct the original sequence of Tokens
            TermsEnum termsEnum = tpv.GetEnumerator();
            int totalTokens = 0;
            while (termsEnum.MoveNext())
            {
                totalTokens += (int)termsEnum.TotalTermFreq;
            }
            Token[] tokensInOriginalOrder = new Token[totalTokens];
            JCG.List<Token> unsortedTokens = null;
            termsEnum = tpv.GetEnumerator();
            DocsAndPositionsEnum dpEnum = null;
            while (termsEnum.MoveNext())
            {
                dpEnum = termsEnum.DocsAndPositions(null, dpEnum);
                if (dpEnum is null)
                {
                    throw new ArgumentException("Required TermVector Offset information was not found");
                }
                string term = termsEnum.Term.Utf8ToString();

                dpEnum.NextDoc();
                int freq = dpEnum.Freq;
                for (int posUpto = 0; posUpto < freq; posUpto++)
                {
                    int pos = dpEnum.NextPosition();
                    if (dpEnum.StartOffset < 0)
                    {
                        throw new ArgumentException("Required TermVector Offset information was not found");
                    }
                    Token token = new Token(term,dpEnum.StartOffset, dpEnum.EndOffset);
                    if (hasPayloads)
                    {
                        // Must make a deep copy of the returned payload,
                        // since D&PEnum API is allowed to re-use on every
                        // call:
                        token.Payload = BytesRef.DeepCopyOf(dpEnum.GetPayload());
                    }

                    if (tokenPositionsGuaranteedContiguous && pos != -1)
                    {
                        // We have positions stored and a guarantee that the token position
                        // information is contiguous

                        // This may be fast BUT wont work if Tokenizers used which create >1
                        // token in same position or
                        // creates jumps in position numbers - this code would fail under those
                        // circumstances

                        // tokens stored with positions - can use this to index straight into
                        // sorted array
                        tokensInOriginalOrder[pos] = token;
                    }
                    else
                    {
                        // tokens NOT stored with positions or not guaranteed contiguous - must
                        // add to list and sort later
                        if (unsortedTokens is null)
                        {
                            unsortedTokens = new JCG.List<Token>();
                        }
                        unsortedTokens.Add(token);
                    }
                }
            }

            // If the field has been stored without position data we must perform a sort
            if (unsortedTokens != null)
            {
                tokensInOriginalOrder = unsortedTokens.ToArray();
                ArrayUtil.TimSort(tokensInOriginalOrder, TokenComparer.Default);
                //tokensInOriginalOrder = tokensInOriginalOrder
                //    .OrderBy(t => t, new TokenComparer() )
                //    .ToArray();
            }
            return new StoredTokenStream(tokensInOriginalOrder);
        }

        ///<summary>
        /// Returns a <see cref="TokenStream"/> with positions and offsets constructed from
        /// field termvectors. If the field has no termvectors or offsets
        /// are not included in the termvector, return null.  See 
        /// <see cref="GetTokenStream(Terms)"/>
        /// for an explanation of what happens when positions aren't present.
        /// </summary>
        /// <param name="reader">the <see cref="IndexReader"/> to retrieve term vectors from</param>
        /// <param name="docId">the document to retrieve term vectors for </param>
        /// <param name="field">the field to retrieve term vectors for</param>
        /// <returns>a <see cref="TokenStream"/>, or null if offsets are not available</returns>
        /// <exception cref="IOException"> If there is a low-level I/O error</exception>
        public static TokenStream GetTokenStreamWithOffsets(IndexReader reader, int docId, string field) 
        {
            Fields vectors = reader.GetTermVectors(docId);
            if (vectors is null) {
                return null;
            }

            Terms vector = vectors.GetTerms(field);
            if (vector is null) {
                return null;
            }

            if (!vector.HasPositions || !vector.HasOffsets) {
                return null;
            }
    
            return GetTokenStream(vector);
        }

        // convenience method
        public static TokenStream GetTokenStream(IndexReader reader, int docId,
              string field, Analyzer analyzer)
        {
            Document doc = reader.Document(docId);
            return GetTokenStream(doc, field, analyzer);
        }

        public static TokenStream GetTokenStream(Document doc, string field,
            Analyzer analyzer)
        {
            string contents = doc.Get(field);
            if (contents is null)
            {
                throw new ArgumentException("Field " + field
                    + " in document is not stored and cannot be analyzed");
            }
            return GetTokenStream(field, contents, analyzer);
        }

        // convenience method
        public static TokenStream GetTokenStream(string field, string contents,
            Analyzer analyzer)
        {
            try
            {
                return analyzer.GetTokenStream(field, contents);
            }
            catch (Exception ex) when (ex.IsIOException())
            {
                throw RuntimeException.Create(ex);
            }
        }
    }
}
