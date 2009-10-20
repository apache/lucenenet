/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using TermDocs = Lucene.Net.Index.TermDocs;
using TermEnum = Lucene.Net.Index.TermEnum;

namespace Lucene.Net.Search
{


    /// <summary> 
    /// 
    /// 
    /// </summary>
    public class ExtendedFieldCacheImpl : FieldCacheImpl, ExtendedFieldCache
    {
        public ExtendedFieldCacheImpl()
        {
            InitBlock();
        }
        public class AnonymousClassLongParser : LongParser
        {
            public virtual long ParseLong(System.String value_Renamed)
            {
                return System.Int64.Parse(value_Renamed);
            }
        }
        public class AnonymousClassDoubleParser : DoubleParser
        {
            public virtual double ParseDouble(System.String value_Renamed)
            {
                return System.Double.Parse(value_Renamed.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            }
        }

        new internal class AnonymousClassCache : Cache
        {
            public AnonymousClassCache(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
            public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override object CreateValue(IndexReader reader, object entryKey)
            {
                Entry entry = (Entry)entryKey;
                System.String field = entry.field;
                LongParser parser = (LongParser)entry.custom;
                long[] retArray = new long[reader.MaxDoc()];
                TermDocs termDocs = reader.TermDocs();
                TermEnum termEnum = reader.Terms(new Term(field));
                try
                {
                    do
                    {
                        Term term = termEnum.Term();
                        if (term == null || (object)term.Field() != (object)field)
                            break;
                        long termval = parser.ParseLong(term.Text());
                        termDocs.Seek(termEnum);
                        while (termDocs.Next())
                        {
                            retArray[termDocs.Doc()] = termval;
                        }
                    }
                    while (termEnum.Next());
                }
                finally
                {
                    termDocs.Close();
                    termEnum.Close();
                }
                return retArray;
            }
        }

        new internal class AnonymousClassCache1 : Cache
        {
            public AnonymousClassCache1(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
            public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override object CreateValue(IndexReader reader, object entryKey)
            {
                Entry entry = (Entry)entryKey;
                System.String field = entry.field;
                DoubleParser parser = (DoubleParser)entry.custom;
                double[] retArray = new double[reader.MaxDoc()];
                TermDocs termDocs = reader.TermDocs();
                TermEnum termEnum = reader.Terms(new Term(field));
                try
                {
                    do
                    {
                        Term term = termEnum.Term();
                        if (term == null || (object)term.Field() != (object)field)
                            break;
                        double termval = parser.ParseDouble(term.Text());
                        termDocs.Seek(termEnum);
                        while (termDocs.Next())
                        {
                            retArray[termDocs.Doc()] = termval;
                        }
                    }
                    while (termEnum.Next());
                }
                finally
                {
                    termDocs.Close();
                    termEnum.Close();
                }
                return retArray;
            }
        }

        new internal class AnonymousClassCache2 : Cache
        {
            public AnonymousClassCache2(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void InitBlock(Lucene.Net.Search.FieldCacheImpl enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private Lucene.Net.Search.FieldCacheImpl enclosingInstance;
            public Lucene.Net.Search.FieldCacheImpl Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }

            protected internal override object CreateValue(IndexReader reader, object fieldKey)
            {
                System.String field = String.Intern(((System.String)fieldKey));
                TermEnum enumerator = reader.Terms(new Term(field));
                try
                {
                    Term term = enumerator.Term();
                    if (term == null)
                    {
                        throw new System.SystemException("no terms in field " + field + " - cannot determine sort type");
                    }
                    object ret = null;
                    if ((object)term.Field() == (object)field)
                    {
                        System.String termtext = term.Text().Trim();

                        /**
                        * Java 1.4 level code:
						
                        if (pIntegers.matcher(termtext).matches())
                        return IntegerSortedHitQueue.comparator (reader, enumerator, field);
						
                        else if (pFloats.matcher(termtext).matches())
                        return FloatSortedHitQueue.comparator (reader, enumerator, field);
                        */

                        // Java 1.3 level code:
                        try
                        {
                            int parsedIntValue;
                            long parsedLongValue;
                            if (int.TryParse(termtext, out parsedIntValue))
                            {
                                ret = Enclosing_Instance.GetInts(reader, field);
                            }
                            else if (long.TryParse(termtext, out parsedLongValue))
                            {
                                ret = ((ExtendedFieldCacheImpl)Enclosing_Instance).GetLongs(reader, field);
                            }
                            else
                            {
                                float f = 0.0f;
                                if (SupportClass.Single.TryParse(termtext, out f))
                                {
                                    ret = Enclosing_Instance.GetFloats(reader, field);
                                }
                                else
                                {
                                    ret = Enclosing_Instance.GetStringIndex(reader, field);
                                }
                            }
                        }
                        catch (System.Exception)
                        {
                            ret = Enclosing_Instance.GetStringIndex(reader, field);
                        }
                    }
                    else
                    {
                        throw new System.SystemException("field \"" + field + "\" does not appear to be indexed");
                    }
                    return ret;
                }
                finally
                {
                    enumerator.Close();
                }
            }
        }
        private void InitBlock()
        {
            longsCache = new AnonymousClassCache(this);
            doublesCache = new AnonymousClassCache1(this);
            autoCache = new AnonymousClassCache2(this);
        }
        private static readonly LongParser LONG_PARSER;

        private static readonly DoubleParser DOUBLE_PARSER;


        public virtual long[] GetLongs(IndexReader reader, System.String field)
        {
            return GetLongs(reader, field, LONG_PARSER);
        }

        // inherit javadocs
        public virtual long[] GetLongs(IndexReader reader, System.String field, LongParser parser)
        {
            return (long[])longsCache.Get(reader, new Entry(field, parser));
        }

        internal Cache longsCache;

        // inherit javadocs
        public virtual double[] GetDoubles(IndexReader reader, System.String field)
        {
            return GetDoubles(reader, field, DOUBLE_PARSER);
        }

        // inherit javadocs
        public virtual double[] GetDoubles(IndexReader reader, System.String field, DoubleParser parser)
        {
            return (double[])doublesCache.Get(reader, new Entry(field, parser));
        }

        internal Cache doublesCache;


        // inherit javadocs
        public override object GetAuto(IndexReader reader, System.String field)
        {
            return autoCache.Get(reader, field);
        }

        new internal Cache autoCache;
        static ExtendedFieldCacheImpl()
        {
            LONG_PARSER = new AnonymousClassLongParser();
            DOUBLE_PARSER = new AnonymousClassDoubleParser();
        }
    }
}