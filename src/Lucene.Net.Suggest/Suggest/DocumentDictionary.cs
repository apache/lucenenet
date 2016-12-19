using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Spell;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

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
    ///    <item>
    ///      The term and (optionally) payload fields have to be
    ///      stored
    ///    </item>
    ///    <item>
    ///      The weight field can be stored or can be a <see cref="NumericDocValues"/>.
    ///      If the weight field is not defined, the value of the weight is <c>0</c>
    ///    </item>
    ///    <item>
    ///      if any of the term or (optionally) payload fields supplied
    ///      do not have a value for a document, then the document is 
    ///      skipped by the dictionary
    ///    </item>
    ///  </list>
    /// </summary>
    public class DocumentDictionary : IDictionary
    {

        /// <summary>
        /// <see cref="IndexReader"/> to load documents from </summary>
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
        /// for the entry and <paramref name="contextsFeild"/> for associated contexts.
        /// </summary>
        public DocumentDictionary(IndexReader reader, string field, string weightField, string payloadField, string contextsField)
        {
            this.reader = reader;
            this.field = field;
            this.weightField = weightField;
            this.payloadField = payloadField;
            this.contextsField = contextsField;
        }

        public virtual IInputIterator EntryIterator
        {
            get
            {
                return new DocumentInputIterator(this, payloadField != null, contextsField != null);
            }
        }

        /// <summary>
        /// Implements <see cref="IInputIterator"/> from stored fields. </summary>
        protected internal class DocumentInputIterator : IInputIterator
        {
            private readonly DocumentDictionary outerInstance;


            private readonly int docCount;
            private readonly HashSet<string> relevantFields;
            private readonly bool hasPayloads;
            private readonly bool hasContexts;
            private readonly Bits liveDocs;
            private int currentDocId = -1;
            private long currentWeight;
            private BytesRef currentPayload;
            private HashSet<BytesRef> currentContexts;
            private readonly NumericDocValues weightValues;


            /// <summary>
            /// Creates an iterator over term, weight and payload fields from the lucene
            /// index. setting <see cref="HasPayloads"/> to false, implies an iterator
            /// over only term and weight.
            /// </summary>
            public DocumentInputIterator(DocumentDictionary outerInstance, bool hasPayloads, bool hasContexts)
            {
                this.outerInstance = outerInstance;
                this.hasPayloads = hasPayloads;
                this.hasContexts = hasContexts;
                docCount = outerInstance.reader.MaxDoc - 1;
                weightValues = (outerInstance.weightField != null) ? MultiDocValues.GetNumericValues(outerInstance.reader, outerInstance.weightField) : null;
                liveDocs = (outerInstance.reader.Leaves.Count > 0) ? MultiFields.GetLiveDocs(outerInstance.reader) : null;
                relevantFields = GetRelevantFields(new string[] { outerInstance.field, outerInstance.weightField, outerInstance.payloadField, outerInstance.contextsField });
            }

            public virtual long Weight
            {
                get { return currentWeight; }
            }

            public virtual IComparer<BytesRef> Comparator
            {
                get
                {
                    return null;
                }
            }

            public virtual BytesRef Next()
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
                        if (payload == null || (payload.GetBinaryValue() == null && payload.GetStringValue() == null))
                        {
                            continue;
                        }
                        tempPayload = payload.GetBinaryValue() ?? new BytesRef(payload.GetStringValue());
                    }

                    if (hasContexts)
                    {
                        IndexableField[] contextFields = doc.GetFields(outerInstance.contextsField);
                        foreach (IndexableField contextField in contextFields)
                        {
                            if (contextField.GetBinaryValue() == null && contextField.GetStringValue() == null)
                            {
                                continue;
                            }
                            else
                            {
                                tempContexts.Add(contextField.GetBinaryValue() ?? new BytesRef(contextField.GetStringValue()));
                            }
                        }
                    }

                    IndexableField fieldVal = doc.GetField(outerInstance.field);
                    if (fieldVal == null || (fieldVal.GetBinaryValue() == null && fieldVal.GetStringValue() == null))
                    {
                        continue;
                    }
                    tempTerm = (fieldVal.GetStringValue() != null) ? new BytesRef(fieldVal.GetStringValue()) : fieldVal.GetBinaryValue();

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
            /// Returns the value of the <see cref="Weight"/> property for the current document.
            /// Retrieves the value for the <see cref="Weight"/> property if its stored (using <paramref name="doc"/>)
            /// or if its indexed as <see cref="NumericDocValues"/> (using <paramref name="docId"/>) for the document.
            /// If no value is found, then the weight is 0.
            /// </summary>
            protected internal virtual long GetWeight(Document doc, int docId)
            {
                IndexableField weight = doc.GetField(outerInstance.weightField);
                if (weight != null) // found weight as stored
                {
                    // LUCENENET TODO: See if we can make NumericValue into Decimal (which can be converted to any other type of number)
                    // rather than using object.
                    return (weight.GetNumericValue() != null) ? Convert.ToInt64(weight.GetNumericValue()) : 0;
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

            private HashSet<string> GetRelevantFields(params string[] fields)
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

            public virtual IEnumerable<BytesRef> Contexts
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