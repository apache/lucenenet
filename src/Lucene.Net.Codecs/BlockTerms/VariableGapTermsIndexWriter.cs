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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Lucene.Net.Codecs;
using Lucene.Net.Codecs.BlockTerms;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;

namespace Lucene.Net.Codecs.BlockTerms
{
    
}
/**
 * Selects index terms according to provided pluggable
 * {@link IndexTermSelector}, and stores them in a prefix trie that's
 * loaded entirely in RAM stored as an FST.  This terms
 * index only supports unsigned byte term sort order
 * (unicode codepoint order when the bytes are UTF8).
 *
 * @lucene.experimental */
public class VariableGapTermsIndexWriter : TermsIndexWriterBase {
  protected IndexOutput output;

  /** Extension of terms index file */
  public const String TERMS_INDEX_EXTENSION = "tiv";

 public const String CODEC_NAME = "VARIABLE_GAP_TERMS_INDEX";
 public const int VERSION_START = 0;
public const int VERSION_APPEND_ONLY = 1;
  public const int VERSION_CHECKSUM = 2;
  public const int VERSION_CURRENT = VERSION_CHECKSUM;

  private readonly List<FSTFieldWriter> fields = new ArrayList<>();
  
  @SuppressWarnings("unused") private final FieldInfos fieldInfos; // unread
  private final IndexTermSelector policy;

  /** 
   * Hook for selecting which terms should be placed in the terms index.
   * <p>
   * {@link #newField} is called at the start of each new field, and
   * {@link #isIndexTerm} for each term in that field.
   * 
   * @lucene.experimental 
   */

    public abstract class IndexTermSelector
    {
        /// <summary>
        /// Called sequentially on every term being written
        /// returning true if this term should be indexed
        /// </summary>
        public abstract bool IsIndexTerm(BytesRef term, TermStats stats);
        
        /// <summary>Called when a new field is started</summary>
        public abstract void NewField(FieldInfo fieldInfo);
    }

    /// <remarks>
    /// Same policy as {@link FixedGapTermsIndexWriter}
    /// </remarks>
    public sealed class EveryNTermSelector : IndexTermSelector
    {
        private int count;
        private readonly int interval;

        public EveryNTermSelector(int interval)
        {
            this.interval = interval;
            // First term is first indexed term:
            count = interval;
        }

        public override bool IsIndexTerm(BytesRef term, TermStats stats)
        {
            if (count >= interval)
            {
                count = 1;
                return true;
            }
            else
            {
                count++;
                return false;
            }
        }

        public override void NewField(FieldInfo fieldInfo)
        {
            count = interval;
        }
    }

    /// <summary>
    /// Sets an index term when docFreq >= docFreqThresh, or
    /// every interval terms.  This should reduce seek time
    /// to high docFreq terms. 
    /// </summary>
    public class EveryNOrDocFreqTermSelector : IndexTermSelector
    {
        private int count;
        private readonly int docFreqThresh;
        private readonly int interval;

        public EveryNOrDocFreqTermSelector(int docFreqThresh, int interval)
        {
            this.interval = interval;
            this.docFreqThresh = docFreqThresh;

            // First term is first indexed term:
            count = interval;
        }

        public override bool IsIndexTerm(BytesRef term, TermStats stats)
        {
            if (stats.DocFreq >= docFreqThresh || count >= interval)
            {
                count = 1;
                return true;
            }
            else
            {
                count++;
                return false;
            }
        }

        public override void NewField(FieldInfo fieldInfo)
        {
            count = interval;
        }
    }

    // TODO: it'd be nice to let the FST builder prune based
  // on term count of each node (the prune1/prune2 that it
  // accepts), and build the index based on that.  This
  // should result in a more compact terms index, more like
  // a prefix trie than the other selectors, because it
  // only stores enough leading bytes to get down to N
  // terms that may complete that prefix.  It becomes
  // "deeper" when terms are dense, and "shallow" when they
  // are less dense.
  //
  // However, it's not easy to make that work this this
  // API, because that pruning doesn't immediately know on
  // seeing each term whether that term will be a seek point
  // or not.  It requires some non-causality in the API, ie
  // only on seeing some number of future terms will the
  // builder decide which past terms are seek points.
  // Somehow the API'd need to be able to return a "I don't
  // know" value, eg like a Future, which only later on is
  // flipped (frozen) to true or false.
  //
  // We could solve this with a 2-pass approach, where the
  // first pass would build an FSA (no outputs) solely to
  // determine which prefixes are the 'leaves' in the
  // pruning. The 2nd pass would then look at this prefix
  // trie to mark the seek points and build the FST mapping
  // to the true output.
  //
  // But, one downside to this approach is that it'd result
  // in uneven index term selection.  EG with prune1=10, the
  // resulting index terms could be as frequent as every 10
  // terms or as rare as every <maxArcCount> * 10 (eg 2560),
  // in the extremes.

