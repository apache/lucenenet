using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial.Queries;
using Lucene.Net.Spatial.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Io;
using Spatial4n.Core.Shapes;
using System;
using System.Collections;
using System.IO;

namespace Lucene.Net.Spatial.Serialized
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
    /// A SpatialStrategy based on serializing a Shape stored into BinaryDocValues.
    /// This is not at all fast; it's designed to be used in conjuction with another index based
    /// SpatialStrategy that is approximated(like { @link org.apache.lucene.spatial.prefix.RecursivePrefixTreeStrategy})
    /// to add precision or eventually make more specific / advanced calculations on the per-document
    /// geometry.
    /// The serialization uses Spatial4j's {@link com.spatial4j.core.io.BinaryCodec}.
    ///
    /// @lucene.experimental
    /// </summary>
    public class SerializedDVStrategy : SpatialStrategy
    {
        /// <summary>
        /// A cache heuristic for the buf size based on the last shape size.
        /// </summary>
        //TODO do we make this non-volatile since it's merely a heuristic?
        private volatile int indexLastBufSize = 8 * 1024;//8KB default on first run

        /// <summary>
        /// Constructs the spatial strategy with its mandatory arguments.
        /// </summary>
        public SerializedDVStrategy(SpatialContext ctx, string fieldName)
            : base(ctx, fieldName)
        {
        }

        public override Field[] CreateIndexableFields(IShape shape)
        {
            int bufSize = Math.Max(128, (int)(this.indexLastBufSize * 1.5));//50% headroom over last
            ByteArrayOutputStream byteStream = new ByteArrayOutputStream(bufSize);
            BytesRef bytesRef = new BytesRef();//receiver of byteStream's bytes
            try
            {
                ctx.BinaryCodec.WriteShape(new BinaryWriter(byteStream), shape);

                //this is a hack to avoid redundant byte array copying by byteStream.toByteArray()
                byteStream.WriteTo(new OutputStreamAnonymousHelper(bytesRef));


                //            byteStream.WriteTo(new FilterOutputStream(null/*not used*/) {
                //    @Override
                //    public void write(byte[] b, int off, int len) throws IOException
                //    {
                //        bytesRef.bytes = b;
                //        bytesRef.offset = off;
                //        bytesRef.length = len;
                //    }
                //});
            }
            catch (IOException e)
            {
                throw new ApplicationException(e.Message, e);
            }
            this.indexLastBufSize = bytesRef.Length;//cache heuristic
            return new Field[] { new BinaryDocValuesField(FieldName, bytesRef) };
        }

        internal class OutputStreamAnonymousHelper : MemoryStream
        {
            private readonly BytesRef bytesRef;

            public OutputStreamAnonymousHelper(BytesRef bytesRef)
            {
                this.bytesRef = bytesRef;
            }

            public override void Write(byte[] buffer, int index, int count)
            {
                bytesRef.Bytes = buffer;
                bytesRef.Offset = index;
                bytesRef.Length = count;
            }
        }

        public override ValueSource MakeDistanceValueSource(IPoint queryPoint, double multiplier)
        {
            //TODO if makeShapeValueSource gets lifted to the top; this could become a generic impl.
            return new DistanceToShapeValueSource(MakeShapeValueSource(), queryPoint, multiplier, ctx);
        }

        public override ConstantScoreQuery MakeQuery(SpatialArgs args)
        {
            throw new NotSupportedException("This strategy can't return a query that operates" +
                " efficiently. Instead try a Filter or ValueSource.");
        }

        /// <summary>
        /// Returns a Filter that should be used with <see cref="FilteredQuery.QUERY_FIRST_FILTER_STRATEGY"/>.
        /// Use in another manner is likely to result in an <see cref="NotSupportedException"/>
        /// to prevent misuse because the filter can't efficiently work via iteration.
        /// </summary>
        public override Filter MakeFilter(SpatialArgs args)
        {
            ValueSource shapeValueSource = MakeShapeValueSource();
            ShapePredicateValueSource predicateValueSource = new ShapePredicateValueSource(
                shapeValueSource, args.Operation, args.Shape);
            return new PredicateValueSourceFilter(predicateValueSource);
        }

        /// <summary>
        /// Provides access to each shape per document as a ValueSource in which
        /// <see cref="FunctionValues.ObjectVal(int)"/> returns a <see cref="IShape"/>.
        /// </summary>
        //TODO raise to SpatialStrategy
        public virtual ValueSource MakeShapeValueSource()
        {
            return new ShapeDocValueSource(this, FieldName, ctx.BinaryCodec);
        }

        /// <summary>
        /// This filter only supports returning a DocSet with a GetBits(). If you try to grab the
        /// iterator then you'll get a <see cref="NotSupportedException"/>.
        /// </summary>
        internal class PredicateValueSourceFilter : Filter
        {
            private readonly ValueSource predicateValueSource;//we call boolVal(doc)

            public PredicateValueSourceFilter(ValueSource predicateValueSource)
            {
                this.predicateValueSource = predicateValueSource;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, Bits acceptDocs)
            {
                return new DocIdSetAnonymousHelper(this, context, acceptDocs);

                //      return new DocIdSet()
                //        {
                //            @Override
                //        public DocIdSetIterator iterator() throws IOException
                //        {
                //          throw new UnsupportedOperationException(
                //              "Iteration is too slow; instead try FilteredQuery.QUERY_FIRST_FILTER_STRATEGY");
                //        //Note that if you're truly bent on doing this, then see FunctionValues.getRangeScorer
                //    }

                //    @Override
                //        public Bits bits() throws IOException
                //    {
                //        //null Map context -- we simply don't have one. That's ok.
                //        final FunctionValues predFuncValues = predicateValueSource.getValues(null, context);

                //          return new Bits()
                //    {

                //        @Override
                //            public boolean get(int index)
                //    {
                //        if (acceptDocs != null && !acceptDocs.get(index))
                //            return false;
                //        return predFuncValues.boolVal(index);
                //    }

                //    @Override
                //            public int length()
                //    {
                //        return context.reader().maxDoc();
                //    }
                //};
                //  }
                //};
            }

            internal class DocIdSetAnonymousHelper : DocIdSet
            {
                private readonly PredicateValueSourceFilter outerInstance;
                private readonly AtomicReaderContext context;
                private readonly Bits acceptDocs;

                public DocIdSetAnonymousHelper(PredicateValueSourceFilter outerInstance, AtomicReaderContext context, Bits acceptDocs)
                {
                    this.outerInstance = outerInstance;
                    this.context = context;
                    this.acceptDocs = acceptDocs;
                }

                public override DocIdSetIterator GetIterator()
                {
                    throw new NotSupportedException(
                        "Iteration is too slow; instead try FilteredQuery.QUERY_FIRST_FILTER_STRATEGY");
                        //Note that if you're truly bent on doing this, then see FunctionValues.getRangeScorer
                }

                public override Bits GetBits()
                {
                    //null Map context -- we simply don't have one. That's ok.
                    FunctionValues predFuncValues = outerInstance.predicateValueSource.GetValues(null, context);

                    return new BitsAnonymousHelper(this, predFuncValues, context, acceptDocs);
                }

                internal class BitsAnonymousHelper : Bits
                {
                    private readonly DocIdSetAnonymousHelper outerInstance;
                    private readonly FunctionValues predFuncValues;
                    private readonly AtomicReaderContext context;
                    private readonly Bits acceptDocs;

                    public BitsAnonymousHelper(DocIdSetAnonymousHelper outerInstance, FunctionValues predFuncValues, AtomicReaderContext context, Bits acceptDocs)
                    {
                        this.outerInstance = outerInstance;
                        this.predFuncValues = predFuncValues;
                        this.context = context;
                        this.acceptDocs = acceptDocs;
                    }

                    public bool Get(int index)
                    {
                        if (acceptDocs != null && !acceptDocs.Get(index))
                            return false;
                        return predFuncValues.BoolVal(index);
                    }

                    public int Length()
                    {
                        return context.Reader.MaxDoc;
                    }
                }
            }

            public override bool Equals(object o)
            {
                if (this == o) return true;
                if (o == null || GetType() != o.GetType()) return false;

                PredicateValueSourceFilter that = (PredicateValueSourceFilter)o;

                if (!predicateValueSource.Equals(that.predicateValueSource)) return false;

                return true;
            }


            public override int GetHashCode()
            {
                return predicateValueSource.GetHashCode();
            }
        }//PredicateValueSourceFilter

        /// <summary>
        /// Implements a <see cref="ValueSource"/> by deserializing a <see cref="IShape"/> in from <see cref="BinaryDocValues"/> using <see cref="BinaryCodec"/>.
        /// </summary>
        /// <seealso cref="MakeShapeValueSource()"/>
        internal class ShapeDocValueSource : ValueSource
        {
            private readonly SerializedDVStrategy outerInstance;
            private readonly string fieldName;
            private readonly BinaryCodec binaryCodec;//spatial4n

            internal ShapeDocValueSource(SerializedDVStrategy outerInstance, string fieldName, BinaryCodec binaryCodec)
            {
                this.outerInstance = outerInstance;
                this.fieldName = fieldName;
                this.binaryCodec = binaryCodec;
            }

            public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
            {
                BinaryDocValues docValues = readerContext.AtomicReader.GetBinaryDocValues(fieldName);

                return new FuctionValuesAnonymousHelper(this, docValues);

                //      return new FunctionValues()
                //{
                //    int bytesRefDoc = -1;
                //    BytesRef bytesRef = new BytesRef();//scratch

                //    bool fillBytes(int doc) {
                //        if (bytesRefDoc != doc)
                //        {
                //            docValues.Get(doc, bytesRef);
                //            bytesRefDoc = doc;
                //        }
                //        return bytesRef.length != 0;
                //    }

                //    @Override
                //        public boolean exists(int doc)
                //{
                //    return fillBytes(doc);
                //}

                //@Override
                //        public boolean bytesVal(int doc, BytesRef target)
                //{
                //    if (fillBytes(doc))
                //    {
                //        target.bytes = bytesRef.bytes;
                //        target.offset = bytesRef.offset;
                //        target.length = bytesRef.length;
                //        return true;
                //    }
                //    else
                //    {
                //        target.length = 0;
                //        return false;
                //    }
                //}

                //@Override
                //        public Object objectVal(int docId)
                //{
                //    if (!fillBytes(docId))
                //        return null;
                //    DataInputStream dataInput = new DataInputStream(
                //        new ByteArrayInputStream(bytesRef.bytes, bytesRef.offset, bytesRef.length));
                //    try
                //    {
                //        return binaryCodec.readShape(dataInput);
                //    }
                //    catch (IOException e)
                //    {
                //        throw new RuntimeException(e);
                //    }
                //}

                //@Override
                //        public Explanation explain(int doc)
                //{
                //    return new Explanation(Float.NaN, toString(doc));
                //}

                //@Override
                //        public String toString(int doc)
                //{
                //    return description() + "=" + objectVal(doc);//TODO truncate?
                //}

                //      };
            }

            internal class FuctionValuesAnonymousHelper : FunctionValues
            {
                private readonly ShapeDocValueSource outerInstance;
                private readonly BinaryDocValues docValues;

                public FuctionValuesAnonymousHelper(ShapeDocValueSource outerInstance, BinaryDocValues docValues)
                {
                    this.outerInstance = outerInstance;
                    this.docValues = docValues;
                }

                private int bytesRefDoc = -1;
                private BytesRef bytesRef = new BytesRef();//scratch

                internal bool FillBytes(int doc)
                {
                    if (bytesRefDoc != doc)
                    {
                        docValues.Get(doc, bytesRef);
                        bytesRefDoc = doc;
                    }
                    return bytesRef.Length != 0;
                }

                public override bool Exists(int doc)
                {
                    return FillBytes(doc);
                }

                public override bool BytesVal(int doc, BytesRef target)
                {
                    if (FillBytes(doc))
                    {
                        target.Bytes = bytesRef.Bytes;
                        target.Offset = bytesRef.Offset;
                        target.Length = bytesRef.Length;
                        return true;
                    }
                    else
                    {
                        target.Length = 0;
                        return false;
                    }
                }

                public override object ObjectVal(int docId)
                {
                    if (!FillBytes(docId))
                        return null;
                    BinaryReader dataInput = new BinaryReader(
                        new MemoryStream(bytesRef.Bytes, bytesRef.Offset, bytesRef.Length));
                    try
                    {
                        return outerInstance.binaryCodec.ReadShape(dataInput);
                    }
                    catch (IOException e)
                    {
                        throw new ApplicationException(e.Message, e);
                    }
                }

                public override string ToString(int doc)
                {
                    return outerInstance.Description + "=" + ObjectVal(doc);//TODO truncate?
                }
            }

            public override bool Equals(object o)
            {
                if (this == o) return true;
                if (o == null || GetType() != o.GetType()) return false;

                ShapeDocValueSource that = (ShapeDocValueSource)o;

                if (!fieldName.Equals(that.fieldName)) return false;

                return true;
            }

            public override int GetHashCode()
            {
                int result = fieldName.GetHashCode();
                return result;
            }

            public override string Description
            {
                get
                {
                    return "shapeDocVal(" + fieldName + ")";
                }
            }

        }//ShapeDocValueSource
    }
}
