using J2N.Threading.Atomic;
using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
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
    /// Creates <see cref="Document"/> objects. Uses a <see cref="ContentSource"/> to generate
    /// <see cref="DocData"/> objects.
    /// </summary>
    /// <remarks>
    /// Supports the following parameters:
    /// <list type="bullet">
    ///     <item><term>content.source</term><description>specifies the <see cref="ContentSource"/> class to use (default <b>SingleDocSource</b>).</description></item>
    ///     <item><term>doc.stored</term><description>specifies whether fields should be stored (default <b>false</b>).</description></item>
    ///     <item><term>doc.body.stored</term><description>specifies whether the body field should be stored (default = <b>doc.stored</b>).</description></item>
    ///     <item><term>doc.tokenized</term><description>specifies whether fields should be tokenized (default <b>true</b>).</description></item>
    ///     <item><term>doc.body.tokenized</term><description>specifies whether the body field should be tokenized (default = <b>doc.tokenized</b>).</description></item>
    ///     <item><term>doc.tokenized.norms</term><description>specifies whether norms should be stored in the index or not. (default <b>false</b>).</description></item>
    ///     <item><term>doc.body.tokenized.norms</term><description>
    ///         specifies whether norms should be stored in the index for the body field. 
    ///         This can be set to true, while <c>doc.tokenized.norms</c> is set to false, to allow norms storing just
    ///         for the body field. (default <b>true</b>).
    ///         </description></item>
    ///     <item><term>doc.term.vector</term><description>specifies whether term vectors should be stored for fields (default <b>false</b>).</description></item>
    ///     <item><term>doc.term.vector.positions</term><description>specifies whether term vectors should be stored with positions (default <b>false</b>).</description></item>
    ///     <item><term>doc.term.vector.offsets</term><description>specifies whether term vectors should be stored with offsets (default <b>false</b>).</description></item>
    ///     <item><term>doc.store.body.bytes</term><description>specifies whether to store the raw bytes of the document's content in the document (default <b>false</b>).</description></item>
    ///     <item><term>doc.reuse.fields</term><description>specifies whether <see cref="Field"/> and <see cref="Document"/> objects  should be reused (default <b>true</b>).</description></item>
    ///     <item><term>doc.index.props</term><description>specifies whether the properties returned by</description></item>
    ///     <item><term>doc.random.id.limit</term><description>
    ///         if specified, docs will be assigned random
    ///         IDs from 0 to this limit.  This is useful with UpdateDoc
    ///         for testing performance of <see cref="Index.IndexWriter.UpdateDocument(Index.Term, IEnumerable{Index.IIndexableField})"/>.
    ///         <see cref="DocData.Props"/> will be indexed. (default <b>false</b>).
    ///     </description></item>
    /// </list>
    /// </remarks>
    public class DocMaker : IDisposable
    {
        private class LeftOver
        {
            public DocData DocData { get; set; }
            public int Count { get; set; }
        }

        private Random r;
        private int updateDocIDLimit;

        /// <summary>
        /// Document state, supports reuse of field instances
        /// across documents (see <c>reuseFields</c> parameter).
        /// </summary>
        protected class DocState
        {
            private readonly IDictionary<string, Field> fields;
            private readonly IDictionary<string, Field> numericFields;
            private readonly bool reuseFields;
            internal readonly Document doc;
            internal DocData docData = new DocData();

            public DocState(bool reuseFields, FieldType ft, FieldType bodyFt)
            {

                this.reuseFields = reuseFields;

                if (reuseFields)
                {
                    fields = new Dictionary<string, Field>();
                    numericFields = new Dictionary<string, Field>();

                    // Initialize the map with the default fields.
                    fields[BODY_FIELD] = new Field(BODY_FIELD, "", bodyFt);
                    fields[TITLE_FIELD] = new Field(TITLE_FIELD, "", ft);
                    fields[DATE_FIELD] = new Field(DATE_FIELD, "", ft);
                    fields[ID_FIELD] = new StringField(ID_FIELD, "", Field.Store.YES);
                    fields[NAME_FIELD] = new Field(NAME_FIELD, "", ft);

                    numericFields[DATE_MSEC_FIELD] = new Int64Field(DATE_MSEC_FIELD, 0L, Field.Store.NO);
                    numericFields[TIME_SEC_FIELD] = new Int32Field(TIME_SEC_FIELD, 0, Field.Store.NO);

                    doc = new Document();
                }
                else
                {
                    numericFields = null;
                    fields = null;
                    doc = null;
                }
            }

            /// <summary>
            /// Returns a field corresponding to the field name. If
            /// <c>reuseFields</c> was set to <c>true</c>, then it attempts to reuse a
            /// <see cref="Field"/> instance. If such a field does not exist, it creates a new one.
            /// </summary>
            internal Field GetField(string name, FieldType ft)
            {
                if (!reuseFields)
                {
                    return new Field(name, "", ft);
                }

                if (!fields.TryGetValue(name, out Field f) || f is null)
                {
                    f = new Field(name, "", ft);
                    fields[name] = f;
                }
                return f;
            }

            internal Field GetNumericField(string name, NumericType type)
            {
                Field f;
                if (reuseFields)
                {
                    numericFields.TryGetValue(name, out f);
                }
                else
                {
                    f = null;
                }

                if (f is null)
                {
                    switch (type)
                    {
                        case NumericType.INT32:
                            f = new Int32Field(name, 0, Field.Store.NO);
                            break;
                        case NumericType.INT64:
                            f = new Int64Field(name, 0L, Field.Store.NO);
                            break;
                        case NumericType.SINGLE:
                            f = new SingleField(name, 0.0F, Field.Store.NO);
                            break;
                        case NumericType.DOUBLE:
                            f = new DoubleField(name, 0.0, Field.Store.NO);
                            break;
                        default:
                            throw AssertionError.Create("Cannot get here");
                    }
                    if (reuseFields)
                    {
                        numericFields[name] = f;
                    }
                }
                return f;
            }
        }

        private bool storeBytes = false;

        // LUCENENET specific: DateUtil not used

        // leftovers are thread local, because it is unsafe to share residues between threads
        private readonly DisposableThreadLocal<LeftOver> leftovr = new DisposableThreadLocal<LeftOver>();
        private DisposableThreadLocal<DocState> docState = new DisposableThreadLocal<DocState>();

        public static readonly string BODY_FIELD = "body";
        public static readonly string TITLE_FIELD = "doctitle";
        public static readonly string DATE_FIELD = "docdate";
        public static readonly string DATE_MSEC_FIELD = "docdatenum";
        public static readonly string TIME_SEC_FIELD = "doctimesecnum";
        public static readonly string ID_FIELD = "docid";
        public static readonly string BYTES_FIELD = "bytes";
        public static readonly string NAME_FIELD = "docname";

        protected Config m_config;

        protected FieldType m_valType;
        protected FieldType m_bodyValType;

        protected ContentSource m_source;
        protected bool m_reuseFields;
        protected bool m_indexProperties;

        private readonly AtomicInt32 numDocsCreated = new AtomicInt32();

        public DocMaker()
        {
        }

        // create a doc
        // use only part of the body, modify it to keep the rest (or use all if size==0).
        // reset the docdata properties so they are not added more than once.
        private Document CreateDocument(DocData docData, int size, int cnt)
        {

            DocState ds = GetDocState();
            Document doc = m_reuseFields ? ds.doc : new Document();
            doc.Fields.Clear();

            // Set ID_FIELD
            FieldType ft = new FieldType(m_valType);
            ft.IsIndexed = true;

            Field idField = ds.GetField(ID_FIELD, ft);
            int id;
            if (r != null)
            {
                id = r.Next(updateDocIDLimit);
            }
            else
            {
                id = docData.ID;
                if (id == -1)
                {
                    id = numDocsCreated.GetAndIncrement();
                }
            }
            idField.SetStringValue(Convert.ToString(id, CultureInfo.InvariantCulture));
            doc.Add(idField);

            // Set NAME_FIELD
            string name = docData.Name;
            if (name is null) name = "";
            name = cnt < 0 ? name : name + "_" + cnt;
            Field nameField = ds.GetField(NAME_FIELD, m_valType);
            nameField.SetStringValue(name);
            doc.Add(nameField);

            // Set DATE_FIELD
            DateTime? date = null;
            string dateString = docData.Date;
            if (dateString != null)
            {
                // LUCENENET: TryParseExact needs a non-nullable DateTime to work.
                if (DateTime.TryParseExact(dateString, new string[] {
                    // Original format from Java
                    "dd-MMM-yyyy HH:mm:ss",
                    // Actual format from the test files...
                    "yyyyMMddHHmmss"
                    }, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime temp))
                {
                    date = temp;
                }
                // LUCENENET: Hail Mary in case the formats above are not adequate
                else if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out temp))
                {
                    date = temp;
                }
            }
            else
            {
                dateString = "";
            }
            Field dateStringField = ds.GetField(DATE_FIELD, m_valType);
            dateStringField.SetStringValue(dateString);
            doc.Add(dateStringField);

            if (date is null)
            {
                // just set to right now
                date = DateTime.Now; 
            }

            Field dateField = ds.GetNumericField(DATE_MSEC_FIELD, NumericType.INT64);
            dateField.SetInt64Value(date.Value.Ticks);
            doc.Add(dateField);

            //util.cal.setTime(date);
            //int sec = util.cal.get(Calendar.HOUR_OF_DAY) * 3600 + util.cal.get(Calendar.MINUTE) * 60 + util.cal.get(Calendar.SECOND);
            int sec = Convert.ToInt32(date.Value.ToUniversalTime().TimeOfDay.TotalSeconds, CultureInfo.InvariantCulture);

            Field timeSecField = ds.GetNumericField(TIME_SEC_FIELD, NumericType.INT32);
            timeSecField.SetInt32Value(sec);
            doc.Add(timeSecField);

            // Set TITLE_FIELD
            string title = docData.Title;
            Field titleField = ds.GetField(TITLE_FIELD, m_valType);
            titleField.SetStringValue(title ?? "");
            doc.Add(titleField);

            string body = docData.Body;
            if (body != null && body.Length > 0)
            {
                string bdy;
                if (size <= 0 || size >= body.Length)
                {
                    bdy = body; // use all
                    docData.Body = ""; // nothing left
                }
                else
                {
                    // attempt not to break words - if whitespace found within next 20 chars...
                    for (int n = size - 1; n < size + 20 && n < body.Length; n++)
                    {
                        if (char.IsWhiteSpace(body[n]))
                        {
                            size = n;
                            break;
                        }
                    }
                    bdy = body.Substring(0, size - 0); // use part
                    docData.Body = body.Substring(size); // some left
                }
                Field bodyField = ds.GetField(BODY_FIELD, m_bodyValType);
                bodyField.SetStringValue(bdy);
                doc.Add(bodyField);

                if (storeBytes)
                {
                    Field bytesField = ds.GetField(BYTES_FIELD, StringField.TYPE_STORED);
                    bytesField.SetBytesValue(Encoding.UTF8.GetBytes(bdy));
                    doc.Add(bytesField);
                }
            }

            if (m_indexProperties)
            {
                var props = docData.Props;
                if (props != null)
                {
                    foreach (var entry in props)
                    {
                        Field f = ds.GetField((string)entry.Key, m_valType);
                        f.SetStringValue((string)entry.Value);
                        doc.Add(f);
                    }
                    docData.Props = null;
                }
            }

            //System.out.println("============== Created doc "+numDocsCreated+" :\n"+doc+"\n==========");
            return doc;
        }

        private void ResetLeftovers()
        {
            leftovr.Value = null;
        }

        protected virtual DocState GetDocState()
        {
            DocState ds = docState.Value;
            if (ds is null)
            {
                ds = new DocState(m_reuseFields, m_valType, m_bodyValType);
                docState.Value = ds;
            }
            return ds;
        }

        /// <summary>
        /// Closes the <see cref="DocMaker"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Closes the <see cref="DocMaker"/>. The base implementation closes the
        /// <see cref="ContentSource"/>, and it can be overridden to do more work (but make
        /// sure to call <c>base.Dispose(bool)</c>).
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_source?.Dispose();
                leftovr?.Dispose(); // LUCENENET specific
                docState?.Dispose(); // LUCENENET specific
            }
        }

        /// <summary>
        /// Creates a <see cref="Document"/> object ready for indexing. This method uses the
        /// <see cref="ContentSource"/> to get the next document from the source, and creates
        /// a <see cref="Document"/> object from the returned fields. If
        /// <c>reuseFields</c> was set to <c>true</c>, it will reuse <see cref="Document"/>
        /// and <see cref="Field"/> instances.
        /// </summary>
        /// <returns></returns>
        public virtual Document MakeDocument()
        {
            ResetLeftovers();
            DocData docData = m_source.GetNextDocData(GetDocState().docData);
            Document doc = CreateDocument(docData, 0, -1);
            return doc;
        }

        /// <summary>
        /// Same as <see cref="MakeDocument()"/>, only this method creates a document of the
        /// given size input by <paramref name="size"/>.
        /// </summary>
        public virtual Document MakeDocument(int size)
        {
            LeftOver lvr = leftovr.Value;
            if (lvr is null || lvr.DocData is null || lvr.DocData.Body is null
                || lvr.DocData.Body.Length == 0)
            {
                ResetLeftovers();
            }
            DocData docData = GetDocState().docData;
            DocData dd = (lvr is null ? m_source.GetNextDocData(docData) : lvr.DocData);
            int cnt = (lvr is null ? 0 : lvr.Count);
            while (dd.Body is null || dd.Body.Length < size)
            {
                DocData dd2 = dd;
                dd = m_source.GetNextDocData(new DocData());
                cnt = 0;
                dd.Body = (dd2.Body + dd.Body);
            }
            Document doc = CreateDocument(dd, size, cnt);
            if (dd.Body is null || dd.Body.Length == 0)
            {
                ResetLeftovers();
            }
            else
            {
                if (lvr is null)
                {
                    lvr = new LeftOver();
                    leftovr.Value = lvr;
                }
                lvr.DocData = dd;
                lvr.Count = ++cnt;
            }
            return doc;
        }

        /// <summary>Reset inputs so that the test run would behave, input wise, as if it just started.</summary>
        public virtual void ResetInputs()
        {
            m_source.PrintStatistics("docs");
            // re-initiate since properties by round may have changed.
            SetConfig(m_config, m_source);
            m_source.ResetInputs();
            numDocsCreated.Value = 0;
            ResetLeftovers();
        }

        /// <summary>Set the configuration parameters of this doc maker.</summary>
        public virtual void SetConfig(Config config, ContentSource source)
        {
            this.m_config = config;
            this.m_source = source;

            bool stored = config.Get("doc.stored", false);
            bool bodyStored = config.Get("doc.body.stored", stored);
            bool tokenized = config.Get("doc.tokenized", true);
            bool bodyTokenized = config.Get("doc.body.tokenized", tokenized);
            bool norms = config.Get("doc.tokenized.norms", false);
            bool bodyNorms = config.Get("doc.body.tokenized.norms", true);
            bool termVec = config.Get("doc.term.vector", false);
            bool termVecPositions = config.Get("doc.term.vector.positions", false);
            bool termVecOffsets = config.Get("doc.term.vector.offsets", false);

            m_valType = new FieldType(TextField.TYPE_NOT_STORED);
            m_valType.IsStored = stored;
            m_valType.IsTokenized = tokenized;
            m_valType.OmitNorms = !norms;
            m_valType.StoreTermVectors = termVec;
            m_valType.StoreTermVectorPositions = termVecPositions;
            m_valType.StoreTermVectorOffsets = termVecOffsets;
            m_valType.Freeze();

            m_bodyValType = new FieldType(TextField.TYPE_NOT_STORED);
            m_bodyValType.IsStored = bodyStored;
            m_bodyValType.IsTokenized = bodyTokenized;
            m_bodyValType.OmitNorms = !bodyNorms;
            m_bodyValType.StoreTermVectors = termVec;
            m_bodyValType.StoreTermVectorPositions = termVecPositions;
            m_bodyValType.StoreTermVectorOffsets = termVecOffsets;
            m_bodyValType.Freeze();

            storeBytes = config.Get("doc.store.body.bytes", false);

            m_reuseFields = config.Get("doc.reuse.fields", true);

            // In a multi-rounds run, it is important to reset DocState since settings
            // of fields may change between rounds, and this is the only way to reset
            // the cache of all threads.
            docState = new DisposableThreadLocal<DocState>();

            m_indexProperties = config.Get("doc.index.props", false);

            updateDocIDLimit = config.Get("doc.random.id.limit", -1);
            if (updateDocIDLimit != -1)
            {
                r = new J2N.Randomizer(179);
            }
        }
    }
}
