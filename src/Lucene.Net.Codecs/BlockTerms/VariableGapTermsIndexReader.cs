using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Lucene.Net.Codecs.BlockTerms
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
    /// See VariableGapTermsIndexWriter
    /// 
    /// lucene.experimental
    /// </summary>
    public class VariableGapTermsIndexReader : TermsIndexReaderBase
    {
        private readonly PositiveInt32Outputs _fstOutputs = PositiveInt32Outputs.Singleton;
        private readonly int _indexDivisor;

        private readonly IndexInput _input;       // Closed if indexLoaded is true:
        private volatile bool _indexLoaded;

        private readonly Dictionary<FieldInfo, FieldIndexData> _fields = new Dictionary<FieldInfo, FieldIndexData>();

        private long _dirOffset;                 // start of the field info data

        private readonly int _version;

        private readonly string segment;
        
        public VariableGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, string segment, int indexDivisor,
            string segmentSuffix, IOContext context)
        {
            _input =
                dir.OpenInput(
                    IndexFileNames.SegmentFileName(segment, segmentSuffix,
                        VariableGapTermsIndexWriter.TERMS_INDEX_EXTENSION), new IOContext(context, true));
            this.segment = segment;
            bool success = false;
            Debug.Assert(indexDivisor == -1 || indexDivisor > 0);

            try
            {
                _version = ReadHeader(_input);
                _indexDivisor = indexDivisor;

                if (_version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
                {
                    CodecUtil.ChecksumEntireFile(_input);
                }
                
                SeekDir(_input, _dirOffset);

                // Read directory
                int numFields = _input.ReadVInt32();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + _input + ")");
                }

                for (var i = 0; i < numFields; i++)
                {
                    int field = _input.ReadVInt32();
                    long indexStart = _input.ReadVInt64();
                    FieldInfo fieldInfo = fieldInfos.FieldInfo(field);
                    FieldIndexData previous = _fields.Put(fieldInfo, new FieldIndexData(this, fieldInfo, indexStart));
                    if (previous != null)
                    {
                        throw new CorruptIndexException("duplicate field: " + fieldInfo.Name + " (resource=" + _input +")");
                    }
                }
                success = true;
            }
            finally
            {
                if (indexDivisor > 0)
                {
                    _input.Dispose();
                    _input = null;
                    if (success)
                    {
                        _indexLoaded = true;
                    }
                }
            }
        }

        public override int Divisor
        {
            get { return _indexDivisor; }
        }

        private int ReadHeader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, VariableGapTermsIndexWriter.CODEC_NAME,
                VariableGapTermsIndexWriter.VERSION_START, VariableGapTermsIndexWriter.VERSION_CURRENT);
            if (version < VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                _dirOffset = input.ReadInt64();
            }
            return version;
        }

        private class IndexEnum : FieldIndexEnum
        {
            private readonly BytesRefFSTEnum<long?> _fstEnum;
            private BytesRefFSTEnum.InputOutput<long?> _current;

            public IndexEnum(FST<long?> fst)
            {
                _fstEnum = new BytesRefFSTEnum<long?>(fst);
            }

            public override BytesRef Term
            {
                get { return _current == null ? null : _current.Input; }
            }

            public override long Seek(BytesRef target)
            {
                //System.out.println("VGR: seek field=" + fieldInfo.name + " target=" + target);
                _current = _fstEnum.SeekFloor(target);
                if (_current.Output.HasValue)
                {
                    //System.out.println("  got input=" + current.input + " output=" + current.output);
                    return _current.Output.Value;
                }
                throw new NullReferenceException("_current.Output is null"); // LUCENENET NOTE: NullReferenceException would be thrown in Java, so doing it here
            }

            public override long Next()
            {
                //System.out.println("VGR: next field=" + fieldInfo.name);
                _current = _fstEnum.Next();
                if (_current == null)
                {
                    //System.out.println("  eof");
                    return -1;
                }

                if (_current.Output.HasValue)
                {
                    return _current.Output.Value;
                }
                throw new NullReferenceException("_current.Output is null"); // LUCENENET NOTE: NullReferenceException would be thrown in Java, so doing it here
            }

            public override long Ord
            {
                get { throw new NotSupportedException(); } 
            }

            public override long Seek(long ord)
            {
                throw new NotSupportedException();
            }
        }

        public override bool SupportsOrd
        {
            get { return false; }
        }

        private class FieldIndexData
        {
            private readonly VariableGapTermsIndexReader outerInstance;

            private readonly long _indexStart;
            // Set only if terms index is loaded:
            internal volatile FST<long?> fst;
            
            public FieldIndexData(VariableGapTermsIndexReader outerInstance, FieldInfo fieldInfo, long indexStart)
            {
                this.outerInstance = outerInstance;

                _indexStart = indexStart;

                if (this.outerInstance._indexDivisor > 0)
                {
                    LoadTermsIndex();
                }  
            }

            private void LoadTermsIndex()
            {
                if (fst == null)
                {
                    IndexInput clone = (IndexInput)outerInstance._input.Clone();
                    clone.Seek(_indexStart);
                    fst = new FST<long?>(clone, outerInstance._fstOutputs);
                    clone.Dispose(); // LUCENENET TODO: No using block here is bad...

                    /*
                    final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
                    Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
                    Util.toDot(fst, w, false, false);
                    System.out.println("FST INDEX: SAVED to " + dotFileName);
                    w.close();
                    */

                    if (outerInstance._indexDivisor > 1)
                    {
                        // subsample
                        Int32sRef scratchIntsRef = new Int32sRef();
                        PositiveInt32Outputs outputs = PositiveInt32Outputs.Singleton;
                        Builder<long?> builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
                        BytesRefFSTEnum<long?> fstEnum = new BytesRefFSTEnum<long?>(fst);
                        BytesRefFSTEnum.InputOutput<long?> result;
                        int count = outerInstance._indexDivisor;
                        while ((result = fstEnum.Next()) != null)
                        {
                            if (count == outerInstance._indexDivisor)
                            {
                                builder.Add(Util.Fst.Util.ToInt32sRef(result.Input, scratchIntsRef), result.Output);
                                count = 0;
                            }
                            count++;
                        }
                        fst = builder.Finish();
                    }
                }
            }

            /// <summary>Returns approximate RAM bytes used</summary>
            public virtual long RamBytesUsed()
            {
                return fst == null ? 0 : fst.SizeInBytes();
            }
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            FieldIndexData fieldData;
            if (!_fields.TryGetValue(fieldInfo, out fieldData) || fieldData == null)
            {
                return null;
            }
            else
            {
                return new IndexEnum(fieldData.fst);
            }
        }

        public override void Dispose()
        {
            if (_input != null && !_indexLoaded)
            { 
                _input.Dispose(); 
            } 
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (_version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadInt64();
            }
            else if (_version >= VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadInt64();
            }
            input.Seek(dirOffset);
        }

        public override long RamBytesUsed()
        {
            long sizeInBytes = 0;
            foreach (FieldIndexData entry in _fields.Values)
            {
                sizeInBytes += entry.RamBytesUsed();
            }
            return sizeInBytes;
        }
    }
}
