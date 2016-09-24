using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Facet
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

    using ArrayUtil = Lucene.Net.Util.ArrayUtil;
    using AssociationFacetField = Lucene.Net.Facet.Taxonomy.AssociationFacetField;
    using BinaryDocValuesField = Lucene.Net.Documents.BinaryDocValuesField;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Document = Lucene.Net.Documents.Document;
    using FacetLabel = Lucene.Net.Facet.Taxonomy.FacetLabel;
    using Field = Lucene.Net.Documents.Field;
    using FloatAssociationFacetField = Lucene.Net.Facet.Taxonomy.FloatAssociationFacetField;
    using IndexableField = Lucene.Net.Index.IndexableField;
    using IndexableFieldType = Lucene.Net.Index.IndexableFieldType;
    using IntAssociationFacetField = Lucene.Net.Facet.Taxonomy.IntAssociationFacetField;
    using IntsRef = Lucene.Net.Util.IntsRef;
    using SortedSetDocValuesFacetField = Lucene.Net.Facet.SortedSet.SortedSetDocValuesFacetField;
    using SortedSetDocValuesField = Lucene.Net.Documents.SortedSetDocValuesField;
    using StringField = Lucene.Net.Documents.StringField;
    using TaxonomyWriter = Lucene.Net.Facet.Taxonomy.TaxonomyWriter;

    /// <summary>
    /// Records per-dimension configuration.  By default a
    ///  dimension is flat, single valued and does
    ///  not require count for the dimension; use
    ///  the setters in this class to change these settings for
    ///  each dim.
    /// 
    ///  <para><b>NOTE</b>: this configuration is not saved into the
    ///  index, but it's vital, and up to the application to
    ///  ensure, that at search time the provided {@code
    ///  FacetsConfig} matches what was used during indexing.
    /// 
    ///  @lucene.experimental 
    /// </para>
    /// </summary>
    public class FacetsConfig
    {
        /// <summary>
        /// Which Lucene field holds the drill-downs and ords (as
        ///  doc values). 
        /// </summary>
        public const string DEFAULT_INDEX_FIELD_NAME = "$facets";

        private readonly IDictionary<string, DimConfig> fieldTypes = new ConcurrentDictionary<string, DimConfig>();

        // Used only for best-effort detection of app mixing
        // int/float/bytes in a single indexed field:
        private readonly IDictionary<string, string> assocDimTypes = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// Holds the configuration for one dimension
        /// 
        /// @lucene.experimental 
        /// </summary>
        public sealed class DimConfig
        {
            /// <summary>
            /// True if this dimension is hierarchical. </summary>
            public bool Hierarchical;

            /// <summary>
            /// True if this dimension is multi-valued. </summary>
            public bool MultiValued;

            /// <summary>
            /// True if the count/aggregate for the entire dimension
            ///  is required, which is unusual (default is false). 
            /// </summary>
            public bool RequireDimCount;

            /// <summary>
            /// Actual field where this dimension's facet labels
            ///  should be indexed 
            /// </summary>
            public string IndexFieldName = DEFAULT_INDEX_FIELD_NAME;

            /// <summary>
            /// Default constructor. </summary>
            public DimConfig()
            {
            }
        }

        /// <summary>
        /// Default per-dimension configuration. </summary>
        public static readonly DimConfig DEFAULT_DIM_CONFIG = new DimConfig();

        /// <summary>
        /// Default constructor. </summary>
        public FacetsConfig()
        {
        }

        /// <summary>
        /// Get the default configuration for new dimensions.  Useful when
        ///  the dimension is not known beforehand and may need different 
        ///  global default settings, like {@code multivalue =
        ///  true}.
        /// </summary>
        ///  <returns> The default configuration to be used for dimensions that 
        ///  are not yet set in the <seealso cref="FacetsConfig"/>  </returns>
        protected virtual DimConfig DefaultDimConfig
        {
            get
            {
                return DEFAULT_DIM_CONFIG;
            }
        }

        /// <summary>
        /// Get the current configuration for a dimension. </summary>
        public virtual DimConfig GetDimConfig(string dimName)
        {
            lock (this)
            {
                DimConfig ft;
                if (!fieldTypes.TryGetValue(dimName, out ft))
                {
                    ft = DefaultDimConfig;
                }
                return ft;
            }
        }

        /// <summary>
        /// Pass {@code true} if this dimension is hierarchical
        ///  (has depth > 1 paths). 
        /// </summary>
        public virtual void SetHierarchical(string dimName, bool v)
        {
            lock (this)
            {
                if (!fieldTypes.ContainsKey(dimName))
                {
                    var ft = new DimConfig { Hierarchical = v };
                    fieldTypes[dimName] = ft;
                }
                else
                {
                    fieldTypes[dimName].Hierarchical = v;
                }
            }
        }

        /// <summary>
        /// Pass {@code true} if this dimension may have more than
        ///  one value per document. 
        /// </summary>
        public virtual void SetMultiValued(string dimName, bool v)
        {
            lock (this)
            {
                if (!fieldTypes.ContainsKey(dimName))
                {
                    var ft = new DimConfig { MultiValued = v };
                    fieldTypes[dimName] = ft;
                }
                else
                {
                    fieldTypes[dimName].MultiValued = v;
                }
            }
        }

        /// <summary>
        /// Pass {@code true} if at search time you require
        ///  accurate counts of the dimension, i.e. how many
        ///  hits have this dimension. 
        /// </summary>
        public virtual void SetRequireDimCount(string dimName, bool v)
        {
            lock (this)
            {
                if (!fieldTypes.ContainsKey(dimName))
                {
                    var ft = new DimConfig { RequireDimCount = v };
                    fieldTypes[dimName] = ft;
                }
                else
                {
                    fieldTypes[dimName].RequireDimCount = v;
                }
            }
        }

        /// <summary>
        /// Specify which index field name should hold the
        ///  ordinals for this dimension; this is only used by the
        ///  taxonomy based facet methods. 
        /// </summary>
        public virtual void SetIndexFieldName(string dimName, string indexFieldName)
        {
            lock (this)
            {
                if (!fieldTypes.ContainsKey(dimName))
                {
                    var ft = new DimConfig { IndexFieldName = indexFieldName };
                    fieldTypes[dimName] = ft;
                }
                else
                {
                    fieldTypes[dimName].IndexFieldName = indexFieldName;
                }
            }
        }

        /// <summary>
        /// Returns map of field name to <seealso cref="DimConfig"/>. </summary>
        public virtual IDictionary<string, DimConfig> DimConfigs
        {
            get
            {
                return fieldTypes;
            }
        }

        private static void CheckSeen(HashSet<string> seenDims, string dim)
        {
            if (seenDims.Contains(dim))
            {
                throw new System.ArgumentException("dimension \"" + dim + "\" is not multiValued, but it appears more than once in this document");
            }
            seenDims.Add(dim);
        }

        /// <summary>
        /// Translates any added <seealso cref="FacetField"/>s into normal fields for indexing;
        /// only use this version if you did not add any taxonomy-based fields (
        /// <seealso cref="FacetField"/> or <seealso cref="AssociationFacetField"/>).
        /// 
        /// <para>
        /// <b>NOTE:</b> you should add the returned document to IndexWriter, not the
        /// input one!
        /// </para>
        /// </summary>
        public virtual Document Build(Document doc)
        {
            return Build(null, doc);
        }

        /// <summary>
        /// Translates any added <seealso cref="FacetField"/>s into normal fields for indexing.
        /// 
        /// <para>
        /// <b>NOTE:</b> you should add the returned document to IndexWriter, not the
        /// input one!
        /// </para>
        /// </summary>
        public virtual Document Build(TaxonomyWriter taxoWriter, Document doc)
        {
            // Find all FacetFields, collated by the actual field:
            IDictionary<string, IList<FacetField>> byField = new Dictionary<string, IList<FacetField>>();

            // ... and also all SortedSetDocValuesFacetFields:
            IDictionary<string, IList<SortedSetDocValuesFacetField>> dvByField = new Dictionary<string, IList<SortedSetDocValuesFacetField>>();

            // ... and also all AssociationFacetFields
            IDictionary<string, IList<AssociationFacetField>> assocByField = new Dictionary<string, IList<AssociationFacetField>>();

            var seenDims = new HashSet<string>();

            foreach (IndexableField field in doc.Fields)
            {
                if (field.FieldType == FacetField.TYPE)
                {
                    FacetField facetField = (FacetField)field;
                    FacetsConfig.DimConfig dimConfig = GetDimConfig(facetField.dim);
                    if (dimConfig.MultiValued == false)
                    {
                        CheckSeen(seenDims, facetField.dim);
                    }
                    string indexFieldName = dimConfig.IndexFieldName;
                    IList<FacetField> fields;
                    if (!byField.TryGetValue(indexFieldName, out fields))
                    {
                        fields = new List<FacetField>();
                        byField[indexFieldName] = fields;
                    }
                    fields.Add(facetField);
                }

                if (field.FieldType == SortedSetDocValuesFacetField.TYPE)
                {
                    var facetField = (SortedSetDocValuesFacetField)field;
                    FacetsConfig.DimConfig dimConfig = GetDimConfig(facetField.Dim);
                    if (dimConfig.MultiValued == false)
                    {
                        CheckSeen(seenDims, facetField.Dim);
                    }
                    string indexFieldName = dimConfig.IndexFieldName;
                    IList<SortedSetDocValuesFacetField> fields;
                    if (!dvByField.TryGetValue(indexFieldName, out fields))
                    {
                        fields = new List<SortedSetDocValuesFacetField>();
                        dvByField[indexFieldName] = fields;
                    }
                    fields.Add(facetField);
                }

                if (field.FieldType == AssociationFacetField.TYPE)
                {
                    AssociationFacetField facetField = (AssociationFacetField)field;
                    FacetsConfig.DimConfig dimConfig = GetDimConfig(facetField.dim);
                    if (dimConfig.MultiValued == false)
                    {
                        CheckSeen(seenDims, facetField.dim);
                    }
                    if (dimConfig.Hierarchical)
                    {
                        throw new System.ArgumentException("AssociationFacetField cannot be hierarchical (dim=\"" + facetField.dim + "\")");
                    }
                    if (dimConfig.RequireDimCount)
                    {
                        throw new System.ArgumentException("AssociationFacetField cannot requireDimCount (dim=\"" + facetField.dim + "\")");
                    }

                    string indexFieldName = dimConfig.IndexFieldName;
                    IList<AssociationFacetField> fields;
                    if (!assocByField.TryGetValue(indexFieldName, out fields))
                    {
                        fields = new List<AssociationFacetField>();
                        assocByField[indexFieldName] = fields;
                    }
                    fields.Add(facetField);

                    // Best effort: detect mis-matched types in same
                    // indexed field:
                    string type;
                    if (facetField is IntAssociationFacetField)
                    {
                        type = "int";
                    }
                    else if (facetField is FloatAssociationFacetField)
                    {
                        type = "float";
                    }
                    else
                    {
                        type = "bytes";
                    }
                    // NOTE: not thread safe, but this is just best effort:
                    string curType;
                    if (!assocDimTypes.TryGetValue(indexFieldName, out curType))
                    {
                        assocDimTypes[indexFieldName] = type;
                    }
                    else if (!curType.Equals(type))
                    {
                        throw new System.ArgumentException("mixing incompatible types of AssocationFacetField (" + curType + " and " + type + ") in indexed field \"" + indexFieldName + "\"; use FacetsConfig to change the indexFieldName for each dimension");
                    }
                }
            }

            Document result = new Document();

            ProcessFacetFields(taxoWriter, byField, result);
            processSSDVFacetFields(dvByField, result);
            ProcessAssocFacetFields(taxoWriter, assocByField, result);

            //System.out.println("add stored: " + addedStoredFields);

            foreach (IndexableField field in doc.Fields)
            {
                IndexableFieldType ft = field.FieldType;
                if (ft != FacetField.TYPE && ft != SortedSetDocValuesFacetField.TYPE && ft != AssociationFacetField.TYPE)
                {
                    result.Add(field);
                }
            }

            return result;
        }

        private void ProcessFacetFields(TaxonomyWriter taxoWriter, IDictionary<string, IList<FacetField>> byField, Document doc)
        {

            foreach (KeyValuePair<string, IList<FacetField>> ent in byField)
            {

                string indexFieldName = ent.Key;
                //System.out.println("  indexFieldName=" + indexFieldName + " fields=" + ent.getValue());

                IntsRef ordinals = new IntsRef(32);
                foreach (FacetField facetField in ent.Value)
                {

                    FacetsConfig.DimConfig ft = GetDimConfig(facetField.dim);
                    if (facetField.path.Length > 1 && ft.Hierarchical == false)
                    {
                        throw new System.ArgumentException("dimension \"" + facetField.dim + "\" is not hierarchical yet has " + facetField.path.Length + " components");
                    }

                    FacetLabel cp = new FacetLabel(facetField.dim, facetField.path);

                    checkTaxoWriter(taxoWriter);
                    int ordinal = taxoWriter.AddCategory(cp);
                    if (ordinals.Length == ordinals.Ints.Length)
                    {
                        ordinals.Grow(ordinals.Length + 1);
                    }
                    ordinals.Ints[ordinals.Length++] = ordinal;
                    //System.out.println("ords[" + (ordinals.length-1) + "]=" + ordinal);
                    //System.out.println("  add cp=" + cp);

                    if (ft.MultiValued && (ft.Hierarchical || ft.RequireDimCount))
                    {
                        //System.out.println("  add parents");
                        // Add all parents too:
                        int parent = taxoWriter.GetParent(ordinal);
                        while (parent > 0)
                        {
                            if (ordinals.Ints.Length == ordinals.Length)
                            {
                                ordinals.Grow(ordinals.Length + 1);
                            }
                            ordinals.Ints[ordinals.Length++] = parent;
                            parent = taxoWriter.GetParent(parent);
                        }

                        if (ft.RequireDimCount == false)
                        {
                            // Remove last (dimension) ord:
                            ordinals.Length--;
                        }
                    }

                    // Drill down:
                    for (int i = 1; i <= cp.Length; i++)
                    {
                        doc.Add(new StringField(indexFieldName, PathToString(cp.Components, i), Field.Store.NO));
                    }
                }

                // Facet counts:
                // DocValues are considered stored fields:
                doc.Add(new BinaryDocValuesField(indexFieldName, DedupAndEncode(ordinals)));
            }
        }

        public void processSSDVFacetFields(IDictionary<string, IList<SortedSetDocValuesFacetField>> byField, Document doc)
        {
            //System.out.println("process SSDV: " + byField);
            foreach (KeyValuePair<string, IList<SortedSetDocValuesFacetField>> ent in byField)
            {

                string indexFieldName = ent.Key;
                //System.out.println("  field=" + indexFieldName);

                foreach (SortedSetDocValuesFacetField facetField in ent.Value)
                {
                    FacetLabel cp = new FacetLabel(facetField.Dim, facetField.Label);
                    string fullPath = PathToString(cp.Components, cp.Length);
                    //System.out.println("add " + fullPath);

                    // For facet counts:
                    doc.Add(new SortedSetDocValuesField(indexFieldName, new BytesRef(fullPath)));

                    // For drill-down:
                    doc.Add(new StringField(indexFieldName, fullPath, Field.Store.NO));
                    doc.Add(new StringField(indexFieldName, facetField.Dim, Field.Store.NO));
                }
            }
        }

        private void ProcessAssocFacetFields(TaxonomyWriter taxoWriter, IDictionary<string, IList<AssociationFacetField>> byField, Document doc)
        {
            foreach (KeyValuePair<string, IList<AssociationFacetField>> ent in byField)
            {
                byte[] bytes = new byte[16];
                int upto = 0;
                string indexFieldName = ent.Key;
                foreach (AssociationFacetField field in ent.Value)
                {
                    // NOTE: we don't add parents for associations
                    checkTaxoWriter(taxoWriter);
                    FacetLabel label = new FacetLabel(field.dim, field.path);
                    int ordinal = taxoWriter.AddCategory(label);
                    if (upto + 4 > bytes.Length)
                    {
                        bytes = ArrayUtil.Grow(bytes, upto + 4);
                    }
                    // big-endian:
                    bytes[upto++] = (byte)(ordinal >> 24);
                    bytes[upto++] = (byte)(ordinal >> 16);
                    bytes[upto++] = (byte)(ordinal >> 8);
                    bytes[upto++] = (byte)ordinal;
                    if (upto + field.assoc.Length > bytes.Length)
                    {
                        bytes = ArrayUtil.Grow(bytes, upto + field.assoc.Length);
                    }
                    Array.Copy(field.assoc.Bytes, field.assoc.Offset, bytes, upto, field.assoc.Length);
                    upto += field.assoc.Length;

                    // Drill down:
                    for (int i = 1; i <= label.Length; i++)
                    {
                        doc.Add(new StringField(indexFieldName, PathToString(label.Components, i), Field.Store.NO));
                    }
                }
                doc.Add(new BinaryDocValuesField(indexFieldName, new BytesRef(bytes, 0, upto)));
            }
        }

        /// <summary>
        /// Encodes ordinals into a BytesRef; expert: subclass can
        ///  override this to change encoding. 
        /// </summary>
        protected virtual BytesRef DedupAndEncode(IntsRef ordinals)
        {
            Array.Sort(ordinals.Ints, ordinals.Offset, ordinals.Length);
            byte[] bytes = new byte[5 * ordinals.Length];
            int lastOrd = -1;
            int upto = 0;
            for (int i = 0; i < ordinals.Length; i++)
            {
                int ord = ordinals.Ints[ordinals.Offset + i];
                // ord could be == lastOrd, so we must dedup:
                if (ord > lastOrd)
                {
                    int delta;
                    if (lastOrd == -1)
                    {
                        delta = ord;
                    }
                    else
                    {
                        delta = ord - lastOrd;
                    }
                    if ((delta & ~0x7F) == 0)
                    {
                        bytes[upto] = (byte)delta;
                        upto++;
                    }
                    else if ((delta & ~0x3FFF) == 0)
                    {
                        bytes[upto] = unchecked((byte)(0x80 | ((delta & 0x3F80) >> 7)));
                        bytes[upto + 1] = (byte)(delta & 0x7F);
                        upto += 2;
                    }
                    else if ((delta & ~0x1FFFFF) == 0)
                    {
                        bytes[upto] = unchecked((byte)(0x80 | ((delta & 0x1FC000) >> 14)));
                        bytes[upto + 1] = unchecked((byte)(0x80 | ((delta & 0x3F80) >> 7)));
                        bytes[upto + 2] = (byte)(delta & 0x7F);
                        upto += 3;
                    }
                    else if ((delta & ~0xFFFFFFF) == 0)
                    {
                        bytes[upto] = unchecked((byte)(0x80 | ((delta & 0xFE00000) >> 21)));
                        bytes[upto + 1] = unchecked((byte)(0x80 | ((delta & 0x1FC000) >> 14)));
                        bytes[upto + 2] = unchecked((byte)(0x80 | ((delta & 0x3F80) >> 7)));
                        bytes[upto + 3] = (byte)(delta & 0x7F);
                        upto += 4;
                    }
                    else
                    {
                        bytes[upto] = unchecked((byte)(0x80 | ((delta & 0xF0000000) >> 28)));
                        bytes[upto + 1] = unchecked((byte)(0x80 | ((delta & 0xFE00000) >> 21)));
                        bytes[upto + 2] = unchecked((byte)(0x80 | ((delta & 0x1FC000) >> 14)));
                        bytes[upto + 3] = unchecked((byte)(0x80 | ((delta & 0x3F80) >> 7)));
                        bytes[upto + 4] = (byte)(delta & 0x7F);
                        upto += 5;
                    }
                    lastOrd = ord;
                }
            }

            return new BytesRef(bytes, 0, upto);
        }

        private void checkTaxoWriter(TaxonomyWriter taxoWriter)
        {
            if (taxoWriter == null)
            {
                throw new ThreadStateException("a non-null TaxonomyWriter must be provided when indexing FacetField or AssociationFacetField");
            }
        }

        // Joins the path components together:
        private const char DELIM_CHAR = '\u001F';

        // Escapes any occurrence of the path component inside the label:
        private const char ESCAPE_CHAR = '\u001E';

        /// <summary>
        /// Turns a dim + path into an encoded string. </summary>
        public static string PathToString(string dim, string[] path)
        {
            string[] fullPath = new string[1 + path.Length];
            fullPath[0] = dim;
            Array.Copy(path, 0, fullPath, 1, path.Length);
            return PathToString(fullPath, fullPath.Length);
        }

        /// <summary>
        /// Turns a dim + path into an encoded string. </summary>
        public static string PathToString(string[] path)
        {
            return PathToString(path, path.Length);
        }

        /// <summary>
        /// Turns the first {@code length} elements of {@code
        /// path} into an encoded string. 
        /// </summary>
        public static string PathToString(string[] path, int length)
        {
            if (length == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                string s = path[i];
                if (s.Length == 0)
                {
                    throw new System.ArgumentException("each path component must have length > 0 (got: \"\")");
                }
                int numChars = s.Length;
                for (int j = 0; j < numChars; j++)
                {
                    char ch = s[j];
                    if (ch == DELIM_CHAR || ch == ESCAPE_CHAR)
                    {
                        sb.Append(ESCAPE_CHAR);
                    }
                    sb.Append(ch);
                }
                sb.Append(DELIM_CHAR);
            }

            // Trim off last DELIM_CHAR:
            sb.Length = sb.Length - 1;
            return sb.ToString();
        }

        /// <summary>
        /// Turns an encoded string (from a previous call to {@link
        ///  #pathToString}) back into the original {@code
        ///  String[]}. 
        /// </summary>
        public static string[] StringToPath(string s)
        {
            IList<string> parts = new List<string>();
            int length = s.Length;
            if (length == 0)
            {
                return new string[0];
            }
            char[] buffer = new char[length];

            int upto = 0;
            bool lastEscape = false;
            for (int i = 0; i < length; i++)
            {
                char ch = s[i];
                if (lastEscape)
                {
                    buffer[upto++] = ch;
                    lastEscape = false;
                }
                else if (ch == ESCAPE_CHAR)
                {
                    lastEscape = true;
                }
                else if (ch == DELIM_CHAR)
                {
                    parts.Add(new string(buffer, 0, upto));
                    upto = 0;
                }
                else
                {
                    buffer[upto++] = ch;
                }
            }
            parts.Add(new string(buffer, 0, upto));
            Debug.Assert(!lastEscape);
            return parts.ToArray();
        }
    }
}