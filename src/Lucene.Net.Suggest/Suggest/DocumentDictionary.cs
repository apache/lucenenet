using System.Collections.Generic;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Spell;
using Lucene.Net.Util;

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
    ///  <ul>
    ///    <li>
    ///      The term and (optionally) payload fields have to be
    ///      stored
    ///    </li>
    ///    <li>
    ///      The weight field can be stored or can be a <seealso cref="NumericDocValues"/>.
    ///      If the weight field is not defined, the value of the weight is <code>0</code>
    ///    </li>
    ///    <li>
    ///      if any of the term or (optionally) payload fields supplied
    ///      do not have a value for a document, then the document is 
    ///      skipped by the dictionary
    ///    </li>
    ///  </ul>
    /// </summary>
    public class DocumentDictionary : Dictionary
    {

        /// <summary>
        /// <seealso cref="IndexReader"/> to load documents from </summary>
        protected internal readonly IndexReader reader;

        /// <summary>
        /// Field to read payload from </summary>
        protected internal readonly string payloadField;
        /// <summary>
        /// Field to read contexts from </summary>
        protected internal readonly string contextsField;
        private readonly string field;
        private readonly string weightField;

        /// <summary>
        /// Creates a new dictionary with the contents of the fields named <code>field</code>
        /// for the terms and <code>weightField</code> for the weights that will be used for
        /// the corresponding terms.
        /// </summary>
        public DocumentDictionary(IndexReader reader, string field, string weightField)
            : this(reader, field, weightField, null)
        {
        }

        /// <summary>
        /// Creates a new dictionary with the contents of the fields named <code>field</code>
        /// for the terms, <code>weightField</code> for the weights that will be used for the 
        /// the corresponding terms and <code>payloadField</code> for the corresponding payloads
        /// for the entry.
        /// </summary>
        public DocumentDictionary(IndexReader reader, string field, string weightField, string payloadField)
            : this(reader, field, weightField, payloadField, null)
        {
        }

        /// <summary>
        /// Creates a new dictionary with the contents of the fields named <code>field</code>
        /// for the terms, <code>weightField</code> for the weights that will be used for the 
        /// the corresponding terms, <code>payloadField</code> for the corresponding payloads
        /// for the entry and <code>contextsFeild</code> for associated contexts.
        /// </summary>
        public DocumentDictionary(IndexReader reader, string field, string weightField, string payloadField, string contextsField)
        {
            this.reader = reader;
            this.field = field;
            this.weightField = weightField;
            this.payloadField = payloadField;
            this.contextsField = contextsField;
        }

        public virtual InputIterator EntryIterator
        {
            get
            {
                return new DocumentInputIterator(this, payloadField != null, contextsField != null);
            }
        }

        /// <summary>
        /// Implements <seealso cref="InputIterator"/> from stored fields. </summary>
        protected internal class DocumentInputIterator : InputIterator
        {
            private readonly DocumentDictionary outerInstance;


            internal readonly int docCount;
            internal readonly HashSet<string> relevantFields;
            internal readonly bool hasPayloads;
            internal readonly bool hasContexts;
            internal readonly Bits liveDocs;
            internal int currentDocId = -1;
            internal long currentWeight;
            internal BytesRef currentPayload;
            internal HashSet<BytesRef> currentContexts;
            internal readonly NumericDocValues weightValues;


            /// <summary>
            /// Creates an iterator over term, weight and payload fields from the lucene
            /// index. setting <code>withPayload</code> to false, implies an iterator
            /// over only term and weight.
            /// </summary>
            public DocumentInputIterator(DocumentDictionary outerInstance, bool hasPayloads, bool hasContexts)
            {
                this.outerInstance = outerInstance;
                this.hasPayloads = hasPayloads;
                this.hasContexts = hasContexts;
                docCount = outerInstance.reader.MaxDoc() - 1;
                weightValues = (outerInstance.weightField != null) ? MultiDocValues.GetNumericValues(outerInstance.reader, outerInstance.weightField) : null;
                liveDocs = (outerInstance.reader.Leaves().Count > 0) ? MultiFields.GetLiveDocs(outerInstance.reader) : null;
                relevantFields = GetRelevantFields(new string[] { outerInstance.field, outerInstance.weightField, outerInstance.payloadField, outerInstance.contextsField });
            }

            public virtual long Weight
            {
                get { return currentWeight; }
            }

            public IComparer<BytesRef> Comparator
            {
                get
                {
                    return null;
                }
            }

            public BytesRef Next()
            {
                while (currentDocId < docCount)
                {
                    currentDocId++;
                    if (liveDocs != null && !liveDocs.Get(currentDocId))
                    {
                        continue;
                    }

                    Document doc = outerInstance.reader.Document(currentDocId, relevantFields);

                    BytesRef tempPayload = null;
                    BytesRef tempTerm = null;
                    HashSet<BytesRef> tempContexts = new HashSet<BytesRef>();

                    if (hasPayloads)
                    {
                        IndexableField payload = doc.GetField(outerInstance.payloadField);
                        if (payload == null || (payload.BinaryValue == null && payload.StringValue == null))
                        {
                            continue;
                        }
                        tempPayload = payload.BinaryValue ?? new BytesRef(payload.StringValue);
                    }

                    if (hasContexts)
                    {
                        IndexableField[] contextFields = doc.GetFields(outerInstance.contextsField);
                        foreach (IndexableField contextField in contextFields)
                        {
                            if (contextField.BinaryValue == null && contextField.StringValue == null)
                            {
                                continue;
                            }
                            else
                            {
                                tempContexts.Add(contextField.BinaryValue ?? new BytesRef(contextField.StringValue));
                            }
                        }
                    }

                    IndexableField fieldVal = doc.GetField(outerInstance.field);
                    if (fieldVal == null || (fieldVal.BinaryValue == null && fieldVal.StringValue == null))
                    {
                        continue;
                    }
                    tempTerm = (fieldVal.StringValue != null) ? new BytesRef(fieldVal.StringValue) : fieldVal.BinaryValue;

                    currentPayload = tempPayload;
                    currentContexts = tempContexts;
                    currentWeight = GetWeight(doc, currentDocId);

                    return tempTerm;
                }
                return null;
            }

            public virtual BytesRef Payload
            {
                get { return currentPayload; }
            }

            public virtual bool HasPayloads
            {
                get { return hasPayloads; }
            }

            /// <summary>
            /// Returns the value of the <code>weightField</code> for the current document.
            /// Retrieves the value for the <code>weightField</code> if its stored (using <code>doc</code>)
            /// or if its indexed as <seealso cref="NumericDocValues"/> (using <code>docId</code>) for the document.
            /// If no value is found, then the weight is 0.
            /// </summary>
            protected internal virtual long GetWeight(Document doc, int docId)
            {
                IndexableField weight = doc.GetField(outerInstance.weightField);
                if (weight != null) // found weight as stored
                {
                    return (weight.NumericValue != null) ? (long)weight.NumericValue : 0;
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

            internal HashSet<string> GetRelevantFields(params string[] fields)
            {
                var relevantFields = new HashSet<string>();
                foreach (string relevantField in fields)
                {
                    if (relevantField != null)
                    {
                        relevantFields.Add(relevantField);
                    }
                }
                return relevantFields;
            }

            public virtual HashSet<BytesRef> Contexts
            {
                get
                {
                    if (hasContexts)
                    {
                        return currentContexts;
                    }
                    return null;
                }
            }

            public virtual bool HasContexts
            {
                get { return hasContexts; }
            }
        }
    }
}