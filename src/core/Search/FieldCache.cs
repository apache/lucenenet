using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lucene.Net.Search
{
    // .NET Port: This file has been refactored and rewritten to use the following:
    // FieldCache - static class containing static members of the java interface FieldCache
    // IFieldCache - equivalent to the java interface FieldCache's real interface members
    // FieldCacheImpl (in FieldCacheImpl.cs) - implementation of IFieldCache

    public static class FieldCache
    {
        public abstract class Bytes
        {
            public abstract sbyte Get(int docID);

            public static readonly Bytes EMPTY = new EmptyBytes();

            public sealed class EmptyBytes : Bytes
            {
                public override sbyte Get(int docID)
                {
                    return 0;
                }
            }
        }

        public abstract class Shorts
        {
            public abstract short Get(int docID);

            public static readonly Shorts EMPTY = new EmptyShorts();

            public sealed class EmptyShorts : Shorts
            {
                public override short Get(int docID)
                {
                    return 0;
                }
            }
        }

        public abstract class Ints
        {
            public abstract int Get(int docID);

            public static readonly Ints EMPTY = new EmptyInts();

            public sealed class EmptyInts : Ints
            {
                public override int Get(int docID)
                {
                    return 0;
                }
            }
        }

        public abstract class Longs
        {
            public abstract long Get(int docID);

            public static readonly Longs EMPTY = new EmptyLongs();

            public sealed class EmptyLongs : Longs
            {
                public override long Get(int docID)
                {
                    return 0;
                }
            }
        }

        public abstract class Floats
        {
            public abstract float Get(int docID);

            public static readonly Floats EMPTY = new EmptyFloats();

            public sealed class EmptyFloats : Floats
            {
                public override float Get(int docID)
                {
                    return 0;
                }
            }
        }

        public abstract class Doubles
        {
            public abstract double Get(int docID);

            public static readonly Doubles EMPTY = new EmptyDoubles();

            public sealed class EmptyDoubles : Doubles
            {
                public override double Get(int docID)
                {
                    return 0;
                }
            }
        }

        public static readonly SortedDocValues EMPTY_TERMSINDEX = new AnonymousEmptyTermsIndexSortedDocValues();

        private sealed class AnonymousEmptyTermsIndexSortedDocValues : SortedDocValues
        {
            public override int GetOrd(int docID)
            {
                return -1;
            }

            public override void LookupOrd(int ord, BytesRef result)
            {
                result.bytes = MISSING;
                result.offset = 0;
                result.length = 0;
            }

            public override int ValueCount
            {
                get { return 0; }
            }
        }

        public sealed class CreationPlaceholder
        {
            internal object value;
        }

        public interface IParser
        {
            TermsEnum TermsEnum(Terms terms);
        }

        public interface IByteParser : IParser
        {
            sbyte ParseByte(BytesRef term);
        }

        public interface IShortParser : IParser
        {
            short ParseShort(BytesRef term);
        }

        public interface IIntParser : IParser
        {
            int ParseInt(BytesRef term);
        }

        public interface IFloatParser : IParser
        {
            float ParseFloat(BytesRef term);
        }

        public interface ILongParser : IParser
        {
            long ParseLong(BytesRef term);
        }

        public interface IDoubleParser : IParser
        {
            double ParseDouble(BytesRef term);
        }

        public static IFieldCache DEFAULT = new FieldCacheImpl();

        public static readonly IByteParser DEFAULT_BYTE_PARSER = new AnonymousByteParser();

        private sealed class AnonymousByteParser : IByteParser
        {
            public sbyte ParseByte(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // IntField, instead, which already decodes
                // directly from byte[]
                return sbyte.Parse(term.Utf8ToString());
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".DEFAULT_BYTE_PARSER";
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        public static readonly IShortParser DEFAULT_SHORT_PARSER = new AnonymousShortParser();

        private sealed class AnonymousShortParser : IShortParser
        {
            public short ParseShort(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // IntField, instead, which already decodes
                // directly from byte[]
                return short.Parse(term.Utf8ToString());
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".DEFAULT_SHORT_PARSER";
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }
        }

        public static readonly IIntParser DEFAULT_INT_PARSER = new AnonymousIntParser();

        private sealed class AnonymousIntParser : IIntParser
        {
            public int ParseInt(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // IntField, instead, which already decodes
                // directly from byte[]
                return int.Parse(term.Utf8ToString());
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".DEFAULT_INT_PARSER";
            }
        }

        public static readonly IFloatParser DEFAULT_FLOAT_PARSER = new AnonymousFloatParser();

        private sealed class AnonymousFloatParser : IFloatParser
        {
            public float ParseFloat(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // FloatField, instead, which already decodes
                // directly from byte[]
                return float.Parse(term.Utf8ToString());
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".DEFAULT_FLOAT_PARSER";
            }
        }

        public static readonly ILongParser DEFAULT_LONG_PARSER = new AnonymousLongParser();

        private sealed class AnonymousLongParser : ILongParser
        {
            public long ParseLong(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // LongField, instead, which already decodes
                // directly from byte[]
                return long.Parse(term.Utf8ToString());
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".DEFAULT_LONG_PARSER";
            }
        }

        public static readonly IDoubleParser DEFAULT_DOUBLE_PARSER = new AnonymousDoubleParser();

        private sealed class AnonymousDoubleParser : IDoubleParser
        {
            public double ParseDouble(BytesRef term)
            {
                // TODO: would be far better to directly parse from
                // UTF8 bytes... but really users should use
                // DoubleField, instead, which already decodes
                // directly from byte[]
                return double.Parse(term.Utf8ToString());
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return terms.Iterator(null);
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".DEFAULT_DOUBLE_PARSER";
            }
        }

        public static readonly IIntParser NUMERIC_UTILS_INT_PARSER = new AnonymousNumericUtilsIntParser();

        private sealed class AnonymousNumericUtilsIntParser : IIntParser
        {
            public int ParseInt(BytesRef term)
            {
                return NumericUtils.PrefixCodedToInt(term);
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedInts(terms.Iterator(null));
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".NUMERIC_UTILS_INT_PARSER";
            }
        }

        public static readonly IFloatParser NUMERIC_UTILS_FLOAT_PARSER = new AnonymousNumericUtilsFloatParser();

        private sealed class AnonymousNumericUtilsFloatParser : IFloatParser
        {
            public float ParseFloat(BytesRef term)
            {
                return NumericUtils.SortableIntToFloat(NumericUtils.PrefixCodedToInt(term));
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".NUMERIC_UTILS_FLOAT_PARSER";
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedInts(terms.Iterator(null));
            }
        }

        public static readonly ILongParser NUMERIC_UTILS_LONG_PARSER = new AnonymousNumericUtilsLongParser();

        private sealed class AnonymousNumericUtilsLongParser : ILongParser
        {
            public long ParseLong(BytesRef term)
            {
                return NumericUtils.PrefixCodedToLong(term);
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".NUMERIC_UTILS_LONG_PARSER";
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedLongs(terms.Iterator(null));
            }
        }

        public static readonly IDoubleParser NUMERIC_UTILS_DOUBLE_PARSER = new AnonymousNumericUtilsDoubleParser();

        private sealed class AnonymousNumericUtilsDoubleParser : IDoubleParser
        {
            public double ParseDouble(BytesRef term)
            {
                return NumericUtils.SortableLongToDouble(NumericUtils.PrefixCodedToLong(term));
            }

            public override string ToString()
            {
                return typeof(FieldCache).FullName + ".NUMERIC_UTILS_DOUBLE_PARSER";
            }

            public TermsEnum TermsEnum(Terms terms)
            {
                return NumericUtils.FilterPrefixCodedLongs(terms.Iterator(null));
            }
        }

        // .NET Port: skipping down to about line 681 of java version. The actual interface methods of FieldCache are in IFieldCache below.
        public sealed class CacheEntry
        {
            private readonly object readerKey;
            private readonly string fieldName;
            private readonly Type cacheType;
            private readonly object custom;
            private readonly object value;
            private string size;

            public CacheEntry(object readerKey, string fieldName,
                      Type cacheType,
                      object custom,
                      object value)
            {
                this.readerKey = readerKey;
                this.fieldName = fieldName;
                this.cacheType = cacheType;
                this.custom = custom;
                this.value = value;
            }

            public object ReaderKey
            {
                get { return readerKey; }
            }

            public string FieldName
            {
                get { return fieldName; }
            }

            public Type CacheType
            {
                get { return cacheType; }
            }

            public object Custom
            {
                get { return custom; }
            }

            public object Value
            {
                get { return value; }
            }

            public void EstimateSize()
            {
                long bytesUsed = RamUsageEstimator.SizeOf(Value);
                size = RamUsageEstimator.HumanReadableUnits(bytesUsed);
            }

            public string EstimatedSize
            {
                get { return size; }
            }

            public override string ToString()
            {
                StringBuilder b = new StringBuilder();
                b.Append("'").Append(ReaderKey).Append("'=>");
                b.Append("'").Append(FieldName).Append("',");
                b.Append(CacheType).Append(",").Append(Custom);
                b.Append("=>").Append(Value.GetType().FullName).Append("#");
                b.Append(RuntimeHelpers.GetHashCode(Value));

                String s = EstimatedSize;
                if (null != s)
                {
                    b.Append(" (size =~ ").Append(s).Append(')');
                }

                return b.ToString();
            }
        }
    }

    public interface IFieldCache
    {
        IBits GetDocsWithField(AtomicReader reader, string field);

        /// <summary>Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <c>field</c> as a single byte and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the single byte values.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Bytes GetBytes(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <c>field</c> as bytes and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the bytes.
        /// </param>
        /// <param name="parser"> Computes byte for string values.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Bytes GetBytes(AtomicReader reader, string field, FieldCache.IByteParser parser, bool setDocsWithField);

        /// <summary>Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <c>field</c> as shorts and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the shorts.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Shorts GetShorts(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <c>field</c> as shorts and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the shorts.
        /// </param>
        /// <param name="parser"> Computes short for string values.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Shorts GetShorts(AtomicReader reader, string field, FieldCache.IShortParser parser, bool setDocsWithField);

        /// <summary>Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <c>field</c> as integers and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the integers.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Ints GetInts(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <c>field</c> as integers and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the integers.
        /// </param>
        /// <param name="parser"> Computes integer for string values.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Ints GetInts(AtomicReader reader, string field, FieldCache.IIntParser parser, bool setDocsWithField);

        /// <summary>Checks the internal cache for an appropriate entry, and if
        /// none is found, reads the terms in <c>field</c> as floats and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the floats.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Floats GetFloats(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary>Checks the internal cache for an appropriate entry, and if
        /// none is found, reads the terms in <c>field</c> as floats and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// </summary>
        /// <param name="reader"> Used to get field values.
        /// </param>
        /// <param name="field">  Which field contains the floats.
        /// </param>
        /// <param name="parser"> Computes float for string values.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException  If any error occurs. </throws>
        FieldCache.Floats GetFloats(AtomicReader reader, string field, FieldCache.IFloatParser parser, bool setDocsWithField);

        /// <summary> Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <c>field</c> as longs and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// 
        /// </summary>
        /// <param name="reader">Used to get field values.
        /// </param>
        /// <param name="field"> Which field contains the longs.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  java.io.IOException If any error occurs. </throws>
        FieldCache.Longs GetLongs(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary> Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <c>field</c> as longs and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field.
        /// 
        /// </summary>
        /// <param name="reader">Used to get field values.
        /// </param>
        /// <param name="field"> Which field contains the longs.
        /// </param>
        /// <param name="parser">Computes integer for string values.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException If any error occurs. </throws>
        FieldCache.Longs GetLongs(AtomicReader reader, string field, FieldCache.ILongParser parser, bool setDocsWithField);


        /// <summary> Checks the internal cache for an appropriate entry, and if none is
        /// found, reads the terms in <c>field</c> as integers and returns an array
        /// of size <c>reader.MaxDoc</c> of the value each document
        /// has in the given field.
        /// 
        /// </summary>
        /// <param name="reader">Used to get field values.
        /// </param>
        /// <param name="field"> Which field contains the doubles.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException If any error occurs. </throws>
        FieldCache.Doubles GetDoubles(AtomicReader reader, string field, bool setDocsWithField);

        /// <summary> Checks the internal cache for an appropriate entry, and if none is found,
        /// reads the terms in <c>field</c> as doubles and returns an array of
        /// size <c>reader.MaxDoc</c> of the value each document has in the
        /// given field.
        /// 
        /// </summary>
        /// <param name="reader">Used to get field values.
        /// </param>
        /// <param name="field"> Which field contains the doubles.
        /// </param>
        /// <param name="parser">Computes integer for string values.
        /// </param>
        /// <returns> The values in the given field for each document.
        /// </returns>
        /// <throws>  IOException If any error occurs. </throws>
        FieldCache.Doubles GetDoubles(AtomicReader reader, string field, FieldCache.IDoubleParser parser, bool setDocsWithField);

        BinaryDocValues GetTerms(AtomicReader reader, string field);

        BinaryDocValues GetTerms(AtomicReader reader, string field, float acceptableOverheadRatio);

        SortedDocValues GetTermsIndex(AtomicReader reader, string field);

        SortedDocValues GetTermsIndex(AtomicReader reader, string field, float acceptableOverheadRatio);

        SortedSetDocValues GetDocTermOrds(AtomicReader reader, string field);

        /// <summary> EXPERT: Generates an array of CacheEntry objects representing all items 
        /// currently in the FieldCache.
        /// <p/>
        /// NOTE: These CacheEntry objects maintain a strong refrence to the 
        /// Cached Values.  Maintaining refrences to a CacheEntry the IndexReader 
        /// associated with it has garbage collected will prevent the Value itself
        /// from being garbage collected when the Cache drops the WeakRefrence.
        /// <p/>
        /// <p/>
        /// <b>EXPERIMENTAL API:</b> This API is considered extremely advanced 
        /// and experimental.  It may be removed or altered w/o warning in future 
        /// releases 
        /// of Lucene.
        /// <p/>
        /// </summary>
        FieldCache.CacheEntry[] GetCacheEntries();

        /// <summary> <p/>
        /// EXPERT: Instructs the FieldCache to forcibly expunge all entries 
        /// from the underlying caches.  This is intended only to be used for 
        /// test methods as a way to ensure a known base state of the Cache 
        /// (with out needing to rely on GC to free WeakReferences).  
        /// It should not be relied on for "Cache maintenance" in general 
        /// application code.
        /// <p/>
        /// <p/>
        /// <b>EXPERIMENTAL API:</b> This API is considered extremely advanced 
        /// and experimental.  It may be removed or altered w/o warning in future 
        /// releases 
        /// of Lucene.
        /// <p/>
        /// </summary>
        void PurgeAllCaches();

        /// <summary>
        /// Expert: drops all cache entries associated with this
        /// reader.  NOTE: this reader must precisely match the
        /// reader that the cache entry is keyed on. If you pass a
        /// top-level reader, it usually will have no effect as
        /// Lucene now caches at the segment reader level.
        /// </summary>
        void Purge(AtomicReader r);

        /// <summary> Gets or sets the InfoStream for this FieldCache.
        /// <para>If non-null, FieldCacheImpl will warn whenever
        /// entries are created that are not sane according to
        /// <see cref="Lucene.Net.Util.FieldCacheSanityChecker" />.
        /// </para>
        /// </summary>
        StreamWriter InfoStream { get; set; }
    }
}
