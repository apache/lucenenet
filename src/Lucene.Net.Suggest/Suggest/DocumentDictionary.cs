using Lucene.Net.Documents;
using Lucene.Net.Documents.Extensions;
using Lucene.Net.Index;
using Lucene.Net.Search.Spell;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Suggest
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

    /// <summary>
    /// <para>
    /// Dictionary with terms, weights, payload (optional) and contexts (optional)
    /// information taken from stored/indexed fields in a Lucene index.
    /// </para>
    /// <b>NOTE:</b> 
    ///  <list type="bullet">
    ///    <item><description>
    ///      The term and (optionally) payload fields have to be
    ///      stored
    ///    </description></item>
    ///    <item><description>
    ///      The weight field can be stored or can be a <see cref="NumericDocValues"/>.
    ///      If the weight field is not defined, the value of the weight is <c>0</c>
    ///    </description></item>
    ///    <item><description>
    ///      if any of the term or (optionally) payload fields supplied
    ///      do not have a value for a document, then the document is 
    ///      skipped by the dictionary
    ///    </description></item>
    ///  </list>
    /// </summary>
    public class DocumentDictionary : IDictionary
    {
        /// <summary>
        /// <see cref="IndexReader"/> to load documents from </summary>
        protected readonly IndexReader m_reader;

        /// <summary>
        /// Field to read payload from </summary>
        protected readonly string m_payloadField;
        /// <summary>
        /// Field to read contexts from </summary>
        protected readonly string m_contextsField;
        private readonly string field;
        private readonly string weightField;

        /// <summary>
        /// Creates a new dictionary with the contents of the fields named <paramref name="field"/>
        /// for the terms and <paramref name="weightField"/> for the weights that will be used for
        /// the corresponding terms.
        /// </summary>
        public DocumentDictionary(IndexReader reader, string field, string weightField)
            : this(reader, field, weightField, null)
        {
        }

        /// <summary>
        /// Creates a new dictionary with the contents of the fields named <paramref name="field"/>
        /// for the terms, <paramref name="weightField"/> for the weights that will be used for the 
        /// the corresponding terms and <paramref name="payloadField"/> for the corresponding payloads
        /// for the entry.
        /// </summary>
        public DocumentDictionary(IndexReader reader, string field, string weightField, string payloadField)
            : this(reader, field, weightField, payloadField, null)
        {
        }

        /// <summary>
        /// Creates a new dictionary with the contents of the fields named <paramref name="field"/>
        /// for the terms, <paramref name="weightField"/> for the weights that will be used for the 
        /// the corresponding terms, <paramref name="payloadField"/> for the corresponding payloads
        /// for the entry and <paramref name="contextsField"/> for associated contexts.
        /// </summary>
        public DocumentDictionary(IndexReader reader, string field, string weightField, string payloadField, string contextsField)
        {
            this.m_reader = reader;
            this.field = field;
            this.weightField = weightField;
            this.m_payloadField = payloadField;
            this.m_contextsField = contextsField;
        }

        public virtual IInputEnumerator GetEntryEnumerator()
        {
            return new DocumentInputEnumerator(this, m_payloadField != null, m_contextsField != null);
        }

        /// <summary>
        /// Implements <see cref="IInputEnumerator"/> from stored fields. </summary>
        protected internal class DocumentInputEnumerator : IInputEnumerator
        {
            private readonly DocumentDictionary outerInstance;

            private readonly int docCount;
            private readonly ISet<string> relevantFields;
            private readonly bool hasPayloads;
            private readonly bool hasContexts;
            private readonly IBits liveDocs;
            private int currentDocId = -1;
            private long currentWeight;
            private BytesRef currentPayload;
            private ISet<BytesRef> currentContexts;
            private readonly NumericDocValues weightValues;
            private BytesRef current;

            /// <summary>
            /// Creates an iterator over term, weight and payload fields from the lucene
            /// index. Setting <paramref name="hasPayloads"/> to <c>false</c>, implies an enumerator
            /// over only term and weight.
            /// </summary>
            public DocumentInputEnumerator(DocumentDictionary documentDictionary, bool hasPayloads, bool hasContexts)
            {
                this.outerInstance = documentDictionary;
                this.hasPayloads = hasPayloads;
                this.hasContexts = hasContexts;
                docCount = documentDictionary.m_reader.MaxDoc - 1;
                weightValues = (documentDictionary.weightField != null) ? MultiDocValues.GetNumericValues(documentDictionary.m_reader, documentDictionary.weightField) : null;
                liveDocs = (documentDictionary.m_reader.Leaves.Count > 0) ? MultiFields.GetLiveDocs(documentDictionary.m_reader) : null;
                relevantFields = GetRelevantFields(new string[] { documentDictionary.field, documentDictionary.weightField, documentDictionary.m_payloadField, documentDictionary.m_contextsField });
            }

            public virtual long Weight => currentWeight;

            public virtual IComparer<BytesRef> Comparer => null;

            public BytesRef Current => current;

            public bool MoveNext()
            {
                while (currentDocId < docCount)
                {
                    currentDocId++;
                    if (liveDocs != null && !liveDocs.Get(currentDocId))
                    {
                        continue;
                    }

                    Document doc = outerInstance.m_reader.Document(currentDocId, relevantFields);

                    BytesRef tempPayload = null;
                    ISet<BytesRef> tempContexts = new JCG.HashSet<BytesRef>();

                    if (hasPayloads)
                    {
                        IIndexableField payload = doc.GetField(outerInstance.m_payloadField);
                        if (payload is null || (payload.GetBinaryValue() is null && payload.GetStringValue() is null))
                        {
                            continue;
                        }
                        tempPayload = payload.GetBinaryValue() ?? new BytesRef(payload.GetStringValue());
                    }

                    if (hasContexts)
                    {
                        IIndexableField[] contextFields = doc.GetFields(outerInstance.m_contextsField);
                        foreach (IIndexableField contextField in contextFields)
                        {
                            if (contextField.GetBinaryValue() is null && contextField.GetStringValue() is null)
                            {
                                //continue; //LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
                            }
                            else
                            {
                                tempContexts.Add(contextField.GetBinaryValue() ?? new BytesRef(contextField.GetStringValue()));
                            }
                        }
                    }

                    IIndexableField fieldVal = doc.GetField(outerInstance.field);
                    if (fieldVal is null || (fieldVal.GetBinaryValue() is null && fieldVal.GetStringValue() is null))
                    {
                        continue;
                    }
                    current = (fieldVal.GetStringValue() is null) ? fieldVal.GetBinaryValue() : new BytesRef(fieldVal.GetStringValue());

                    currentPayload = tempPayload;
                    currentContexts = tempContexts;
                    currentWeight = GetWeight(doc, currentDocId);

                    return true;
                }
                current = null;
                return false;
            }

            public virtual BytesRef Payload => currentPayload;

            public virtual bool HasPayloads => hasPayloads;

            /// <summary>
            /// Returns the value of the <see cref="Weight"/> property for the current document.
            /// Retrieves the value for the <see cref="Weight"/> property if its stored (using <paramref name="doc"/>)
            /// or if its indexed as <see cref="NumericDocValues"/> (using <paramref name="docId"/>) for the document.
            /// If no value is found, then the weight is 0.
            /// </summary>
            protected internal virtual long GetWeight(Document doc, int docId)
            {
                IIndexableField weight = doc.GetField(outerInstance.weightField);
                if (weight != null) // found weight as stored
                {
                    return weight.GetInt64ValueOrDefault();
                } // found weight as NumericDocValue
                else if (weightValues != null)
                {
                    return weightValues.Get(docId);
                } // fall back
                else
                {
                    return 0;
                }
            }

            private static ISet<string> GetRelevantFields(params string[] fields) // LUCENENET: CA1822: Mark members as static
            {
                var relevantFields = new JCG.HashSet<string>();
                foreach (string relevantField in fields)
                {
                    if (relevantField != null)
                    {
                        relevantFields.Add(relevantField);
                    }
                }
                return relevantFields;
            }

            public virtual ICollection<BytesRef> Contexts => hasContexts ? currentContexts : null;

            public virtual bool HasContexts => hasContexts;
        }
    }
}