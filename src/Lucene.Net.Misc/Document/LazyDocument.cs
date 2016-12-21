using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Lucene.Net.Documents
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
    /// Defers actually loading a field's value until you ask
    ///  for it.  You must not use the returned Field instances
    ///  after the provided reader has been closed. </summary>
    /// <seealso cref="GetField(FieldInfo)"/>
    public class LazyDocument
    {
        private readonly IndexReader reader;
        private readonly int docID;

        // null until first field is loaded
        private Document doc;

        private IDictionary<int?, IList<LazyField>> fields = new Dictionary<int?, IList<LazyField>>();
        private HashSet<string> fieldNames = new HashSet<string>();

        public LazyDocument(IndexReader reader, int docID)
        {
            this.reader = reader;
            this.docID = docID;
        }

        /// <summary>
        /// Creates an IndexableField whose value will be lazy loaded if and 
        /// when it is used. 
        /// <para>
        /// <b>NOTE:</b> This method must be called once for each value of the field 
        /// name specified in sequence that the values exist.  This method may not be 
        /// used to generate multiple, lazy, IndexableField instances refering to 
        /// the same underlying IndexableField instance.
        /// </para>
        /// <para>
        /// The lazy loading of field values from all instances of IndexableField 
        /// objects returned by this method are all backed by a single Document 
        /// per LazyDocument instance.
        /// </para>
        /// </summary>
        public virtual IIndexableField GetField(FieldInfo fieldInfo)
        {
            fieldNames.Add(fieldInfo.Name);
            IList<LazyField> values = fields.ContainsKey(fieldInfo.Number) ? fields[fieldInfo.Number] : null;
            if (null == values)
            {
                values = new List<LazyField>();
                fields[fieldInfo.Number] = values;
            }

            LazyField value = new LazyField(this, fieldInfo.Name, fieldInfo.Number);
            values.Add(value);

            lock (this)
            {
                // edge case: if someone asks this LazyDoc for more LazyFields
                // after other LazyFields from the same LazyDoc have been
                // actuallized, we need to force the doc to be re-fetched
                // so the new LazyFields are also populated.
                doc = null;
            }
            return value;
        }

        /// <summary>
        /// non-private for test only access
        /// @lucene.internal 
        /// </summary>
        internal virtual Document Document
        {
            get
            {
                lock (this)
                {
                    if (doc == null)
                    {
                        try
                        {
                            doc = reader.Document(docID, fieldNames);
                        }
                        catch (IOException ioe)
                        {
                            throw new InvalidOperationException("unable to load document", ioe);
                        }
                    }
                    return doc;
                }
            }
        }

        // :TODO: synchronize to prevent redundent copying? (sync per field name?)
        private void FetchRealValues(string name, int fieldNum)
        {
            Document d = Document;

            IList<LazyField> lazyValues = fields[fieldNum];
            IIndexableField[] realValues = d.GetFields(name);

            Debug.Assert(realValues.Length <= lazyValues.Count, "More lazy values then real values for field: " + name);

            for (int i = 0; i < lazyValues.Count; i++)
            {
                LazyField f = lazyValues[i];
                if (null != f)
                {
                    f.realValue = realValues[i];
                }
            }
        }


        /// <summary>
        /// @lucene.internal 
        /// </summary>
        public class LazyField : IIndexableField
        {
            private readonly LazyDocument outerInstance;

            internal string name;
            internal int fieldNum;
            internal volatile IIndexableField realValue = null;

            internal LazyField(LazyDocument outerInstance, string name, int fieldNum)
            {
                this.outerInstance = outerInstance;
                this.name = name;
                this.fieldNum = fieldNum;
            }

            /// <summary>
            /// non-private for test only access
            /// @lucene.internal 
            /// </summary>
            public virtual bool HasBeenLoaded
            {
                get { return null != realValue; }
            }

            internal virtual IIndexableField RealValue
            {
                get
                {
                    if (null == realValue)
                    {
                        outerInstance.FetchRealValues(name, fieldNum);
                    }
                    Debug.Assert(HasBeenLoaded, "field value was not lazy loaded");
                    Debug.Assert(realValue.Name.Equals(Name), "realvalue name != name: " + realValue.Name + " != " + Name);

                    return realValue;
                }
            }

            public virtual string Name
            {
                get { return name; }
            }

            public virtual float Boost
            {
                get { return 1.0f; }
            }

            public virtual BytesRef GetBinaryValue()
            {
                return RealValue.GetBinaryValue();
            }

            public virtual string GetStringValue()
            {
                return RealValue.GetStringValue();
            }

            public virtual TextReader GetReaderValue()
            {
                return RealValue.GetReaderValue();
            }

            public virtual object GetNumericValue()
            {
                return RealValue.GetNumericValue();
            }

            public virtual IIndexableFieldType FieldType
            {
                get { return RealValue.FieldType; }
            }

            public virtual TokenStream GetTokenStream(Analyzer analyzer)
            {
                return RealValue.GetTokenStream(analyzer);
            }
        }
    }
}