    public VariableGapTermsIndexWriter(SegmentWriteState state, IndexTermSelector policy)
    {
        string indexFileName = IndexFileNames.SegmentFileName(state.SegmentInfo.Name, state.SegmentSuffix,
            TERMS_INDEX_EXTENSION);
        output = state.Directory.CreateOutput(indexFileName, state.Context);
        bool success = false;
        try
        {
            FieldInfos = state.FieldInfos;
            this.Policy = policy;
            writeHeader(output);
            success = true;
        }
        finally
        {
            if (!success)
            {
                IOUtils.CloseWhileHandlingException(output);
            }
        }
    }

    private void WriteHeader(IndexOutput output)
    {
        CodecUtil.WriteHeader(output, CODEC_NAME, VERSION_CURRENT);
    }

    public override FieldWriter AddField(FieldInfo field, long termsFilePointer)
    {
        ////System.out.println("VGW: field=" + field.name);
        Policy.newField(field);
        FSTFieldWriter writer = new FSTFieldWriter(field, termsFilePointer);
        fields.Add(writer);
        return writer;
    }

    /** NOTE: if your codec does not sort in unicode code
   *  point order, you must override this method, to simply
   *  return indexedTerm.length. */

    protected int IndexedTermPrefixLength(BytesRef priorTerm, BytesRef indexedTerm)
    {
        // As long as codec sorts terms in unicode codepoint
        // order, we can safely strip off the non-distinguishing
        // suffix to save RAM in the loaded terms index.
        int idxTermOffset = indexedTerm.Offset;
        int priorTermOffset = priorTerm.Offset;
        int limit = Math.Min(priorTerm.Length, indexedTerm.Length);
        for (int byteIdx = 0; byteIdx < limit; byteIdx++)
        {
            if (priorTerm.Bytes[priorTermOffset + byteIdx] != indexedTerm.Bytes[idxTermOffset + byteIdx])
            {
                return byteIdx + 1;
            }
        }

        return Math.Min(1 + priorTerm.Length, indexedTerm.Length);
    }

    private class FSTFieldWriter : FieldWriter
    {
        private readonly Builder<long> fstBuilder;
        private readonly PositiveIntOutputs fstOutputs;
        private readonly long startTermsFilePointer;

        public FieldInfo fieldInfo;
        private FST<long> fst;
        private long indexStart;

        private readonly BytesRef lastTerm = new BytesRef();
        private bool first = true;

        public FSTFieldWriter(FieldInfo fieldInfo, long termsFilePointer)
        {
            this.fieldInfo = fieldInfo;
            fstOutputs = PositiveIntOutputs.Singleton;
            fstBuilder = new Builder<>(FST.INPUT_TYPE.BYTE1, fstOutputs);
            indexStart = output.FilePointer;
            ////System.out.println("VGW: field=" + fieldInfo.name);

            // Always put empty string in
            fstBuilder.Add(new IntsRef(), termsFilePointer);
            startTermsFilePointer = termsFilePointer;
        }

        public override bool CheckIndexTerm(BytesRef text, TermStats stats)
        {
            //System.out.println("VGW: index term=" + text.utf8ToString());
            // NOTE: we must force the first term per field to be
            // indexed, in case policy doesn't:
            if (policy.isIndexTerm(text, stats) || first)
            {
                first = false;
                //System.out.println("  YES");
                return true;
            }
            else
            {
                lastTerm.CopyBytes(text);
                return false;
            }
        }

        private readonly IntsRef scratchIntsRef = new IntsRef();

        public override void Add(BytesRef text, TermStats stats, long termsFilePointer)
        {
            if (text.Length == 0)
            {
                // We already added empty string in ctor
                Debug.Assert(termsFilePointer == startTermsFilePointer);
                return;
            }
            int lengthSave = text.Length;
            text.Length = IndexedTermPrefixLength(lastTerm, text);
            try
            {
                fstBuilder.Add(Util.ToIntsRef(text, scratchIntsRef), termsFilePointer);
            }
            finally
            {
                text.Length = lengthSave;
            }
            lastTerm.CopyBytes(text);
        }

        public override void Finish(long termsFilePointer)
        {
            fst = fstBuilder.Finish();
            if (fst != null)
            {
                fst.Save(output);
            }
        }
    }

    public void Dispose()
    {
        if (output != null)
        {
            try
            {
                long dirStart = output.FilePointer;
                int fieldCount = fields.Size;

                int nonNullFieldCount = 0;
                for (int i = 0; i < fieldCount; i++)
                {
                    FSTFieldWriter field = fields[i];
                    if (field.fst != null)
                    {
                        nonNullFieldCount++;
                    }
                }

                output.WriteVInt(nonNullFieldCount);
                for (int i = 0; i < fieldCount; i++)
                {
                    FSTFieldWriter field = fields[i];
                    if (field.Fst != null)
                    {
                        output.WriteVInt(field.fieldInfo.Number);
                        output.WriteVLong(field.indexStart);
                    }
                }
                writeTrailer(dirStart);
                CodecUtil.WriteFooter(output);
            }
            finally
            {
                output.Dispose();
                output = null;
            }
        }
    }

    private void WriteTrailer(long dirStart)
    {
        output.WriteLong(dirStart);
    }
}
