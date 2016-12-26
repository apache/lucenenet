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

namespace Lucene.Net.Codecs.BlockTerms
{

    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Index;
    using Store;
    using Util;
    using Util.Fst;
    
    /// <summary>
    /// See VariableGapTermsIndexWriter
    /// 
    /// lucene.experimental
    /// </summary>
    public class VariableGapTermsIndexReader : TermsIndexReaderBase
    {
        private readonly int _indexDivisor;
        private readonly IndexInput _input;       // Closed if indexLoaded is true:
        private readonly int _version;

        private volatile bool _indexLoaded;
        private long _dirOffset;                 // start of the field info data

        private readonly PositiveIntOutputs _fstOutputs = PositiveIntOutputs.Singleton;
        private readonly Dictionary<FieldInfo, FieldIndexData> _fields = new Dictionary<FieldInfo, FieldIndexData>();
        
        public VariableGapTermsIndexReader(Directory dir, FieldInfos fieldInfos, String segment, int indexDivisor,
            String segmentSuffix, IOContext context)
        {
            _input =
                dir.OpenInput(
                    IndexFileNames.SegmentFileName(segment, segmentSuffix,
                        VariableGapTermsIndexWriter.TERMS_INDEX_EXTENSION), new IOContext(context, true));
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
                var numFields = _input.ReadVInt();
                if (numFields < 0)
                {
                    throw new CorruptIndexException("invalid numFields: " + numFields + " (resource=" + _input + ")");
                }

                for (var i = 0; i < numFields; i++)
                {
                    var field = _input.ReadVInt();
                    var indexStart = _input.ReadVLong();
                    var fieldInfo = fieldInfos.FieldInfo(field);
                    
                    try
                    {
                        _fields.Add(fieldInfo, new FieldIndexData(indexStart, this));
                    }
                    catch (ArgumentException)
                    {
                        throw new CorruptIndexException(String.Format("Duplicate Field: {0}, Resource: {1}",
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

        private int ReadHeader(IndexInput input)
        {
            int version = CodecUtil.CheckHeader(input, VariableGapTermsIndexWriter.CODEC_NAME,
                VariableGapTermsIndexWriter.VERSION_START, VariableGapTermsIndexWriter.VERSION_CURRENT);
            if (version < VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                _dirOffset = input.ReadLong();
            }
            return version;
        }

        public override void Dispose()
        {
            if (_input != null && !_indexLoaded) { 
                _input.Dispose(); 
            } 
        }

        public override bool SupportsOrd
        {
            get { return false; }
        }
        
        public override int Divisor
        {
            get { return _indexDivisor; }
        }

        public override FieldIndexEnum GetFieldEnum(FieldInfo fieldInfo)
        {
            FieldIndexData fieldData = _fields[fieldInfo];
            return fieldData.Fst == null ? null : new IndexEnum(fieldData.Fst);
        }

        private void SeekDir(IndexInput input, long dirOffset)
        {
            if (_version >= VariableGapTermsIndexWriter.VERSION_CHECKSUM)
            {
                input.Seek(input.Length - CodecUtil.FooterLength() - 8);
                dirOffset = input.ReadLong();
            }
            else if (_version >= VariableGapTermsIndexWriter.VERSION_APPEND_ONLY)
            {
                input.Seek(input.Length - 8);
                dirOffset = input.ReadLong();
            }
            input.Seek(dirOffset);
        }

        public override long RamBytesUsed
        {
            get { return _fields.Values.Sum(entry => entry.RamBytesUsed()); }
        }

        internal class FieldIndexData
        {

            private readonly long _indexStart;
            // Set only if terms index is loaded:
            public volatile FST<long?> Fst;
            private readonly VariableGapTermsIndexReader _vgtir;

            public FieldIndexData(long indexStart, VariableGapTermsIndexReader vgtir)
            {
                _vgtir = vgtir;
                _indexStart = indexStart;

                if (_vgtir._indexDivisor > 0)
                    LoadTermsIndex();
            }

            private void LoadTermsIndex()
            {
                if (Fst != null) return;

                var clone = (IndexInput) _vgtir._input.Clone();
                clone.Seek(_indexStart);
                Fst = new FST<long?>(clone, _vgtir._fstOutputs);
                clone.Dispose();

                /*
                final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
                Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
                Util.toDot(fst, w, false, false);
                System.out.println("FST INDEX: SAVED to " + dotFileName);
                w.close();
                */

                if (_vgtir._indexDivisor > 1)
                {
                    // subsample
                    var scratchIntsRef = new IntsRef();
                    var outputs = PositiveIntOutputs.Singleton;
                    var builder = new Builder<long?>(FST.INPUT_TYPE.BYTE1, outputs);
                    var fstEnum = new BytesRefFSTEnum<long?>(Fst);
                    var count = _vgtir._indexDivisor;
                        
                    BytesRefFSTEnum.InputOutput<long?> result;
                    while ((result = fstEnum.Next()) != null)
                    {
                        if (count == _vgtir._indexDivisor)
                        {
                            builder.Add(Util.ToIntsRef(result.Input, scratchIntsRef), result.Output);
                            count = 0;
                        }
                        count++;
                    }
                    Fst = builder.Finish();
                }
            }

            /// <summary>Returns approximate RAM bytes used</summary>
            public long RamBytesUsed()
            {
                return Fst == null ? 0 : Fst.SizeInBytes();
            }
        }

        protected class IndexEnum : FieldIndexEnum
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
                set { }
            }

            public override long? Seek(BytesRef target)
            {
                _current = _fstEnum.SeekFloor(target);
                return _current.Output;
            }

            public override long? Next
            {
                get
                {
                    _current = _fstEnum.Next();
                    if (_current == null)
                        return -1;

                    return _current.Output;
                }
            }

            public override long Ord
            {
                get { throw new NotImplementedException(); }
                set { }
            }

            public override long? Seek(long ord)
            {
                throw new NotImplementedException();
            }
        }

    }
}
