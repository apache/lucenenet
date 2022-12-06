// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using JCG = J2N.Collections.Generic;

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
    using IIndexableField = Lucene.Net.Index.IIndexableField;
    using IIndexableFieldType = Lucene.Net.Index.IIndexableFieldType;
    using Int32AssociationFacetField = Lucene.Net.Facet.Taxonomy.Int32AssociationFacetField;
    using Int32sRef = Lucene.Net.Util.Int32sRef;
    using ITaxonomyWriter = Lucene.Net.Facet.Taxonomy.ITaxonomyWriter;
    using SingleAssociationFacetField = Lucene.Net.Facet.Taxonomy.SingleAssociationFacetField;
    using SortedSetDocValuesFacetField = Lucene.Net.Facet.SortedSet.SortedSetDocValuesFacetField;
    using SortedSetDocValuesField = Lucene.Net.Documents.SortedSetDocValuesField;
    using StringField = Lucene.Net.Documents.StringField;

    /// <summary>
    /// Records per-dimension configuration.  By default a
    /// dimension is flat, single valued and does
    /// not require count for the dimension; use
    /// the setters in this class to change these settings for
    /// each dim.
    /// 
    /// <para>
    /// <b>NOTE</b>: this configuration is not saved into the
    /// index, but it's vital, and up to the application to
    /// ensure, that at search time the provided <see cref="FacetsConfig"/>
    /// matches what was used during indexing.
    /// 
    ///  @lucene.experimental 
    /// </para>
    /// </summary>
    public class FacetsConfig
    {
        /// <summary>
        /// Which Lucene field holds the drill-downs and ords (as
        /// doc values). 
        /// </summary>
        public const string DEFAULT_INDEX_FIELD_NAME = "$facets";

        private readonly IDictionary<string, DimConfig> fieldTypes = new ConcurrentDictionary<string, DimConfig>();

        // Used only for best-effort detection of app mixing
        // int/float/bytes in a single indexed field:
        private readonly IDictionary<string, string> assocDimTypes = new ConcurrentDictionary<string, string>();

        private readonly object syncLock = new object(); // LUCENENET specific - avoid lock(this)

        /// <summary>
        /// Holds the configuration for one dimension
        /// 
        /// @lucene.experimental 
        /// </summary>
        public sealed class DimConfig
        {
            /// <summary>
            /// True if this dimension is hierarchical. </summary>
            public bool IsHierarchical { get; set; }

            /// <summary>
            /// True if this dimension is multi-valued. </summary>
            public bool IsMultiValued { get; set; }

            /// <summary>
            /// True if the count/aggregate for the entire dimension
            ///  is required, which is unusual (default is false). 
            /// </summary>
            public bool RequireDimCount { get; set; }

            /// <summary>
            /// Actual field where this dimension's facet labels
            ///  should be indexed 
            /// </summary>
            public string IndexFieldName { get; set; }

            /// <summary>
            /// Default constructor.
            /// </summary>
            public DimConfig()
            {
                IndexFieldName = DEFAULT_INDEX_FIELD_NAME;
            }
        }

        /// <summary>
        /// Default per-dimension configuration. </summary>
        public static readonly DimConfig DEFAULT_DIM_CONFIG = new DimConfig();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FacetsConfig()
        {
        }

        /// <summary>
        /// Get the default configuration for new dimensions.  Useful when
        /// the dimension is not known beforehand and may need different 
        /// global default settings, like <c>multivalue = true</c>.
        /// </summary>
        /// <returns>
        /// The default configuration to be used for dimensions that 
        /// are not yet set in the <see cref="FacetsConfig"/>
        /// </returns>
        protected virtual DimConfig DefaultDimConfig => DEFAULT_DIM_CONFIG;

        /// <summary>
        /// Get the current configuration for a dimension.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual DimConfig GetDimConfig(string dimName)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                if (!fieldTypes.TryGetValue(dimName, out DimConfig ft))
                {
                    ft = DefaultDimConfig;
                }
                return ft;
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        /// <summary>
        /// Pass <c>true</c> if this dimension is hierarchical
        /// (has depth > 1 paths). 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetHierarchical(string dimName, bool v)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                if (!fieldTypes.TryGetValue(dimName, out DimConfig fieldType))
                {
                    fieldTypes[dimName] = new DimConfig { IsHierarchical = v };
                }
                else
                {
                    fieldType.IsHierarchical = v;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        /// <summary>
        /// Pass <c>true</c> if this dimension may have more than
        /// one value per document. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetMultiValued(string dimName, bool v)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                if (!fieldTypes.TryGetValue(dimName, out DimConfig fieldType))
                {
                    fieldTypes[dimName] = new DimConfig { IsMultiValued = v };
                }
                else
                {
                    fieldType.IsMultiValued = v;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        /// <summary>
        /// Pass <c>true</c> if at search time you require
        /// accurate counts of the dimension, i.e. how many
        /// hits have this dimension. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SetRequireDimCount(string dimName, bool v)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                if (!fieldTypes.TryGetValue(dimName, out DimConfig fieldType))
                {
                    fieldTypes[dimName] = new DimConfig { RequireDimCount = v };
                }
                else
                {
                    fieldType.RequireDimCount = v;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        /// <summary>
        /// Specify which index field name should hold the
        /// ordinals for this dimension; this is only used by the
        /// taxonomy based facet methods. 
        /// </summary>
        public virtual void SetIndexFieldName(string dimName, string indexFieldName)
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                // LUCENENET: Eliminated extra lookup by using TryGetValue instead of ContainsKey
                if (!fieldTypes.TryGetValue(dimName, out DimConfig fieldType))
                {
                    fieldTypes[dimName] = new DimConfig { IndexFieldName = indexFieldName };
                }
                else
                {
                    fieldType.IndexFieldName = indexFieldName;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        /// <summary>
        /// Returns map of field name to <see cref="DimConfig"/>.
        /// </summary>
        public virtual IDictionary<string, DimConfig> DimConfigs => fieldTypes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckSeen(ISet<string> seenDims, string dim)
        {
            if (seenDims.Contains(dim))
            {
                throw new ArgumentException("dimension \"" + dim + "\" is not multiValued, but it appears more than once in this document");
            }
            seenDims.Add(dim);
        }

        /// <summary>
        /// Translates any added <see cref="FacetField"/>s into normal fields for indexing;
        /// only use this version if you did not add any taxonomy-based fields 
        /// (<see cref="FacetField"/> or <see cref="AssociationFacetField"/>).
        /// 
        /// <para>
        /// <b>NOTE:</b> you should add the returned document to <see cref="Index.IndexWriter"/>, not the
        /// input one!
        /// </para>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual Document Build(Document doc)
        {
            return Build(null, doc);
        }

        /// <summary>
        /// Translates any added <see cref="FacetField"/>s into normal fields for indexing.
        /// 
        /// <para>
        /// <b>NOTE:</b> you should add the returned document to <see cref="Index.IndexWriter"/>, not the
        /// input one!
        /// </para>
        /// </summary>
        public virtual Document Build(ITaxonomyWriter taxoWriter, Document doc)
        {
            // Find all FacetFields, collated by the actual field:
            IDictionary<string, IList<FacetField>> byField = new Dictionary<string, IList<FacetField>>();

            // ... and also all SortedSetDocValuesFacetFields:
            IDictionary<string, IList<SortedSetDocValuesFacetField>> dvByField = new Dictionary<string, IList<SortedSetDocValuesFacetField>>();

            // ... and also all AssociationFacetFields
            IDictionary<string, IList<AssociationFacetField>> assocByField = new Dictionary<string, IList<AssociationFacetField>>();

            var seenDims = new JCG.HashSet<string>();

            foreach (IIndexableField field in doc.Fields)
            {
                if (field.IndexableFieldType == FacetField.TYPE)
                {
                    FacetField facetField = (FacetField)field;
                    FacetsConfig.DimConfig dimConfig = GetDimConfig(facetField.Dim);
                    if (dimConfig.IsMultiValued == false)
                    {
                        CheckSeen(seenDims, facetField.Dim);
                    }
                    string indexFieldName = dimConfig.IndexFieldName;
                    if (!byField.TryGetValue(indexFieldName, out IList<FacetField> fields))
                    {
                        fields = new JCG.List<FacetField>();
                        byField[indexFieldName] = fields;
                    }
                    fields.Add(facetField);
                }

                if (field.IndexableFieldType == SortedSetDocValuesFacetField.TYPE)
                {
                    var facetField = (SortedSetDocValuesFacetField)field;
                    FacetsConfig.DimConfig dimConfig = GetDimConfig(facetField.Dim);
                    if (dimConfig.IsMultiValued == false)
                    {
                        CheckSeen(seenDims, facetField.Dim);
                    }
                    string indexFieldName = dimConfig.IndexFieldName;
                    if (!dvByField.TryGetValue(indexFieldName, out IList<SortedSetDocValuesFacetField> fields))
                    {
                        fields = new JCG.List<SortedSetDocValuesFacetField>();
                        dvByField[indexFieldName] = fields;
                    }
                    fields.Add(facetField);
                }

                if (field.IndexableFieldType == AssociationFacetField.TYPE)
                {
                    AssociationFacetField facetField = (AssociationFacetField)field;
                    FacetsConfig.DimConfig dimConfig = GetDimConfig(facetField.Dim);
                    if (dimConfig.IsMultiValued == false)
                    {
                        CheckSeen(seenDims, facetField.Dim);
                    }
                    if (dimConfig.IsHierarchical)
                    {
                        throw new ArgumentException("AssociationFacetField cannot be hierarchical (dim=\"" + facetField.Dim + "\")");
                    }
                    if (dimConfig.RequireDimCount)
                    {
                        throw new ArgumentException("AssociationFacetField cannot requireDimCount (dim=\"" + facetField.Dim + "\")");
                    }

                    string indexFieldName = dimConfig.IndexFieldName;
                    if (!assocByField.TryGetValue(indexFieldName, out IList<AssociationFacetField> fields))
                    {
                        fields = new JCG.List<AssociationFacetField>();
                        assocByField[indexFieldName] = fields;
                    }
                    fields.Add(facetField);

                    // Best effort: detect mis-matched types in same
                    // indexed field:
                    string type;
                    if (facetField is Int32AssociationFacetField)
                    {
                        type = "int";
                    }
                    else if (facetField is SingleAssociationFacetField)
                    {
                        type = "float";
                    }
                    else
                    {
                        type = "bytes";
                    }
                    // NOTE: not thread safe, but this is just best effort:
                    if (!assocDimTypes.TryGetValue(indexFieldName, out string curType))
                    {
                        assocDimTypes[indexFieldName] = type;
                    }
                    else if (!curType.Equals(type, StringComparison.Ordinal))
                    {
                        throw new ArgumentException("mixing incompatible types of AssocationFacetField (" + curType + " and " + type + ") in indexed field \"" + indexFieldName + "\"; use FacetsConfig to change the indexFieldName for each dimension");
                    }
                }
            }

            Document result = new Document();

            ProcessFacetFields(taxoWriter, byField, result);
            ProcessSSDVFacetFields(dvByField, result);
            ProcessAssocFacetFields(taxoWriter, assocByField, result);

            //System.out.println("add stored: " + addedStoredFields);

            foreach (IIndexableField field in doc.Fields)
            {
                IIndexableFieldType ft = field.IndexableFieldType;
                if (ft != FacetField.TYPE && ft != SortedSetDocValuesFacetField.TYPE && ft != AssociationFacetField.TYPE)
                {
                    result.Add(field);
                }
            }

            return result;
        }

        private void ProcessFacetFields(ITaxonomyWriter taxoWriter, IDictionary<string, IList<FacetField>> byField, Document doc)
        {

            foreach (KeyValuePair<string, IList<FacetField>> ent in byField)
            {

                string indexFieldName = ent.Key;
                //System.out.println("  indexFieldName=" + indexFieldName + " fields=" + ent.getValue());

                Int32sRef ordinals = new Int32sRef(32);
                foreach (FacetField facetField in ent.Value)
                {

                    FacetsConfig.DimConfig ft = GetDimConfig(facetField.Dim);
                    if (facetField.Path.Length > 1 && ft.IsHierarchical == false)
                    {
                        throw new ArgumentException("dimension \"" + facetField.Dim + "\" is not hierarchical yet has " + facetField.Path.Length + " components");
                    }

                    FacetLabel cp = new FacetLabel(facetField.Dim, facetField.Path);

                    CheckTaxoWriter(taxoWriter);
                    int ordinal = taxoWriter.AddCategory(cp);
                    if (ordinals.Length == ordinals.Int32s.Length)
                    {
                        ordinals.Grow(ordinals.Length + 1);
                    }
                    ordinals.Int32s[ordinals.Length++] = ordinal;
                    //System.out.println("ords[" + (ordinals.length-1) + "]=" + ordinal);
                    //System.out.println("  add cp=" + cp);

                    if (ft.IsMultiValued && (ft.IsHierarchical || ft.RequireDimCount))
                    {
                        //System.out.println("  add parents");
                        // Add all parents too:
                        int parent = taxoWriter.GetParent(ordinal);
                        while (parent > 0)
                        {
                            if (ordinals.Int32s.Length == ordinals.Length)
                            {
                                ordinals.Grow(ordinals.Length + 1);
                            }
                            ordinals.Int32s[ordinals.Length++] = parent;
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

        private static void ProcessSSDVFacetFields(IDictionary<string, IList<SortedSetDocValuesFacetField>> byField, Document doc) // LUCENENET: CA1822: Mark members as static
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

        private static void ProcessAssocFacetFields(ITaxonomyWriter taxoWriter, IDictionary<string, IList<AssociationFacetField>> byField, Document doc) // LUCENENET: CA1822: Mark members as static
        {
            foreach (KeyValuePair<string, IList<AssociationFacetField>> ent in byField)
            {
                byte[] bytes = new byte[16];
                int upto = 0;
                string indexFieldName = ent.Key;
                foreach (AssociationFacetField field in ent.Value)
                {
                    // NOTE: we don't add parents for associations
                    CheckTaxoWriter(taxoWriter);
                    FacetLabel label = new FacetLabel(field.Dim, field.Path);
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
                    if (upto + field.Assoc.Length > bytes.Length)
                    {
                        bytes = ArrayUtil.Grow(bytes, upto + field.Assoc.Length);
                    }
                    Arrays.Copy(field.Assoc.Bytes, field.Assoc.Offset, bytes, upto, field.Assoc.Length);
                    upto += field.Assoc.Length;

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
        /// Encodes ordinals into a <see cref="BytesRef"/>; expert: subclass can
        /// override this to change encoding. 
        /// </summary>
        protected virtual BytesRef DedupAndEncode(Int32sRef ordinals)
        {
            Array.Sort(ordinals.Int32s, ordinals.Offset, ordinals.Length);
            byte[] bytes = new byte[5 * ordinals.Length];
            int lastOrd = -1;
            int upto = 0;
            for (int i = 0; i < ordinals.Length; i++)
            {
                int ord = ordinals.Int32s[ordinals.Offset + i];
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckTaxoWriter(ITaxonomyWriter taxoWriter) // LUCENENET: CA1822: Mark members as static
        {
            if (taxoWriter is null)
            {
                throw new ThreadStateException("a non-null ITaxonomyWriter must be provided when indexing FacetField or AssociationFacetField");
            }
        }

        // Joins the path components together:
        private const char DELIM_CHAR = '\u001F';

        // Escapes any occurrence of the path component inside the label:
        private const char ESCAPE_CHAR = '\u001E';

        /// <summary>
        /// Turns a dim + path into an encoded string.
        /// </summary>
        public static string PathToString(string dim, string[] path)
        {
            string[] fullPath = new string[1 + path.Length];
            fullPath[0] = dim;
            Arrays.Copy(path, 0, fullPath, 1, path.Length);
            return PathToString(fullPath, fullPath.Length);
        }

        /// <summary>
        /// Turns a dim + path into an encoded string.
        /// </summary>
        public static string PathToString(string[] path)
        {
            return PathToString(path, path.Length);
        }

        /// <summary>
        /// Turns the first <paramref name="length"/> elements of <paramref name="path"/>
        /// into an encoded string. 
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
                    throw new ArgumentException("each path component must have length > 0 (got: \"\")");
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
            sb.Length -= 1;
            return sb.ToString();
        }

        /// <summary>
        /// Turns an encoded string (from a previous call to <see cref="PathToString(string[])"/>) 
        /// back into the original <see cref="T:string[]"/>. 
        /// </summary>
        public static string[] StringToPath(string s)
        {
            JCG.List<string> parts = new JCG.List<string>();
            int length = s.Length;
            if (length == 0)
            {
                return Arrays.Empty<string>();
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
            if (Debugging.AssertsEnabled) Debugging.Assert(!lastEscape);
            return parts.ToArray();
        }
    }
}