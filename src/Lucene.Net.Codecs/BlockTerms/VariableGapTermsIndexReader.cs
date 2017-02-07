using Lucene.Net.Index;
using Lucene.Net.Store;
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
        private readonly PositiveIntOutputs _fstOutputs = PositiveIntOutputs.Singleton;
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
            var success = false;

            Debug.Assert(indexDivisor == -1 || indexDivisor > 0);

            try
            {
                _version = ReadHeader(_input);
                _indexDivisor = indexDivisor;

                if (_version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
                    CodecUtil.ChecksumEntireFile(_input);
                
                SeekDir(_input, _dirOffset);

                // Read directory
                var numFields = _input.ReadVInt32();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + _input + ")");
                }

                for (var i = 0; i < numFields; i++)
                {
                    var field = _input.ReadVInt32();
                    var indexStart = _input.ReadVInt64();
                    var fieldInfo = fieldInfos.FieldInfo(field);
                    
                    try
                    {
                        _fields.Add(fieldInfo, new FieldIndexData(this, indexStart));
                    }
                    catch (ArgumentException)
                    {
                        throw new CorruptIndexException(string.Format("Duplicate Field: {0}, Resource: {1}",
                            fieldInfo.Name, _input));
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
                _current = _fstEnum.SeekFloor(target);
                return _current.Output.GetValueOrDefault(); // LUCENENET NOTE: Not sure what to return if Output is null, so we are returning 0
            }

            public override long Next()
            {
                _current = _fstEnum.Next();
                if (_current == null)
                    return -1;

                return _current.Output.Value;
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
            
            public FieldIndexData(VariableGapTermsIndexReader outerInstance, long indexStart)
            {
                this.outerInstance = outerInstance;
                _indexStart = indexStart;

                if (this.outerInstance._indexDivisor > 0)
                    LoadTermsIndex();
            }

            private void LoadTermsIndex()
            {
                if (fst != null) return;

                var clone = (IndexInput)outerInstance._input.Clone();
                clone.Seek(_indexStart);
                fst = new FST<long?>(clone, outerInstance._fstOutputs);
                clone.Dispose();

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
                    var scratchIntsRef = new IntsRef();
                    var outputs = PositiveIntOutputs.Singleton;
                    var builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
                    var fstEnum = new BytesRefFSTEnum<long?>(fst);
                    var count = outerInstance._indexDivisor;

                    BytesRefFSTEnum.InputOutput<long?> result;
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

            /// <summary>Returns approximate RAM bytes used</summary>
            public virtual long RamBytesUsed()
            {
                return fst == null ? 0 : fst.SizeInBytes();
            }
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            FieldIndexData fieldData = _fields[fieldInfo];
            return fieldData.fst == null ? null : new IndexEnum(fieldData.fst);
        }

        public override void Dispose()
        {
            if (_input != null && !_indexLoaded) { 
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
            return _fields.Values.Sum(entry => entry.RamBytesUsed());
        }
    }
}
