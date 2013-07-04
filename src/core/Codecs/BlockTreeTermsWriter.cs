using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Codecs
{
    public class BlockTreeTermsWriter : FieldsConsumer
    {
        public const int DEFAULT_MIN_BLOCK_SIZE = 25;

        public const int DEFAULT_MAX_BLOCK_SIZE = 48;

        internal const int OUTPUT_FLAGS_NUM_BITS = 2;
        internal const int OUTPUT_FLAGS_MASK = 0x3;
        internal const int OUTPUT_FLAG_IS_FLOOR = 0x1;
        internal const int OUTPUT_FLAG_HAS_TERMS = 0x2;

        internal const string TERMS_EXTENSION = "tim";
        internal const string TERMS_CODEC_NAME = "BLOCK_TREE_TERMS_DICT";

        public const int TERMS_VERSION_START = 0;

        public const int TERMS_VERSION_APPEND_ONLY = 1;

        public const int TERMS_VERSION_CURRENT = TERMS_VERSION_APPEND_ONLY;

        internal const string TERMS_INDEX_EXTENSION = "tip";
        internal const string TERMS_INDEX_CODEC_NAME = "BLOCK_TREE_TERMS_INDEX";

        public const int TERMS_INDEX_VERSION_START = 0;

        public const int TERMS_INDEX_VERSION_APPEND_ONLY = 1;

        public const int TERMS_INDEX_VERSION_CURRENT = TERMS_INDEX_VERSION_APPEND_ONLY;

        private readonly IndexOutput output;
        private readonly IndexOutput indexOut;
        internal readonly int minItemsInBlock;
        internal readonly int maxItemsInBlock;

        internal readonly PostingsWriterBase postingsWriter;
        internal readonly FieldInfos fieldInfos;
        internal FieldInfo currentField;

        private class FieldMetaData
        {
            public readonly FieldInfo fieldInfo;
            public readonly BytesRef rootCode;
            public readonly long numTerms;
            public readonly long indexStartFP;
            public readonly long sumTotalTermFreq;
            public readonly long sumDocFreq;
            public readonly int docCount;

            public FieldMetaData(FieldInfo fieldInfo, BytesRef rootCode, long numTerms, long indexStartFP, long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                //assert numTerms > 0;
                this.fieldInfo = fieldInfo;
                //assert rootCode != null: "field=" + fieldInfo.name + " numTerms=" + numTerms;
                this.rootCode = rootCode;
                this.indexStartFP = indexStartFP;
                this.numTerms = numTerms;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.sumDocFreq = sumDocFreq;
                this.docCount = docCount;
            }
        }

        private readonly IList<FieldMetaData> fields = new List<FieldMetaData>();

        public BlockTreeTermsWriter(
                              SegmentWriteState state,
                              PostingsWriterBase postingsWriter,
                              int minItemsInBlock,
                              int maxItemsInBlock)
        {
            if (minItemsInBlock <= 1)
            {
                throw new ArgumentException("minItemsInBlock must be >= 2; got " + minItemsInBlock);
            }
            if (maxItemsInBlock <= 0)
            {
                throw new ArgumentException("maxItemsInBlock must be >= 1; got " + maxItemsInBlock);
            }
            if (minItemsInBlock > maxItemsInBlock)
            {
                throw new ArgumentException("maxItemsInBlock must be >= minItemsInBlock; got maxItemsInBlock=" + maxItemsInBlock + " minItemsInBlock=" + minItemsInBlock);
            }
            if (2 * (minItemsInBlock - 1) > maxItemsInBlock)
            {
                throw new ArgumentException("maxItemsInBlock must be at least 2*(minItemsInBlock-1); got maxItemsInBlock=" + maxItemsInBlock + " minItemsInBlock=" + minItemsInBlock);
            }

            String termsFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_EXTENSION);
            output = state.directory.CreateOutput(termsFileName, state.context);
            bool success = false;
            IndexOutput indexOut = null;
            try
            {
                fieldInfos = state.fieldInfos;
                this.minItemsInBlock = minItemsInBlock;
                this.maxItemsInBlock = maxItemsInBlock;
                WriteHeader(output);

                //DEBUG = state.segmentName.equals("_4a");

                String termsIndexFileName = IndexFileNames.SegmentFileName(state.segmentInfo.name, state.segmentSuffix, TERMS_INDEX_EXTENSION);
                indexOut = state.directory.CreateOutput(termsIndexFileName, state.context);
                WriteIndexHeader(indexOut);

                currentField = null;
                this.postingsWriter = postingsWriter;
                // segment = state.segmentName;

                // System.out.println("BTW.init seg=" + state.segmentName);

                postingsWriter.Start(output);                          // have consumer write its format/header
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException((IDisposable)output, indexOut);
                }
            }
            this.indexOut = indexOut;
        }

        protected virtual void WriteHeader(IndexOutput output)
        {
            CodecUtil.WriteHeader(output, TERMS_CODEC_NAME, TERMS_VERSION_CURRENT);
        }

        protected virtual void WriteIndexHeader(IndexOutput output)
        {
            CodecUtil.WriteHeader(output, TERMS_INDEX_CODEC_NAME, TERMS_INDEX_VERSION_CURRENT);
        }

        protected void WriteTrailer(IndexOutput output, long dirStart)
        {
            output.WriteLong(dirStart);
        }

        protected void WriteIndexTrailer(IndexOutput indexOut, long dirStart)
        {
            indexOut.WriteLong(dirStart);
        }

        public override TermsConsumer AddField(FieldInfo field)
        {
            //DEBUG = field.name.equals("id");
            //if (DEBUG) System.out.println("\nBTTW.addField seg=" + segment + " field=" + field.name);
            //assert currentField == null || currentField.name.compareTo(field.name) < 0;
            currentField = field;
            return new TermsWriter(this, field);
        }

        internal static long EncodeOutput(long fp, bool hasTerms, bool isFloor)
        {
            //assert fp < (1L << 62);
            return (fp << 2) | (hasTerms ? OUTPUT_FLAG_HAS_TERMS : 0) | (isFloor ? OUTPUT_FLAG_IS_FLOOR : 0);
        }

        private class PendingEntry
        {
            public readonly bool isTerm;

            protected PendingEntry(bool isTerm)
            {
                this.isTerm = isTerm;
            }
        }

        private sealed class PendingTerm : PendingEntry
        {
            public readonly BytesRef term;
            public readonly TermStats stats;

            public PendingTerm(BytesRef term, TermStats stats)
                : base(true)
            {
                this.term = term;
                this.stats = stats;
            }

            public override string ToString()
            {
                return term.Utf8ToString();
            }
        }

        private sealed class PendingBlock : PendingEntry
        {
            public readonly BytesRef prefix;
            public readonly long fp;
            public FST<BytesRef> index;
            public IList<FST<BytesRef>> subIndices;
            public readonly bool hasTerms;
            public readonly bool isFloor;
            public readonly int floorLeadByte;
            private readonly IntsRef scratchIntsRef = new IntsRef();

            public PendingBlock(BytesRef prefix, long fp, bool hasTerms, bool isFloor, int floorLeadByte, IList<FST<BytesRef>> subIndices)
                : base(false)
            {
                this.prefix = prefix;
                this.fp = fp;
                this.hasTerms = hasTerms;
                this.isFloor = isFloor;
                this.floorLeadByte = floorLeadByte;
                this.subIndices = subIndices;
            }

            public override string ToString()
            {
                return "BLOCK: " + prefix.Utf8ToString();
            }

            public void CompileIndex(IList<PendingBlock> floorBlocks, RAMOutputStream scratchBytes)
            {

                //assert (isFloor && floorBlocks != null && floorBlocks.size() != 0) || (!isFloor && floorBlocks == null): "isFloor=" + isFloor + " floorBlocks=" + floorBlocks;

                //assert scratchBytes.getFilePointer() == 0;

                // TODO: try writing the leading vLong in MSB order
                // (opposite of what Lucene does today), for better
                // outputs sharing in the FST
                scratchBytes.WriteVLong(EncodeOutput(fp, hasTerms, isFloor));
                if (isFloor)
                {
                    scratchBytes.WriteVInt(floorBlocks.Count);
                    foreach (PendingBlock sub in floorBlocks)
                    {
                        //assert sub.floorLeadByte != -1;
                        //if (DEBUG) {
                        //  System.out.println("    write floorLeadByte=" + Integer.toHexString(sub.floorLeadByte&0xff));
                        //}
                        scratchBytes.WriteByte((byte)sub.floorLeadByte);
                        //assert sub.fp > fp;
                        scratchBytes.WriteVLong((sub.fp - fp) << 1 | (sub.hasTerms ? 1 : 0));
                    }
                }

                ByteSequenceOutputs outputs = ByteSequenceOutputs.GetSingleton();
                Builder<BytesRef> indexBuilder = new Builder<BytesRef>(FST.INPUT_TYPE.BYTE1,
                                                                             0, 0, true, false, int.MaxValue,
                                                                             outputs, null, false,
                                                                             PackedInts.COMPACT, true, 15);
                //if (DEBUG) {
                //  System.out.println("  compile index for prefix=" + prefix);
                //}
                //indexBuilder.DEBUG = false;
                byte[] bytes = new byte[(int)scratchBytes.FilePointer];
                //assert bytes.length > 0;
                scratchBytes.WriteTo(bytes, 0);
                indexBuilder.Add(Lucene.Net.Util.Fst.Util.ToIntsRef(prefix, scratchIntsRef), new BytesRef((sbyte[])(Array)bytes, 0, bytes.Length));
                scratchBytes.Reset();

                // Copy over index for all sub-blocks

                if (subIndices != null)
                {
                    foreach (FST<BytesRef> subIndex in subIndices)
                    {
                        Append(indexBuilder, subIndex);
                    }
                }

                if (floorBlocks != null)
                {
                    foreach (PendingBlock sub in floorBlocks)
                    {
                        if (sub.subIndices != null)
                        {
                            foreach (FST<BytesRef> subIndex in sub.subIndices)
                            {
                                Append(indexBuilder, subIndex);
                            }
                        }
                        sub.subIndices = null;
                    }
                }

                index = indexBuilder.Finish();
                subIndices = null;

                /*
                Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"));
                Util.toDot(index, w, false, false);
                System.out.println("SAVED to out.dot");
                w.close();
                */
            }

            private void Append(Builder<BytesRef> builder, FST<BytesRef> subIndex)
            {
                BytesRefFSTEnum<BytesRef> subIndexEnum = new BytesRefFSTEnum<BytesRef>(subIndex);
                BytesRefFSTEnum<BytesRef>.InputOutput<BytesRef> indexEnt;
                while ((indexEnt = subIndexEnum.Next()) != null)
                {
                    //if (DEBUG) {
                    //  System.out.println("      add sub=" + indexEnt.input + " " + indexEnt.input + " output=" + indexEnt.output);
                    //}
                    builder.Add(Util.ToIntsRef(indexEnt.Input, scratchIntsRef), indexEnt.Output);
                }
            }
        }

        internal readonly RAMOutputStream scratchBytes = new RAMOutputStream();

        internal class TermsWriter : TermsConsumer
        {
            private readonly FieldInfo fieldInfo;
            private long numTerms;
            internal long sumTotalTermFreq;
            internal long sumDocFreq;
            internal int docCount;
            internal long indexStartFP;

            private readonly NoOutputs noOutputs;
            private readonly Builder<Object> blockBuilder;

            private readonly IList<PendingEntry> pending = new List<PendingEntry>();

            private int lastBlockIndex = -1;

            private int[] subBytes = new int[10];
            private int[] subTermCounts = new int[10];
            private int[] subTermCountSums = new int[10];
            private int[] subSubCounts = new int[10];

            private readonly BlockTreeTermsWriter parent;

            private class FindBlocks : Builder<object>.FreezeTail<object>
            {
                private readonly TermsWriter parent;

                public FindBlocks(TermsWriter parent)
                {
                    this.parent = parent;
                }

                public override void Freeze(Builder<object>.UnCompiledNode<object>[] frontier, int prefixLenPlus1, IntsRef lastInput)
                {
                    //if (DEBUG) System.out.println("  freeze prefixLenPlus1=" + prefixLenPlus1);

                    for (int idx = lastInput.length; idx >= prefixLenPlus1; idx--)
                    {
                        Builder<object>.UnCompiledNode<Object> node = frontier[idx];

                        long totCount = 0;

                        if (node.IsFinal)
                        {
                            totCount++;
                        }

                        for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
                        {
                            Builder<object>.UnCompiledNode<Object> target = (Builder<object>.UnCompiledNode<Object>)node.Arcs[arcIdx].Target;
                            totCount += target.InputCount;
                            target.Clear();
                            node.Arcs[arcIdx].Target = null;
                        }
                        node.NumArcs = 0;

                        if (totCount >= parent.parent.minItemsInBlock || idx == 0)
                        {
                            // We are on a prefix node that has enough
                            // entries (terms or sub-blocks) under it to let
                            // us write a new block or multiple blocks (main
                            // block + follow on floor blocks):
                            //if (DEBUG) {
                            //  if (totCount < minItemsInBlock && idx != 0) {
                            //    System.out.println("  force block has terms");
                            //  }
                            //}
                            parent.WriteBlocks(lastInput, idx, (int)totCount);
                            node.InputCount = 1;
                        }
                        else
                        {
                            // stragglers!  carry count upwards
                            node.InputCount = totCount;
                        }
                        frontier[idx] = new Builder<object>.UnCompiledNode<Object>(parent.blockBuilder, idx);
                    }
                }
            }

            internal void WriteBlocks(IntsRef prevTerm, int prefixLength, int count)
            {
                if (prefixLength == 0 || count <= parent.maxItemsInBlock)
                {
                    // Easy case: not floor block.  Eg, prefix is "foo",
                    // and we found 30 terms/sub-blocks starting w/ that
                    // prefix, and minItemsInBlock <= 30 <=
                    // maxItemsInBlock.
                    PendingBlock nonFloorBlock = WriteBlock(prevTerm, prefixLength, prefixLength, count, count, 0, false, -1, true);
                    nonFloorBlock.CompileIndex(null, parent.scratchBytes);
                    pending.Add(nonFloorBlock);
                }
                else
                {
                    // Floor block case.  Eg, prefix is "foo" but we
                    // have 100 terms/sub-blocks starting w/ that
                    // prefix.  We segment the entries into a primary
                    // block and following floor blocks using the first
                    // label in the suffix to assign to floor blocks.

                    // TODO: we could store min & max suffix start byte
                    // in each block, to make floor blocks authoritative

                    //if (DEBUG) {
                    //  final BytesRef prefix = new BytesRef(prefixLength);
                    //  for(int m=0;m<prefixLength;m++) {
                    //    prefix.bytes[m] = (byte) prevTerm.ints[m];
                    //  }
                    //  prefix.length = prefixLength;
                    //  //System.out.println("\nWBS count=" + count + " prefix=" + prefix.utf8ToString() + " " + prefix);
                    //  System.out.println("writeBlocks: prefix=" + prefix + " " + prefix + " count=" + count + " pending.size()=" + pending.size());
                    //}
                    //System.out.println("\nwbs count=" + count);

                    int savLabel = prevTerm.ints[prevTerm.offset + prefixLength];

                    // Count up how many items fall under
                    // each unique label after the prefix.

                    // TODO: this is wasteful since the builder had
                    // already done this (partitioned these sub-terms
                    // according to their leading prefix byte)

                    IList<PendingEntry> slice = pending.SubList(pending.Count - count, pending.Count);
                    int lastSuffixLeadLabel = -1;
                    int termCount = 0;
                    int subCount = 0;
                    int numSubs = 0;

                    foreach (PendingEntry ent in slice)
                    {

                        // First byte in the suffix of this term
                        int suffixLeadLabel;
                        if (ent.isTerm)
                        {
                            PendingTerm term = (PendingTerm)ent;
                            if (term.term.length == prefixLength)
                            {
                                // Suffix is 0, ie prefix 'foo' and term is
                                // 'foo' so the term has empty string suffix
                                // in this block
                                //assert lastSuffixLeadLabel == -1;
                                //assert numSubs == 0;
                                suffixLeadLabel = -1;
                            }
                            else
                            {
                                suffixLeadLabel = term.term.bytes[term.term.offset + prefixLength] & 0xff;
                            }
                        }
                        else
                        {
                            PendingBlock block = (PendingBlock)ent;
                            //assert block.prefix.length > prefixLength;
                            suffixLeadLabel = block.prefix.bytes[block.prefix.offset + prefixLength] & 0xff;
                        }

                        if (suffixLeadLabel != lastSuffixLeadLabel && (termCount + subCount) != 0)
                        {
                            if (subBytes.Length == numSubs)
                            {
                                subBytes = ArrayUtil.Grow(subBytes);
                                subTermCounts = ArrayUtil.Grow(subTermCounts);
                                subSubCounts = ArrayUtil.Grow(subSubCounts);
                            }
                            subBytes[numSubs] = lastSuffixLeadLabel;
                            lastSuffixLeadLabel = suffixLeadLabel;
                            subTermCounts[numSubs] = termCount;
                            subSubCounts[numSubs] = subCount;
                            /*
                            if (suffixLeadLabel == -1) {
                              System.out.println("  sub " + -1 + " termCount=" + termCount + " subCount=" + subCount);
                            } else {
                              System.out.println("  sub " + Integer.toHexString(suffixLeadLabel) + " termCount=" + termCount + " subCount=" + subCount);
                            }
                            */
                            termCount = subCount = 0;
                            numSubs++;
                        }

                        if (ent.isTerm)
                        {
                            termCount++;
                        }
                        else
                        {
                            subCount++;
                        }
                    }

                    if (subBytes.Length == numSubs)
                    {
                        subBytes = ArrayUtil.Grow(subBytes);
                        subTermCounts = ArrayUtil.Grow(subTermCounts);
                        subSubCounts = ArrayUtil.Grow(subSubCounts);
                    }

                    subBytes[numSubs] = lastSuffixLeadLabel;
                    subTermCounts[numSubs] = termCount;
                    subSubCounts[numSubs] = subCount;
                    numSubs++;
                    /*
                    if (lastSuffixLeadLabel == -1) {
                      System.out.println("  sub " + -1 + " termCount=" + termCount + " subCount=" + subCount);
                    } else {
                      System.out.println("  sub " + Integer.toHexString(lastSuffixLeadLabel) + " termCount=" + termCount + " subCount=" + subCount);
                    }
                    */

                    if (subTermCountSums.Length < numSubs)
                    {
                        subTermCountSums = ArrayUtil.Grow(subTermCountSums, numSubs);
                    }

                    // Roll up (backwards) the termCounts; postings impl
                    // needs this to know where to pull the term slice
                    // from its pending terms stack:
                    int sum = 0;
                    for (int idx = numSubs - 1; idx >= 0; idx--)
                    {
                        sum += subTermCounts[idx];
                        subTermCountSums[idx] = sum;
                    }

                    // TODO: make a better segmenter?  It'd have to
                    // absorb the too-small end blocks backwards into
                    // the previous blocks

                    // Naive greedy segmentation; this is not always
                    // best (it can produce a too-small block as the
                    // last block):
                    int pendingCount = 0;
                    int startLabel = subBytes[0];
                    int curStart = count;
                    subCount = 0;

                    IList<PendingBlock> floorBlocks = new List<PendingBlock>();
                    PendingBlock firstBlock = null;

                    for (int sub = 0; sub < numSubs; sub++)
                    {
                        pendingCount += subTermCounts[sub] + subSubCounts[sub];
                        //System.out.println("  " + (subTermCounts[sub] + subSubCounts[sub]));
                        subCount++;

                        // Greedily make a floor block as soon as we've
                        // crossed the min count
                        if (pendingCount >= parent.minItemsInBlock)
                        {
                            int curPrefixLength;
                            if (startLabel == -1)
                            {
                                curPrefixLength = prefixLength;
                            }
                            else
                            {
                                curPrefixLength = 1 + prefixLength;
                                // floor term:
                                prevTerm.ints[prevTerm.offset + prefixLength] = startLabel;
                            }
                            //System.out.println("  " + subCount + " subs");
                            PendingBlock floorBlock = WriteBlock(prevTerm, prefixLength, curPrefixLength, curStart, pendingCount, subTermCountSums[1 + sub], true, startLabel, curStart == pendingCount);
                            if (firstBlock == null)
                            {
                                firstBlock = floorBlock;
                            }
                            else
                            {
                                floorBlocks.Add(floorBlock);
                            }
                            curStart -= pendingCount;
                            //System.out.println("    = " + pendingCount);
                            pendingCount = 0;

                            //assert minItemsInBlock == 1 || subCount > 1: "minItemsInBlock=" + minItemsInBlock + " subCount=" + subCount + " sub=" + sub + " of " + numSubs + " subTermCount=" + subTermCountSums[sub] + " subSubCount=" + subSubCounts[sub] + " depth=" + prefixLength;
                            subCount = 0;
                            startLabel = subBytes[sub + 1];

                            if (curStart == 0)
                            {
                                break;
                            }

                            if (curStart <= parent.maxItemsInBlock)
                            {
                                // remainder is small enough to fit into a
                                // block.  NOTE that this may be too small (<
                                // minItemsInBlock); need a true segmenter
                                // here
                                //assert startLabel != -1;
                                //assert firstBlock != null;
                                prevTerm.ints[prevTerm.offset + prefixLength] = startLabel;
                                //System.out.println("  final " + (numSubs-sub-1) + " subs");
                                /*
                                for(sub++;sub < numSubs;sub++) {
                                  System.out.println("  " + (subTermCounts[sub] + subSubCounts[sub]));
                                }
                                System.out.println("    = " + curStart);
                                if (curStart < minItemsInBlock) {
                                  System.out.println("      **");
                                }
                                */
                                floorBlocks.Add(WriteBlock(prevTerm, prefixLength, prefixLength + 1, curStart, curStart, 0, true, startLabel, true));
                                break;
                            }
                        }
                    }

                    prevTerm.ints[prevTerm.offset + prefixLength] = savLabel;

                    //assert firstBlock != null;
                    firstBlock.CompileIndex(floorBlocks, parent.scratchBytes);

                    pending.Add(firstBlock);
                    //if (DEBUG) System.out.println("  done pending.size()=" + pending.size());
                }
                lastBlockIndex = pending.Count - 1;
            }

            private String ToString(BytesRef b)
            {
                try
                {
                    return b.Utf8ToString() + " " + b;
                }
                catch
                {
                    // If BytesRef isn't actually UTF8, or it's eg a
                    // prefix of UTF8 that ends mid-unicode-char, we
                    // fallback to hex:
                    return b.ToString();
                }
            }

            private PendingBlock WriteBlock(IntsRef prevTerm, int prefixLength, int indexPrefixLength, int startBackwards, int length,
                                    int futureTermCount, bool isFloor, int floorLeadByte, bool isLastInFloor)
            {
                //assert length > 0;

                int start = pending.Count - startBackwards;

                //assert start >= 0: "pending.size()=" + pending.size() + " startBackwards=" + startBackwards + " length=" + length;

                IList<PendingEntry> slice = pending.SubList(start, start + length);

                long startFP = parent.output.FilePointer;

                BytesRef prefix = new BytesRef(indexPrefixLength);
                for (int m = 0; m < indexPrefixLength; m++)
                {
                    prefix.bytes[m] = (sbyte)prevTerm.ints[m];
                }
                prefix.length = indexPrefixLength;

                // Write block header:
                parent.output.WriteVInt((length << 1) | (isLastInFloor ? 1 : 0));

                // if (DEBUG) {
                //   System.out.println("  writeBlock " + (isFloor ? "(floor) " : "") + "seg=" + segment + " pending.size()=" + pending.size() + " prefixLength=" + prefixLength + " indexPrefix=" + toString(prefix) + " entCount=" + length + " startFP=" + startFP + " futureTermCount=" + futureTermCount + (isFloor ? (" floorLeadByte=" + Integer.toHexString(floorLeadByte&0xff)) : "") + " isLastInFloor=" + isLastInFloor);
                // }

                // 1st pass: pack term suffix bytes into byte[] blob
                // TODO: cutover to bulk int codec... simple64?

                bool isLeafBlock;
                if (lastBlockIndex < start)
                {
                    // This block definitely does not contain sub-blocks:
                    isLeafBlock = true;
                    //System.out.println("no scan true isFloor=" + isFloor);
                }
                else if (!isFloor)
                {
                    // This block definitely does contain at least one sub-block:
                    isLeafBlock = false;
                    //System.out.println("no scan false " + lastBlockIndex + " vs start=" + start + " len=" + length);
                }
                else
                {
                    // Must scan up-front to see if there is a sub-block
                    bool v = true;
                    //System.out.println("scan " + lastBlockIndex + " vs start=" + start + " len=" + length);
                    foreach (PendingEntry ent in slice)
                    {
                        if (!ent.isTerm)
                        {
                            v = false;
                            break;
                        }
                    }
                    isLeafBlock = v;
                }

                IList<FST<BytesRef>> subIndices;

                int termCount;
                if (isLeafBlock)
                {
                    subIndices = null;
                    foreach (PendingEntry ent in slice)
                    {
                        //assert ent.isTerm;
                        PendingTerm term = (PendingTerm)ent;
                        int suffix = term.term.length - prefixLength;
                        // if (DEBUG) {
                        //   BytesRef suffixBytes = new BytesRef(suffix);
                        //   System.arraycopy(term.term.bytes, prefixLength, suffixBytes.bytes, 0, suffix);
                        //   suffixBytes.length = suffix;
                        //   System.out.println("    write term suffix=" + suffixBytes);
                        // }
                        // For leaf block we write suffix straight
                        bytesWriter.WriteVInt(suffix);
                        bytesWriter.WriteBytes(term.term.bytes, prefixLength, suffix);

                        // Write term stats, to separate byte[] blob:
                        bytesWriter2.WriteVInt(term.stats.docFreq);
                        if (fieldInfo.IndexOptionsValue != FieldInfo.IndexOptions.DOCS_ONLY)
                        {
                            //assert term.stats.totalTermFreq >= term.stats.docFreq: term.stats.totalTermFreq + " vs " + term.stats.docFreq;
                            bytesWriter2.WriteVLong(term.stats.totalTermFreq - term.stats.docFreq);
                        }
                    }
                    termCount = length;
                }
                else
                {
                    subIndices = new List<FST<BytesRef>>();
                    termCount = 0;
                    foreach (PendingEntry ent in slice)
                    {
                        if (ent.isTerm)
                        {
                            PendingTerm term = (PendingTerm)ent;
                            int suffix = term.term.length - prefixLength;
                            // if (DEBUG) {
                            //   BytesRef suffixBytes = new BytesRef(suffix);
                            //   System.arraycopy(term.term.bytes, prefixLength, suffixBytes.bytes, 0, suffix);
                            //   suffixBytes.length = suffix;
                            //   System.out.println("    write term suffix=" + suffixBytes);
                            // }
                            // For non-leaf block we borrow 1 bit to record
                            // if entry is term or sub-block
                            bytesWriter.WriteVInt(suffix << 1);
                            bytesWriter.WriteBytes(term.term.bytes, prefixLength, suffix);

                            // Write term stats, to separate byte[] blob:
                            bytesWriter2.WriteVInt(term.stats.docFreq);
                            if (fieldInfo.IndexOptionsValue != FieldInfo.IndexOptions.DOCS_ONLY)
                            {
                                //assert term.stats.totalTermFreq >= term.stats.docFreq;
                                bytesWriter2.WriteVLong(term.stats.totalTermFreq - term.stats.docFreq);
                            }

                            termCount++;
                        }
                        else
                        {
                            PendingBlock block = (PendingBlock)ent;
                            int suffix = block.prefix.length - prefixLength;

                            //assert suffix > 0;

                            // For non-leaf block we borrow 1 bit to record
                            // if entry is term or sub-block
                            bytesWriter.WriteVInt((suffix << 1) | 1);
                            bytesWriter.WriteBytes(block.prefix.bytes, prefixLength, suffix);
                            //assert block.fp < startFP;

                            // if (DEBUG) {
                            //   BytesRef suffixBytes = new BytesRef(suffix);
                            //   System.arraycopy(block.prefix.bytes, prefixLength, suffixBytes.bytes, 0, suffix);
                            //   suffixBytes.length = suffix;
                            //   System.out.println("    write sub-block suffix=" + toString(suffixBytes) + " subFP=" + block.fp + " subCode=" + (startFP-block.fp) + " floor=" + block.isFloor);
                            // }

                            bytesWriter.WriteVLong(startFP - block.fp);
                            subIndices.Add(block.index);
                        }
                    }

                    //assert subIndices.size() != 0;
                }

                // TODO: we could block-write the term suffix pointers;
                // this would take more space but would enable binary
                // search on lookup

                // Write suffixes byte[] blob to terms dict output:
                parent.output.WriteVInt((int)(bytesWriter.FilePointer << 1) | (isLeafBlock ? 1 : 0));
                bytesWriter.WriteTo(parent.output);
                bytesWriter.Reset();

                // Write term stats byte[] blob
                parent.output.WriteVInt((int)bytesWriter2.FilePointer);
                bytesWriter2.WriteTo(parent.output);
                bytesWriter2.Reset();

                // Have postings writer write block
                parent.postingsWriter.FlushTermsBlock(futureTermCount + termCount, termCount);

                // Remove slice replaced by block:
                slice.Clear();

                if (lastBlockIndex >= start)
                {
                    if (lastBlockIndex < start + length)
                    {
                        lastBlockIndex = start;
                    }
                    else
                    {
                        lastBlockIndex -= length;
                    }
                }

                // if (DEBUG) {
                //   System.out.println("      fpEnd=" + out.getFilePointer());
                // }

                return new PendingBlock(prefix, startFP, termCount != 0, isFloor, floorLeadByte, subIndices);
            }

            internal TermsWriter(BlockTreeTermsWriter parent, FieldInfo fieldInfo)
            {
                this.parent = parent;
                this.fieldInfo = fieldInfo;

                noOutputs = NoOutputs.GetSingleton();

                // This Builder is just used transiently to fragment
                // terms into "good" blocks; we don't save the
                // resulting FST:
                blockBuilder = new Builder<Object>(FST.INPUT_TYPE.BYTE1,
                                                   0, 0, true,
                                                   true, int.MaxValue,
                                                   noOutputs,
                                                   new FindBlocks(this), false,
                                                   PackedInts.COMPACT,
                                                   true, 15);

                parent.postingsWriter.SetField(fieldInfo);
            }

            public override IComparer<BytesRef> Comparator
            {
                get { return BytesRef.UTF8SortedAsUnicodeComparer; }
            }

            public override PostingsConsumer StartTerm(BytesRef text)
            {
                //if (DEBUG) System.out.println("\nBTTW.startTerm term=" + fieldInfo.name + ":" + toString(text) + " seg=" + segment);
                parent.postingsWriter.StartTerm();
                /*
                if (fieldInfo.name.equals("id")) {
                  postingsWriter.termID = Integer.parseInt(text.utf8ToString());
                } else {
                  postingsWriter.termID = -1;
                }
                */
                return parent.postingsWriter;
            }

            private readonly IntsRef scratchIntsRef = new IntsRef();

            public override void FinishTerm(BytesRef text, TermStats stats)
            {
                //assert stats.docFreq > 0;
                //if (DEBUG) System.out.println("BTTW.finishTerm term=" + fieldInfo.name + ":" + toString(text) + " seg=" + segment + " df=" + stats.docFreq);

                blockBuilder.Add(Util.ToIntsRef(text, scratchIntsRef), noOutputs.GetNoOutput());
                pending.Add(new PendingTerm(BytesRef.DeepCopyOf(text), stats));
                parent.postingsWriter.FinishTerm(stats);
                numTerms++;
            }

            public override void Finish(long sumTotalTermFreq, long sumDocFreq, int docCount)
            {
                if (numTerms > 0)
                {
                    blockBuilder.Finish();

                    // We better have one final "root" block:
                    //assert pending.size() == 1 && !pending.get(0).isTerm: "pending.size()=" + pending.size() + " pending=" + pending;
                    PendingBlock root = (PendingBlock)pending[0];
                    //assert root.prefix.length == 0;
                    //assert root.index.getEmptyOutput() != null;

                    this.sumTotalTermFreq = sumTotalTermFreq;
                    this.sumDocFreq = sumDocFreq;
                    this.docCount = docCount;

                    // Write FST to index
                    indexStartFP = parent.indexOut.FilePointer;
                    root.index.Save(parent.indexOut);
                    //System.out.println("  write FST " + indexStartFP + " field=" + fieldInfo.name);

                    // if (SAVE_DOT_FILES || DEBUG) {
                    //   final String dotFileName = segment + "_" + fieldInfo.name + ".dot";
                    //   Writer w = new OutputStreamWriter(new FileOutputStream(dotFileName));
                    //   Util.toDot(root.index, w, false, false);
                    //   System.out.println("SAVED to " + dotFileName);
                    //   w.close();
                    // }

                    parent.fields.Add(new FieldMetaData(fieldInfo,
                                                 ((PendingBlock)pending[0]).index.EmptyOutput,
                                                 numTerms,
                                                 indexStartFP,
                                                 sumTotalTermFreq,
                                                 sumDocFreq,
                                                 docCount));
                }
                else
                {
                    //assert sumTotalTermFreq == 0 || fieldInfo.getIndexOptions() == IndexOptions.DOCS_ONLY && sumTotalTermFreq == -1;
                    //assert sumDocFreq == 0;
                    //assert docCount == 0;
                }
            }

            private readonly RAMOutputStream bytesWriter = new RAMOutputStream();
            private readonly RAMOutputStream bytesWriter2 = new RAMOutputStream();
        }

        protected override void Dispose(bool disposing)
        {
            System.IO.IOException ioe = null;
            try
            {

                long dirStart = output.FilePointer;
                long indexDirStart = indexOut.FilePointer;

                output.WriteVInt(fields.Count);

                foreach (FieldMetaData field in fields)
                {
                    //System.out.println("  field " + field.fieldInfo.name + " " + field.numTerms + " terms");
                    output.WriteVInt(field.fieldInfo.number);
                    output.WriteVLong(field.numTerms);
                    output.WriteVInt(field.rootCode.length);
                    output.WriteBytes(field.rootCode.bytes, field.rootCode.offset, field.rootCode.length);
                    if (field.fieldInfo.IndexOptionsValue != FieldInfo.IndexOptions.DOCS_ONLY)
                    {
                        output.WriteVLong(field.sumTotalTermFreq);
                    }
                    output.WriteVLong(field.sumDocFreq);
                    output.WriteVInt(field.docCount);
                    indexOut.WriteVLong(field.indexStartFP);
                }
                WriteTrailer(output, dirStart);
                WriteIndexTrailer(indexOut, indexDirStart);
            }
            catch (System.IO.IOException ioe2)
            {
                ioe = ioe2;
            }
            finally
            {
                IOUtils.CloseWhileHandlingException(ioe, output, indexOut, postingsWriter);
            }
        }
    }
}
