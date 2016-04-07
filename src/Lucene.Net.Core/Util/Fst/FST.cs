using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Util.Fst
{
    using Lucene.Net.Util;
    using System.IO;
    using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;

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

    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using GrowableWriter = Lucene.Net.Util.Packed.GrowableWriter;
    using InputStreamDataInput = Lucene.Net.Store.InputStreamDataInput;
    using OutputStreamDataOutput = Lucene.Net.Store.OutputStreamDataOutput;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;

    // TODO: break this into WritableFST and ReadOnlyFST.. then
    // we can have subclasses of ReadOnlyFST to handle the
    // different byte[] level encodings (packed or
    // not)... and things like nodeCount, arcCount are read only

    // TODO: if FST is pure prefix trie we can do a more compact
    // job, ie, once we are at a 'suffix only', just store the
    // completion labels as a string not as a series of arcs.

    // NOTE: while the FST is able to represent a non-final
    // dead-end state (NON_FINAL_END_NODE=0), the layers above
    // (FSTEnum, Util) have problems with this!!

    /// <summary>
    /// Represents an finite state machine (FST), using a
    ///  compact byte[] format.
    ///  <p> The format is similar to what's used by Morfologik
    ///  (http://sourceforge.net/projects/morfologik).
    ///
    ///  <p> See the {@link Lucene.Net.Util.Fst package
    ///      documentation} for some simple examples.
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class FST<T> : FST
    {
        /*/// <summary>
        /// Specifies allowed range of each int input label for
        ///  this FST.
        /// </summary>
        public enum INPUT_TYPE
        {
            BYTE1,
            BYTE2,
            BYTE4
        }*/
        public readonly INPUT_TYPE inputType;

        /*internal static readonly int BIT_FINAL_ARC = 1 << 0;
        internal static readonly int BIT_LAST_ARC = 1 << 1;
        internal static readonly int BIT_TARGET_NEXT = 1 << 2;

        // TODO: we can free up a bit if we can nuke this:
        internal static readonly int BIT_STOP_NODE = 1 << 3;
        internal static readonly int BIT_ARC_HAS_OUTPUT = 1 << 4;
        internal static readonly int BIT_ARC_HAS_FINAL_OUTPUT = 1 << 5;

        // Arcs are stored as fixed-size (per entry) array, so
        // that we can find an arc using binary search.  We do
        // this when number of arcs is > NUM_ARCS_ARRAY:

        // If set, the target node is delta coded vs current
        // position:
        private static readonly int BIT_TARGET_DELTA = 1 << 6;

        // We use this as a marker (because this one flag is
        // illegal by itself ...):
        private static readonly sbyte ARCS_AS_FIXED_ARRAY = (sbyte)BIT_ARC_HAS_FINAL_OUTPUT;

        /// <seealso cref= #shouldExpand(UnCompiledNode) </seealso>
        internal const int FIXED_ARRAY_SHALLOW_DISTANCE = 3; // 0 => only root node.

        /// <seealso cref= #shouldExpand(UnCompiledNode) </seealso>
        internal const int FIXED_ARRAY_NUM_ARCS_SHALLOW = 5;

        /// <seealso cref= #shouldExpand(UnCompiledNode) </seealso>
        internal const int FIXED_ARRAY_NUM_ARCS_DEEP = 10;*/

        private int[] BytesPerArc = new int[0];

        /*// Increment version to change it
        private const string FILE_FORMAT_NAME = "FST";
        private const int VERSION_START = 0;

        /// <summary>
        /// Changed numBytesPerArc for array'd case from byte to int. </summary>
        private const int VERSION_INT_NUM_BYTES_PER_ARC = 1;

        /// <summary>
        /// Write BYTE2 labels as 2-byte short, not vInt. </summary>
        private const int VERSION_SHORT_BYTE2_LABELS = 2;

        /// <summary>
        /// Added optional packed format. </summary>
        private const int VERSION_PACKED = 3;

        /// <summary>
        /// Changed from int to vInt for encoding arc targets.
        ///  Also changed maxBytesPerArc from int to vInt in the array case.
        /// </summary>
        private const int VERSION_VINT_TARGET = 4;

        private const int VERSION_CURRENT = VERSION_VINT_TARGET;

        // Never serialized; just used to represent the virtual
        // final node w/ no arcs:
        private const long FINAL_END_NODE = -1;

        // Never serialized; just used to represent the virtual
        // non-final node w/ no arcs:
        private const long NON_FINAL_END_NODE = 0;*/

        // if non-null, this FST accepts the empty string and
        // produces this output
        internal T emptyOutput;

        internal readonly BytesStore Bytes;

        private long StartNode = -1;

        public readonly Outputs<T> Outputs;

        // Used for the BIT_TARGET_NEXT optimization (whereby
        // instead of storing the address of the target node for
        // a given arc, we mark a single bit noting that the next
        // node in the byte[] is the target node):
        private long LastFrozenNode;

        private readonly T NO_OUTPUT;

        public long nodeCount;
        public long arcCount;
        public long arcWithOutputCount;

        private readonly bool Packed;
        private PackedInts.Reader NodeRefToAddress;

        /// <summary>
        /// If arc has this label then that arc is final/accepted </summary>
        public static readonly int END_LABEL = -1;

        private readonly bool AllowArrayArcs;

        private Arc<T>[] CachedRootArcs;
        private Arc<T>[] AssertingCachedRootArcs; // only set wit assert

        internal static bool Flag(int flags, int bit)
        {
            return (flags & bit) != 0;
        }

        private GrowableWriter NodeAddress;

        // TODO: we could be smarter here, and prune periodically
        // as we go; high in-count nodes will "usually" become
        // clear early on:
        private GrowableWriter InCounts;

        private readonly int Version;

        // make a new empty FST, for building; Builder invokes
        // this ctor
        internal FST(INPUT_TYPE inputType, Outputs<T> outputs, bool willPackFST, float acceptableOverheadRatio, bool allowArrayArcs, int bytesPageBits)
        {
            this.inputType = inputType;
            this.Outputs = outputs;
            this.AllowArrayArcs = allowArrayArcs;
            Version = VERSION_CURRENT;
            Bytes = new BytesStore(bytesPageBits);
            // pad: ensure no node gets address 0 which is reserved to mean
            // the stop state w/ no arcs
            Bytes.WriteByte(0);
            NO_OUTPUT = outputs.NoOutput;
            if (willPackFST)
            {
                NodeAddress = new GrowableWriter(15, 8, acceptableOverheadRatio);
                InCounts = new GrowableWriter(1, 8, acceptableOverheadRatio);
            }
            else
            {
                NodeAddress = null;
                InCounts = null;
            }

            emptyOutput = default(T);
            Packed = false;
            NodeRefToAddress = null;
        }

        public static readonly int DEFAULT_MAX_BLOCK_BITS = Constants.JRE_IS_64BIT ? 30 : 28;

        /// <summary>
        /// Load a previously saved FST. </summary>
        public FST(DataInput @in, Outputs<T> outputs)
            : this(@in, outputs, DEFAULT_MAX_BLOCK_BITS)
        {
        }

        /// <summary>
        /// Load a previously saved FST; maxBlockBits allows you to
        ///  control the size of the byte[] pages used to hold the FST bytes.
        /// </summary>
        public FST(DataInput @in, Outputs<T> outputs, int maxBlockBits)
        {
            this.Outputs = outputs;

            if (maxBlockBits < 1 || maxBlockBits > 30)
            {
                throw new System.ArgumentException("maxBlockBits should be 1 .. 30; got " + maxBlockBits);
            }

            // NOTE: only reads most recent format; we don't have
            // back-compat promise for FSTs (they are experimental):
            Version = CodecUtil.CheckHeader(@in, FILE_FORMAT_NAME, VERSION_PACKED, VERSION_VINT_TARGET);
            Packed = @in.ReadByte() == 1;
            if (@in.ReadByte() == 1)
            {
                // accepts empty string
                // 1 KB blocks:
                BytesStore emptyBytes = new BytesStore(10);
                int numBytes = @in.ReadVInt();
                emptyBytes.CopyBytes(@in, numBytes);

                // De-serialize empty-string output:
                BytesReader reader;
                if (Packed)
                {
                    reader = emptyBytes.ForwardReader;
                }
                else
                {
                    reader = emptyBytes.ReverseReader;
                    // NoOutputs uses 0 bytes when writing its output,
                    // so we have to check here else BytesStore gets
                    // angry:
                    if (numBytes > 0)
                    {
                        reader.Position = numBytes - 1;
                    }
                }
                emptyOutput = outputs.ReadFinalOutput(reader);
            }
            else
            {
                emptyOutput = default(T);
            }
            var t = @in.ReadByte();
            switch (t)
            {
                case 0:
                    inputType = INPUT_TYPE.BYTE1;
                    break;

                case 1:
                    inputType = INPUT_TYPE.BYTE2;
                    break;

                case 2:
                    inputType = INPUT_TYPE.BYTE4;
                    break;

                default:
                    throw new InvalidOperationException("invalid input type " + t);
            }
            if (Packed)
            {
                NodeRefToAddress = PackedInts.GetReader(@in);
            }
            else
            {
                NodeRefToAddress = null;
            }
            StartNode = @in.ReadVLong();
            nodeCount = @in.ReadVLong();
            arcCount = @in.ReadVLong();
            arcWithOutputCount = @in.ReadVLong();

            long numBytes_ = @in.ReadVLong();
            Bytes = new BytesStore(@in, numBytes_, 1 << maxBlockBits);

            NO_OUTPUT = outputs.NoOutput;

            CacheRootArcs();

            // NOTE: bogus because this is only used during
            // building; we need to break out mutable FST from
            // immutable
            AllowArrayArcs = false;

            /*
            if (bytes.length == 665) {
              Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"), StandardCharsets.UTF_8);
              Util.toDot(this, w, false, false);
              w.Dispose();
              System.out.println("Wrote FST to out.dot");
            }
            */
        }

        public INPUT_TYPE InputType
        {
            get
            {
                return inputType;
            }
        }

        /// <summary>
        /// Returns bytes used to represent the FST </summary>
        public long SizeInBytes()
        {
            long size = Bytes.Position;
            if (Packed)
            {
                size += NodeRefToAddress.RamBytesUsed();
            }
            else if (NodeAddress != null)
            {
                size += NodeAddress.RamBytesUsed();
                size += InCounts.RamBytesUsed();
            }
            return size;
        }

        public void Finish(long newStartNode)
        {
            if (StartNode != -1)
            {
                throw new InvalidOperationException("already finished");
            }
            if (newStartNode == FINAL_END_NODE && emptyOutput != null)
            {
                newStartNode = 0;
            }
            StartNode = newStartNode;
            Bytes.Finish();

            CacheRootArcs();
        }

        private long GetNodeAddress(long node)
        {
            if (NodeAddress != null)
            {
                // Deref
                return NodeAddress.Get((int)node);
            }
            else
            {
                // Straight
                return node;
            }
        }

        // Caches first 128 labels
        private void CacheRootArcs()
        {
            CachedRootArcs = (Arc<T>[])new Arc<T>[0x80];
            ReadRootArcs(CachedRootArcs);

            Debug.Assert(SetAssertingRootArcs(CachedRootArcs));
            Debug.Assert(AssertRootArcs());
        }

        public void ReadRootArcs(Arc<T>[] arcs)
        {
            Arc<T> arc = new Arc<T>();
            GetFirstArc(arc);
            BytesReader @in = BytesReader;
            if (TargetHasArcs(arc))
            {
                ReadFirstRealTargetArc(arc.Target, arc, @in);
                while (true)
                {
                    Debug.Assert(arc.Label != END_LABEL);
                    if (arc.Label < CachedRootArcs.Length)
                    {
                        arcs[arc.Label] = (new Arc<T>()).CopyFrom(arc);
                    }
                    else
                    {
                        break;
                    }
                    if (arc.Last)
                    {
                        break;
                    }
                    ReadNextRealArc(arc, @in);
                }
            }
        }

        private bool SetAssertingRootArcs(Arc<T>[] arcs)
        {
            AssertingCachedRootArcs = (Arc<T>[])new Arc<T>[arcs.Length];
            ReadRootArcs(AssertingCachedRootArcs);
            return true;
        }

        private bool AssertRootArcs()
        {
            Debug.Assert(CachedRootArcs != null);
            Debug.Assert(AssertingCachedRootArcs != null);
            for (int i = 0; i < CachedRootArcs.Length; i++)
            {
                Arc<T> root = CachedRootArcs[i];
                Arc<T> asserting = AssertingCachedRootArcs[i];
                if (root != null)
                {
                    Debug.Assert(root.ArcIdx == asserting.ArcIdx);
                    Debug.Assert(root.BytesPerArc == asserting.BytesPerArc);
                    Debug.Assert(root.Flags == asserting.Flags);
                    Debug.Assert(root.Label == asserting.Label);
                    Debug.Assert(root.NextArc == asserting.NextArc);
                    Debug.Assert(root.NextFinalOutput.Equals(asserting.NextFinalOutput));
                    Debug.Assert(root.Node == asserting.Node);
                    Debug.Assert(root.NumArcs == asserting.NumArcs);
                    Debug.Assert(root.Output.Equals(asserting.Output));
                    Debug.Assert(root.PosArcsStart == asserting.PosArcsStart);
                    Debug.Assert(root.Target == asserting.Target);
                }
                else
                {
                    Debug.Assert(root == null && asserting == null);
                }
            }
            return true;
        }

        public T EmptyOutput
        {
            get
            {
                return emptyOutput;
            }
            set
            {
                if (emptyOutput != null)
                {
                    emptyOutput = Outputs.Merge(emptyOutput, value);
                }
                else
                {
                    emptyOutput = value;
                }
            }
        }

        public void Save(DataOutput @out)
        {
            if (StartNode == -1)
            {
                throw new InvalidOperationException("call finish first");
            }
            if (NodeAddress != null)
            {
                throw new InvalidOperationException("cannot save an FST pre-packed FST; it must first be packed");
            }
            if (Packed && !(NodeRefToAddress is PackedInts.Mutable))
            {
                throw new InvalidOperationException("cannot save a FST which has been loaded from disk ");
            }
            CodecUtil.WriteHeader(@out, FILE_FORMAT_NAME, VERSION_CURRENT);
            if (Packed)
            {
                @out.WriteByte(1);
            }
            else
            {
                @out.WriteByte(0);
            }
            // TODO: really we should encode this as an arc, arriving
            // to the root node, instead of special casing here:
            if (emptyOutput != null)
            {
                // Accepts empty string
                @out.WriteByte(1);

                // Serialize empty-string output:
                var ros = new RAMOutputStream();
                Outputs.WriteFinalOutput(emptyOutput, ros);

                var emptyOutputBytes = new byte[(int)ros.FilePointer];
                ros.WriteTo(emptyOutputBytes, 0);

                if (!Packed)
                {
                    // reverse
                    int stopAt = emptyOutputBytes.Length / 2;
                    int upto = 0;
                    while (upto < stopAt)
                    {
                        var b = emptyOutputBytes[upto];
                        emptyOutputBytes[upto] = emptyOutputBytes[emptyOutputBytes.Length - upto - 1];
                        emptyOutputBytes[emptyOutputBytes.Length - upto - 1] = b;
                        upto++;
                    }
                }
                @out.WriteVInt(emptyOutputBytes.Length);
                @out.WriteBytes(emptyOutputBytes, 0, emptyOutputBytes.Length);
            }
            else
            {
                @out.WriteByte(0);
            }
            sbyte t;
            if (inputType == INPUT_TYPE.BYTE1)
            {
                t = 0;
            }
            else if (inputType == INPUT_TYPE.BYTE2)
            {
                t = 1;
            }
            else
            {
                t = 2;
            }
            @out.WriteByte((byte)t);
            if (Packed)
            {
                ((PackedInts.Mutable)NodeRefToAddress).Save(@out);
            }
            @out.WriteVLong(StartNode);
            @out.WriteVLong(nodeCount);
            @out.WriteVLong(arcCount);
            @out.WriteVLong(arcWithOutputCount);
            long numBytes = Bytes.Position;
            @out.WriteVLong(numBytes);
            Bytes.WriteTo(@out);
        }

        /// <summary>
        /// Writes an automaton to a file.
        /// </summary>
        public void Save(FileInfo file)
        {
            //TODO: conniey
            //bool success = false;
            //var bs = new BufferedStream(file.OpenWrite());
            //try
            //{
            //    Save(new OutputStreamDataOutput(bs));
            //    success = true;
            //}
            //finally
            //{
            //    if (success)
            //    {
            //        IOUtils.Close(bs);
            //    }
            //    else
            //    {
            //        IOUtils.CloseWhileHandlingException(bs);
            //    }
            //}
        }

        /// <summary>
        /// Reads an automaton from a file.
        /// </summary>
        public static FST<T> Read<T>(FileInfo file, Outputs<T> outputs)
        {
            //TODO: conniey
            throw new NotImplementedException("");
            //var bs = new BufferedStream(file.OpenRead());
            //bool success = false;
            //try
            //{
            //    FST<T> fst = new FST<T>(new InputStreamDataInput(bs), outputs);
            //    success = true;
            //    return fst;
            //}
            //finally
            //{
            //    if (success)
            //    {
            //        IOUtils.Close(bs);
            //    }
            //    else
            //    {
            //        IOUtils.CloseWhileHandlingException(bs);
            //    }
            //}
        }

        private void WriteLabel(DataOutput @out, int v)
        {
            Debug.Assert(v >= 0, "v=" + v);
            if (inputType == INPUT_TYPE.BYTE1)
            {
                Debug.Assert(v <= 255, "v=" + v);
                @out.WriteByte((byte)(sbyte)v);
            }
            else if (inputType == INPUT_TYPE.BYTE2)
            {
                Debug.Assert(v <= 65535, "v=" + v);
                @out.WriteShort((short)v);
            }
            else
            {
                @out.WriteVInt(v);
            }
        }

        internal int ReadLabel(DataInput @in)
        {
            int v;
            if (inputType == INPUT_TYPE.BYTE1)
            {
                // Unsigned byte:
                v = @in.ReadByte() & 0xFF;
            }
            else if (inputType == INPUT_TYPE.BYTE2)
            {
                // Unsigned short:
                v = @in.ReadShort() & 0xFFFF;
            }
            else
            {
                v = @in.ReadVInt();
            }
            return v;
        }

        /// <summary>
        /// returns true if the node at this address has any
        ///  outgoing arcs
        /// </summary>
        public static bool TargetHasArcs(Arc<T> arc)
        {
            return arc.Target > 0;
        }

        // serializes new node by appending its bytes to the end
        // of the current byte[]
        public long AddNode(Builder<T>.UnCompiledNode<T> nodeIn)
        {
            //System.out.println("FST.addNode pos=" + bytes.getPosition() + " numArcs=" + nodeIn.numArcs);
            if (nodeIn.NumArcs == 0)
            {
                if (nodeIn.IsFinal)
                {
                    return FINAL_END_NODE;
                }
                else
                {
                    return NON_FINAL_END_NODE;
                }
            }

            long startAddress = Bytes.Position;
            //System.out.println("  startAddr=" + startAddress);

            bool doFixedArray = ShouldExpand(nodeIn);
            if (doFixedArray)
            {
                //System.out.println("  fixedArray");
                if (BytesPerArc.Length < nodeIn.NumArcs)
                {
                    BytesPerArc = new int[ArrayUtil.Oversize(nodeIn.NumArcs, 1)];
                }
            }

            arcCount += nodeIn.NumArcs;

            int lastArc = nodeIn.NumArcs - 1;

            long lastArcStart = Bytes.Position;
            int maxBytesPerArc = 0;
            for (int arcIdx = 0; arcIdx < nodeIn.NumArcs; arcIdx++)
            {
                Builder<T>.Arc<T> arc = nodeIn.Arcs[arcIdx];
                var target = (Builder<T>.CompiledNode)arc.Target;
                int flags = 0;
                //System.out.println("  arc " + arcIdx + " label=" + arc.Label + " -> target=" + target.Node);

                if (arcIdx == lastArc)
                {
                    flags += BIT_LAST_ARC;
                }

                if (LastFrozenNode == target.Node && !doFixedArray)
                {
                    // TODO: for better perf (but more RAM used) we
                    // could avoid this except when arc is "near" the
                    // last arc:
                    flags += BIT_TARGET_NEXT;
                }

                if (arc.IsFinal)
                {
                    flags += BIT_FINAL_ARC;
                    if (!arc.NextFinalOutput.Equals(NO_OUTPUT))
                    {
                        flags += BIT_ARC_HAS_FINAL_OUTPUT;
                    }
                }
                else
                {
                    Debug.Assert(arc.NextFinalOutput.Equals(NO_OUTPUT));
                }

                bool targetHasArcs = target.Node > 0;

                if (!targetHasArcs)
                {
                    flags += BIT_STOP_NODE;
                }
                else if (InCounts != null)
                {
                    InCounts.Set((int)target.Node, InCounts.Get((int)target.Node) + 1);
                }

                if (!arc.Output.Equals(NO_OUTPUT))
                {
                    flags += BIT_ARC_HAS_OUTPUT;
                }

                Bytes.WriteByte((byte)(sbyte)flags);
                WriteLabel(Bytes, arc.Label);

                // System.out.println("  write arc: label=" + (char) arc.Label + " flags=" + flags + " target=" + target.Node + " pos=" + bytes.getPosition() + " output=" + outputs.outputToString(arc.Output));

                if (!arc.Output.Equals(NO_OUTPUT))
                {
                    Outputs.Write(arc.Output, Bytes);
                    //System.out.println("    write output");
                    arcWithOutputCount++;
                }

                if (!arc.NextFinalOutput.Equals(NO_OUTPUT))
                {
                    //System.out.println("    write final output");
                    Outputs.WriteFinalOutput(arc.NextFinalOutput, Bytes);
                }

                if (targetHasArcs && (flags & BIT_TARGET_NEXT) == 0)
                {
                    Debug.Assert(target.Node > 0);
                    //System.out.println("    write target");
                    Bytes.WriteVLong(target.Node);
                }

                // just write the arcs "like normal" on first pass,
                // but record how many bytes each one took, and max
                // byte size:
                if (doFixedArray)
                {
                    BytesPerArc[arcIdx] = (int)(Bytes.Position - lastArcStart);
                    lastArcStart = Bytes.Position;
                    maxBytesPerArc = Math.Max(maxBytesPerArc, BytesPerArc[arcIdx]);
                    //System.out.println("    bytes=" + bytesPerArc[arcIdx]);
                }
            }

            // TODO: try to avoid wasteful cases: disable doFixedArray in that case
            /*
             *
             * LUCENE-4682: what is a fair heuristic here?
             * It could involve some of these:
             * 1. how "busy" the node is: nodeIn.inputCount relative to frontier[0].inputCount?
             * 2. how much binSearch saves over scan: nodeIn.numArcs
             * 3. waste: numBytes vs numBytesExpanded
             *
             * the one below just looks at #3
            if (doFixedArray) {
              // rough heuristic: make this 1.25 "waste factor" a parameter to the phd ctor????
              int numBytes = lastArcStart - startAddress;
              int numBytesExpanded = maxBytesPerArc * nodeIn.numArcs;
              if (numBytesExpanded > numBytes*1.25) {
                doFixedArray = false;
              }
            }
            */

            if (doFixedArray)
            {
                const int MAX_HEADER_SIZE = 11; // header(byte) + numArcs(vint) + numBytes(vint)
                Debug.Assert(maxBytesPerArc > 0);
                // 2nd pass just "expands" all arcs to take up a fixed
                // byte size

                //System.out.println("write int @pos=" + (fixedArrayStart-4) + " numArcs=" + nodeIn.numArcs);
                // create the header
                // TODO: clean this up: or just rewind+reuse and deal with it
                byte[] header = new byte[MAX_HEADER_SIZE];
                var bad = new ByteArrayDataOutput(header);
                // write a "false" first arc:
                bad.WriteByte((byte)ARCS_AS_FIXED_ARRAY);
                bad.WriteVInt(nodeIn.NumArcs);
                bad.WriteVInt(maxBytesPerArc);
                int headerLen = bad.Position;

                long fixedArrayStart = startAddress + headerLen;

                // expand the arcs in place, backwards
                long srcPos = Bytes.Position;
                long destPos = fixedArrayStart + nodeIn.NumArcs * maxBytesPerArc;
                Debug.Assert(destPos >= srcPos);
                if (destPos > srcPos)
                {
                    Bytes.SkipBytes((int)(destPos - srcPos));
                    for (int arcIdx = nodeIn.NumArcs - 1; arcIdx >= 0; arcIdx--)
                    {
                        destPos -= maxBytesPerArc;
                        srcPos -= BytesPerArc[arcIdx];
                        //System.out.println("  repack arcIdx=" + arcIdx + " srcPos=" + srcPos + " destPos=" + destPos);
                        if (srcPos != destPos)
                        {
                            //System.out.println("  copy len=" + bytesPerArc[arcIdx]);
                            Debug.Assert(destPos > srcPos, "destPos=" + destPos + " srcPos=" + srcPos + " arcIdx=" + arcIdx + " maxBytesPerArc=" + maxBytesPerArc + " bytesPerArc[arcIdx]=" + BytesPerArc[arcIdx] + " nodeIn.numArcs=" + nodeIn.NumArcs);
                            Bytes.CopyBytes(srcPos, destPos, BytesPerArc[arcIdx]);
                        }
                    }
                }

                // now write the header
                Bytes.WriteBytes(startAddress, header, 0, headerLen);
            }

            long thisNodeAddress = Bytes.Position - 1;

            Bytes.Reverse(startAddress, thisNodeAddress);

            // PackedInts uses int as the index, so we cannot handle
            // > 2.1B nodes when packing:
            if (NodeAddress != null && nodeCount == int.MaxValue)
            {
                throw new InvalidOperationException("cannot create a packed FST with more than 2.1 billion nodes");
            }

            nodeCount++;
            long node;
            if (NodeAddress != null)
            {
                // Nodes are addressed by 1+ord:
                if ((int)nodeCount == NodeAddress.Size())
                {
                    NodeAddress = NodeAddress.Resize(ArrayUtil.Oversize(NodeAddress.Size() + 1, NodeAddress.BitsPerValue));
                    InCounts = InCounts.Resize(ArrayUtil.Oversize(InCounts.Size() + 1, InCounts.BitsPerValue));
                }
                NodeAddress.Set((int)nodeCount, thisNodeAddress);
                // System.out.println("  write nodeAddress[" + nodeCount + "] = " + endAddress);
                node = nodeCount;
            }
            else
            {
                node = thisNodeAddress;
            }
            LastFrozenNode = node;

            //System.out.println("  ret node=" + node + " address=" + thisNodeAddress + " nodeAddress=" + nodeAddress);
            return node;
        }

        /// <summary>
        /// Fills virtual 'start' arc, ie, an empty incoming arc to
        ///  the FST's start node
        /// </summary>
        public Arc<T> GetFirstArc(Arc<T> arc)
        {
            if (emptyOutput != null)
            {
                arc.Flags = (sbyte)(BIT_FINAL_ARC | BIT_LAST_ARC);
                arc.NextFinalOutput = emptyOutput;
                if (!emptyOutput.Equals(NO_OUTPUT))
                {
                    arc.Flags |= (sbyte)BIT_ARC_HAS_FINAL_OUTPUT;
                }
            }
            else
            {
                arc.Flags = (sbyte)BIT_LAST_ARC;
                arc.NextFinalOutput = NO_OUTPUT;
            }
            arc.Output = NO_OUTPUT;

            // If there are no nodes, ie, the FST only accepts the
            // empty string, then startNode is 0
            arc.Target = StartNode;
            return arc;
        }

        /// <summary>
        /// Follows the <code>follow</code> arc and reads the last
        ///  arc of its target; this changes the provided
        ///  <code>arc</code> (2nd arg) in-place and returns it.
        /// </summary>
        /// <returns> Returns the second argument
        /// (<code>arc</code>).  </returns>
        public Arc<T> ReadLastTargetArc(Arc<T> follow, Arc<T> arc, FST.BytesReader @in)
        {
            //System.out.println("readLast");
            if (!TargetHasArcs(follow))
            {
                //System.out.println("  end node");
                Debug.Assert(follow.Final);
                arc.Label = END_LABEL;
                arc.Target = FINAL_END_NODE;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)BIT_LAST_ARC;
                return arc;
            }
            else
            {
                @in.Position = GetNodeAddress(follow.Target);
                arc.Node = follow.Target;
                var b = (sbyte)@in.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
                    // array: jump straight to end
                    arc.NumArcs = @in.ReadVInt();
                    if (Packed || Version >= VERSION_VINT_TARGET)
                    {
                        arc.BytesPerArc = @in.ReadVInt();
                    }
                    else
                    {
                        arc.BytesPerArc = @in.ReadInt();
                    }
                    //System.out.println("  array numArcs=" + arc.numArcs + " bpa=" + arc.bytesPerArc);
                    arc.PosArcsStart = @in.Position;
                    arc.ArcIdx = arc.NumArcs - 2;
                }
                else
                {
                    arc.Flags = b;
                    // non-array: linear scan
                    arc.BytesPerArc = 0;
                    //System.out.println("  scan");
                    while (!arc.Last)
                    {
                        // skip this arc:
                        ReadLabel(@in);
                        if (arc.Flag(BIT_ARC_HAS_OUTPUT))
                        {
                            Outputs.Read(@in);
                        }
                        if (arc.Flag(BIT_ARC_HAS_FINAL_OUTPUT))
                        {
                            Outputs.ReadFinalOutput(@in);
                        }
                        if (arc.Flag(BIT_STOP_NODE))
                        {
                        }
                        else if (arc.Flag(BIT_TARGET_NEXT))
                        {
                        }
                        else if (Packed)
                        {
                            @in.ReadVLong();
                        }
                        else
                        {
                            ReadUnpackedNodeTarget(@in);
                        }
                        arc.Flags = (sbyte)@in.ReadByte();
                    }
                    // Undo the byte flags we read:
                    @in.SkipBytes(-1);
                    arc.NextArc = @in.Position;
                }
                ReadNextRealArc(arc, @in);
                Debug.Assert(arc.Last);
                return arc;
            }
        }

        private long ReadUnpackedNodeTarget(BytesReader @in)
        {
            long target;
            if (Version < VERSION_VINT_TARGET)
            {
                target = @in.ReadInt();
            }
            else
            {
                target = @in.ReadVLong();
            }
            return target;
        }

        /// <summary>
        /// Follow the <code>follow</code> arc and read the first arc of its target;
        /// this changes the provided <code>arc</code> (2nd arg) in-place and returns
        /// it.
        /// </summary>
        /// <returns> Returns the second argument (<code>arc</code>). </returns>
        public Arc<T> ReadFirstTargetArc(Arc<T> follow, Arc<T> arc, FST.BytesReader @in)
        {
            //int pos = address;
            //System.out.println("    readFirstTarget follow.target=" + follow.Target + " isFinal=" + follow.isFinal());
            if (follow.Final)
            {
                // Insert "fake" final first arc:
                arc.Label = END_LABEL;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)BIT_FINAL_ARC;
                if (follow.Target <= 0)
                {
                    arc.Flags |= (sbyte)BIT_LAST_ARC;
                }
                else
                {
                    arc.Node = follow.Target;
                    // NOTE: nextArc is a node (not an address!) in this case:
                    arc.NextArc = follow.Target;
                }
                arc.Target = FINAL_END_NODE;
                //System.out.println("    insert isFinal; nextArc=" + follow.Target + " isLast=" + arc.isLast() + " output=" + outputs.outputToString(arc.Output));
                return arc;
            }
            else
            {
                return ReadFirstRealTargetArc(follow.Target, arc, @in);
            }
        }

        public Arc<T> ReadFirstRealTargetArc(long node, Arc<T> arc, FST.BytesReader @in)
        {
            long address = GetNodeAddress(node);
            @in.Position = address;
            //System.out.println("  readFirstRealTargtArc address="
            //+ address);
            //System.out.println("   flags=" + arc.flags);
            arc.Node = node;

            if (@in.ReadByte() == ARCS_AS_FIXED_ARRAY)
            {
                //System.out.println("  fixedArray");
                // this is first arc in a fixed-array
                arc.NumArcs = @in.ReadVInt();
                if (Packed || Version >= VERSION_VINT_TARGET)
                {
                    arc.BytesPerArc = @in.ReadVInt();
                }
                else
                {
                    arc.BytesPerArc = @in.ReadInt();
                }
                arc.ArcIdx = -1;
                arc.NextArc = arc.PosArcsStart = @in.Position;
                //System.out.println("  bytesPer=" + arc.bytesPerArc + " numArcs=" + arc.numArcs + " arcsStart=" + pos);
            }
            else
            {
                //arc.flags = b;
                arc.NextArc = address;
                arc.BytesPerArc = 0;
            }

            return ReadNextRealArc(arc, @in);
        }

        /// <summary>
        /// Checks if <code>arc</code>'s target state is in expanded (or vector) format.
        /// </summary>
        /// <returns> Returns <code>true</code> if <code>arc</code> points to a state in an
        /// expanded array format. </returns>
        public bool IsExpandedTarget(Arc<T> follow, BytesReader @in)
        {
            if (!TargetHasArcs(follow))
            {
                return false;
            }
            else
            {
                @in.Position = GetNodeAddress(follow.Target);
                return @in.ReadByte() == ARCS_AS_FIXED_ARRAY;
            }
        }

        /// <summary>
        /// In-place read; returns the arc. </summary>
        public Arc<T> ReadNextArc(Arc<T> arc, FST.BytesReader @in)
        {
            if (arc.Label == END_LABEL)
            {
                // this was a fake inserted "final" arc
                if (arc.NextArc <= 0)
                {
                    throw new System.ArgumentException("cannot readNextArc when arc.isLast()=true");
                }
                return ReadFirstRealTargetArc(arc.NextArc, arc, @in);
            }
            else
            {
                return ReadNextRealArc(arc, @in);
            }
        }

        /// <summary>
        /// Peeks at next arc's label; does not alter arc.  Do
        ///  not call this if arc.isLast()!
        /// </summary>
        public int ReadNextArcLabel(Arc<T> arc, BytesReader @in)
        {
            Debug.Assert(!arc.Last);

            if (arc.Label == END_LABEL)
            {
                //System.out.println("    nextArc fake " +
                //arc.nextArc);

                long pos = GetNodeAddress(arc.NextArc);
                @in.Position = pos;

                var b = (sbyte)@in.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
                    //System.out.println("    nextArc fixed array");
                    @in.ReadVInt();

                    // Skip bytesPerArc:
                    if (Packed || Version >= VERSION_VINT_TARGET)
                    {
                        @in.ReadVInt();
                    }
                    else
                    {
                        @in.ReadInt();
                    }
                }
                else
                {
                    @in.Position = pos;
                }
            }
            else
            {
                if (arc.BytesPerArc != 0)
                {
                    //System.out.println("    nextArc real array");
                    // arcs are at fixed entries
                    @in.Position = arc.PosArcsStart;
                    @in.SkipBytes((1 + arc.ArcIdx) * arc.BytesPerArc);
                }
                else
                {
                    // arcs are packed
                    //System.out.println("    nextArc real packed");
                    @in.Position = arc.NextArc;
                }
            }
            // skip flags
            @in.ReadByte();
            return ReadLabel(@in);
        }

        /// <summary>
        /// Never returns null, but you should never call this if
        ///  arc.isLast() is true.
        /// </summary>
        public Arc<T> ReadNextRealArc(Arc<T> arc, FST.BytesReader @in)
        {
            // TODO: can't assert this because we call from readFirstArc
            // assert !flag(arc.flags, BIT_LAST_ARC);

            // this is a continuing arc in a fixed array
            if (arc.BytesPerArc != 0)
            {
                // arcs are at fixed entries
                arc.ArcIdx++;
                Debug.Assert(arc.ArcIdx < arc.NumArcs);
                @in.Position = arc.PosArcsStart;
                @in.SkipBytes(arc.ArcIdx * arc.BytesPerArc);
            }
            else
            {
                // arcs are packed
                @in.Position = arc.NextArc;
            }
            arc.Flags = (sbyte)@in.ReadByte();
            arc.Label = ReadLabel(@in);

            if (arc.Flag(BIT_ARC_HAS_OUTPUT))
            {
                arc.Output = Outputs.Read(@in);
            }
            else
            {
                arc.Output = Outputs.NoOutput;
            }

            if (arc.Flag(BIT_ARC_HAS_FINAL_OUTPUT))
            {
                arc.NextFinalOutput = Outputs.ReadFinalOutput(@in);
            }
            else
            {
                arc.NextFinalOutput = Outputs.NoOutput;
            }

            if (arc.Flag(BIT_STOP_NODE))
            {
                if (arc.Flag(BIT_FINAL_ARC))
                {
                    arc.Target = FINAL_END_NODE;
                }
                else
                {
                    arc.Target = NON_FINAL_END_NODE;
                }
                arc.NextArc = @in.Position;
            }
            else if (arc.Flag(BIT_TARGET_NEXT))
            {
                arc.NextArc = @in.Position;
                // TODO: would be nice to make this lazy -- maybe
                // caller doesn't need the target and is scanning arcs...
                if (NodeAddress == null)
                {
                    if (!arc.Flag(BIT_LAST_ARC))
                    {
                        if (arc.BytesPerArc == 0)
                        {
                            // must scan
                            SeekToNextNode(@in);
                        }
                        else
                        {
                            @in.Position = arc.PosArcsStart;
                            @in.SkipBytes(arc.BytesPerArc * arc.NumArcs);
                        }
                    }
                    arc.Target = @in.Position;
                }
                else
                {
                    arc.Target = arc.Node - 1;
                    Debug.Assert(arc.Target > 0);
                }
            }
            else
            {
                if (Packed)
                {
                    long pos = @in.Position;
                    long code = @in.ReadVLong();
                    if (arc.Flag(BIT_TARGET_DELTA))
                    {
                        // Address is delta-coded from current address:
                        arc.Target = pos + code;
                        //System.out.println("    delta pos=" + pos + " delta=" + code + " target=" + arc.target);
                    }
                    else if (code < NodeRefToAddress.Size())
                    {
                        // Deref
                        arc.Target = NodeRefToAddress.Get((int)code);
                        //System.out.println("    deref code=" + code + " target=" + arc.target);
                    }
                    else
                    {
                        // Absolute
                        arc.Target = code;
                        //System.out.println("    abs code=" + code);
                    }
                }
                else
                {
                    arc.Target = ReadUnpackedNodeTarget(@in);
                }
                arc.NextArc = @in.Position;
            }
            return arc;
        }

        // TODO: could we somehow [partially] tableize arc lookups
        // look automaton?

        /// <summary>
        /// Finds an arc leaving the incoming arc, replacing the arc in place.
        ///  this returns null if the arc was not found, else the incoming arc.
        /// </summary>
        public Arc<T> FindTargetArc(int labelToMatch, Arc<T> follow, Arc<T> arc, FST.BytesReader @in)
        {
            if (labelToMatch == END_LABEL)
            {
                if (follow.Final)
                {
                    if (follow.Target <= 0)
                    {
                        arc.Flags = (sbyte)BIT_LAST_ARC;
                    }
                    else
                    {
                        arc.Flags = 0;
                        // NOTE: nextArc is a node (not an address!) in this case:
                        arc.NextArc = follow.Target;
                        arc.Node = follow.Target;
                    }
                    arc.Output = follow.NextFinalOutput;
                    arc.Label = END_LABEL;
                    return arc;
                }
                else
                {
                    return null;
                }
            }

            // Short-circuit if this arc is in the root arc cache:
            if (follow.Target == StartNode && labelToMatch < CachedRootArcs.Length)
            {
                // LUCENE-5152: detect tricky cases where caller
                // modified previously returned cached root-arcs:
                Debug.Assert(AssertRootArcs());
                Arc<T> result = CachedRootArcs[labelToMatch];
                if (result == null)
                {
                    return null;
                }
                else
                {
                    arc.CopyFrom(result);
                    return arc;
                }
            }

            if (!TargetHasArcs(follow))
            {
                return null;
            }

            @in.Position = GetNodeAddress(follow.Target);

            arc.Node = follow.Target;

            // System.out.println("fta label=" + (char) labelToMatch);

            if (@in.ReadByte() == ARCS_AS_FIXED_ARRAY)
            {
                // Arcs are full array; do binary search:
                arc.NumArcs = @in.ReadVInt();
                if (Packed || Version >= VERSION_VINT_TARGET)
                {
                    arc.BytesPerArc = @in.ReadVInt();
                }
                else
                {
                    arc.BytesPerArc = @in.ReadInt();
                }
                arc.PosArcsStart = @in.Position;
                int low = 0;
                int high = arc.NumArcs - 1;
                while (low <= high)
                {
                    //System.out.println("    cycle");
                    int mid = (int)((uint)(low + high) >> 1);
                    @in.Position = arc.PosArcsStart;
                    @in.SkipBytes(arc.BytesPerArc * mid + 1);
                    int midLabel = ReadLabel(@in);
                    int cmp = midLabel - labelToMatch;
                    if (cmp < 0)
                    {
                        low = mid + 1;
                    }
                    else if (cmp > 0)
                    {
                        high = mid - 1;
                    }
                    else
                    {
                        arc.ArcIdx = mid - 1;
                        //System.out.println("    found!");
                        return ReadNextRealArc(arc, @in);
                    }
                }

                return null;
            }

            // Linear scan
            ReadFirstRealTargetArc(follow.Target, arc, @in);

            while (true)
            {
                //System.out.println("  non-bs cycle");
                // TODO: we should fix this code to not have to create
                // object for the output of every arc we scan... only
                // for the matching arc, if found
                if (arc.Label == labelToMatch)
                {
                    //System.out.println("    found!");
                    return arc;
                }
                else if (arc.Label > labelToMatch)
                {
                    return null;
                }
                else if (arc.Last)
                {
                    return null;
                }
                else
                {
                    ReadNextRealArc(arc, @in);
                }
            }
        }

        private void SeekToNextNode(FST.BytesReader @in)
        {
            while (true)
            {
                int flags = @in.ReadByte();
                ReadLabel(@in);

                if (Flag(flags, BIT_ARC_HAS_OUTPUT))
                {
                    Outputs.Read(@in);
                }

                if (Flag(flags, BIT_ARC_HAS_FINAL_OUTPUT))
                {
                    Outputs.ReadFinalOutput(@in);
                }

                if (!Flag(flags, BIT_STOP_NODE) && !Flag(flags, BIT_TARGET_NEXT))
                {
                    if (Packed)
                    {
                        @in.ReadVLong();
                    }
                    else
                    {
                        ReadUnpackedNodeTarget(@in);
                    }
                }

                if (Flag(flags, BIT_LAST_ARC))
                {
                    return;
                }
            }
        }

        public long NodeCount
        {
            get
            {
                // 1+ in order to count the -1 implicit final node
                return 1 + nodeCount;
            }
        }

        public long ArcCount
        {
            get
            {
                return arcCount;
            }
        }

        public long ArcWithOutputCount
        {
            get
            {
                return arcWithOutputCount;
            }
        }

        /// <summary>
        /// Nodes will be expanded if their depth (distance from the root node) is
        /// &lt;= this value and their number of arcs is &gt;=
        /// <seealso cref="#FIXED_ARRAY_NUM_ARCS_SHALLOW"/>.
        ///
        /// <p>
        /// Fixed array consumes more RAM but enables binary search on the arcs
        /// (instead of a linear scan) on lookup by arc label.
        /// </summary>
        /// <returns> <code>true</code> if <code>node</code> should be stored in an
        ///         expanded (array) form.
        /// </returns>
        /// <seealso cref= #FIXED_ARRAY_NUM_ARCS_DEEP </seealso>
        /// <seealso cref= Builder.UnCompiledNode#depth </seealso>
        private bool ShouldExpand(Builder<T>.UnCompiledNode<T> node)
        {
            return AllowArrayArcs && ((node.Depth <= FIXED_ARRAY_SHALLOW_DISTANCE && node.NumArcs >= FIXED_ARRAY_NUM_ARCS_SHALLOW) || node.NumArcs >= FIXED_ARRAY_NUM_ARCS_DEEP);
        }

        /// <summary>
        /// Returns a <seealso cref="FST.BytesReader"/> for this FST, positioned at
        ///  position 0.
        /// </summary>
        public FST.BytesReader BytesReader
        {
            get
            {
                FST.BytesReader @in;
                if (Packed)
                {
                    @in = Bytes.ForwardReader;
                }
                else
                {
                    @in = Bytes.ReverseReader;
                }
                return @in;
            }
        }

        /*
              /// <summary>
              /// Reads bytes stored in an FST. </summary>
              public abstract class BytesReader : DataInput
              {
                /// <summary>
                /// Get current read position. </summary>
                public abstract long Position {get;set;}

                /// <summary>
                /// Returns true if this reader uses reversed bytes
                ///  under-the-hood.
                /// </summary>
                public abstract bool Reversed();

                /// <summary>
                /// Skips bytes. </summary>
                public abstract void SkipBytes(int count);
              }*/

        private class ArcAndState<T>
        {
            internal readonly Arc<T> Arc;
            internal readonly IntsRef Chain;

            public ArcAndState(Arc<T> arc, IntsRef chain)
            {
                this.Arc = arc;
                this.Chain = chain;
            }
        }

        /*
        public void countSingleChains() throws IOException {
          // TODO: must assert this FST was built with
          // "willRewrite"

          final List<ArcAndState<T>> queue = new ArrayList<>();

          // TODO: use bitset to not revisit nodes already
          // visited

          FixedBitSet seen = new FixedBitSet(1+nodeCount);
          int saved = 0;

          queue.add(new ArcAndState<T>(getFirstArc(new Arc<T>()), new IntsRef()));
          Arc<T> scratchArc = new Arc<>();
          while(queue.size() > 0) {
            //System.out.println("cycle size=" + queue.size());
            //for(ArcAndState<T> ent : queue) {
            //  System.out.println("  " + Util.toBytesRef(ent.chain, new BytesRef()));
            //  }
            final ArcAndState<T> arcAndState = queue.get(queue.size()-1);
            seen.set(arcAndState.arc.Node);
            final BytesRef br = Util.toBytesRef(arcAndState.chain, new BytesRef());
            if (br.length > 0 && br.bytes[br.length-1] == -1) {
              br.length--;
            }
            //System.out.println("  top node=" + arcAndState.arc.Target + " chain=" + br.utf8ToString());
            if (targetHasArcs(arcAndState.arc) && !seen.get(arcAndState.arc.target)) {
              // push
              readFirstTargetArc(arcAndState.arc, scratchArc);
              //System.out.println("  push label=" + (char) scratchArc.Label);
              //System.out.println("    tonode=" + scratchArc.Target + " last?=" + scratchArc.isLast());

              final IntsRef chain = IntsRef.deepCopyOf(arcAndState.chain);
              chain.grow(1+chain.length);
              // TODO
              //assert scratchArc.Label != END_LABEL;
              chain.ints[chain.length] = scratchArc.Label;
              chain.length++;

              if (scratchArc.isLast()) {
                if (scratchArc.Target != -1 && inCounts[scratchArc.target] == 1) {
                  //System.out.println("    append");
                } else {
                  if (arcAndState.chain.length > 1) {
                    saved += chain.length-2;
                    try {
                      System.out.println("chain: " + Util.toBytesRef(chain, new BytesRef()).utf8ToString());
                    } catch (AssertionError ae) {
                      System.out.println("chain: " + Util.toBytesRef(chain, new BytesRef()));
                    }
                  }
                  chain.length = 0;
                }
              } else {
                //System.out.println("    reset");
                if (arcAndState.chain.length > 1) {
                  saved += arcAndState.chain.length-2;
                  try {
                    System.out.println("chain: " + Util.toBytesRef(arcAndState.chain, new BytesRef()).utf8ToString());
                  } catch (AssertionError ae) {
                    System.out.println("chain: " + Util.toBytesRef(arcAndState.chain, new BytesRef()));
                  }
                }
                if (scratchArc.Target != -1 && inCounts[scratchArc.target] != 1) {
                  chain.length = 0;
                } else {
                  chain.ints[0] = scratchArc.Label;
                  chain.length = 1;
                }
              }
              // TODO: instead of new Arc() we can re-use from
              // a by-depth array
              queue.add(new ArcAndState<T>(new Arc<T>().copyFrom(scratchArc), chain));
            } else if (!arcAndState.arc.isLast()) {
              // next
              readNextArc(arcAndState.arc);
              //System.out.println("  next label=" + (char) arcAndState.arc.Label + " len=" + arcAndState.chain.length);
              if (arcAndState.chain.length != 0) {
                arcAndState.chain.ints[arcAndState.chain.length-1] = arcAndState.arc.Label;
              }
            } else {
              if (arcAndState.chain.length > 1) {
                saved += arcAndState.chain.length-2;
                System.out.println("chain: " + Util.toBytesRef(arcAndState.chain, new BytesRef()).utf8ToString());
              }
              // pop
              //System.out.println("  pop");
              queue.remove(queue.size()-1);
              while(queue.size() > 0 && queue.get(queue.size()-1).arc.isLast()) {
                queue.remove(queue.size()-1);
              }
              if (queue.size() > 0) {
                final ArcAndState<T> arcAndState2 = queue.get(queue.size()-1);
                readNextArc(arcAndState2.arc);
                //System.out.println("  read next=" + (char) arcAndState2.arc.Label + " queue=" + queue.size());
                assert arcAndState2.arc.Label != END_LABEL;
                if (arcAndState2.chain.length != 0) {
                  arcAndState2.chain.ints[arcAndState2.chain.length-1] = arcAndState2.arc.Label;
                }
              }
            }
          }

          System.out.println("TOT saved " + saved);
        }
       */

        // Creates a packed FST
        private FST(INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            Version = VERSION_CURRENT;
            Packed = true;
            this.inputType = inputType;
            Bytes = new BytesStore(bytesPageBits);
            this.Outputs = outputs;
            NO_OUTPUT = outputs.NoOutput;

            // NOTE: bogus because this is only used during
            // building; we need to break out mutable FST from
            // immutable
            AllowArrayArcs = false;
        }

        /// <summary>
        /// Expert: creates an FST by packing this one.  this
        ///  process requires substantial additional RAM (currently
        ///  up to ~8 bytes per node depending on
        ///  <code>acceptableOverheadRatio</code>), but then should
        ///  produce a smaller FST.
        ///
        ///  <p>The implementation of this method uses ideas from
        ///  <a target="_blank" href="http://www.cs.put.poznan.pl/dweiss/site/publications/download/fsacomp.pdf">Smaller Representation of Finite State Automata</a>,
        ///  which describes techniques to reduce the size of a FST.
        ///  However, this is not a strict implementation of the
        ///  algorithms described in this paper.
        /// </summary>
        internal FST<T> Pack(int minInCountDeref, int maxDerefNodes, float acceptableOverheadRatio)
        {
            // NOTE: maxDerefNodes is intentionally int: we cannot
            // support > 2.1B deref nodes

            // TODO: other things to try
            //   - renumber the nodes to get more next / better locality?
            //   - allow multiple input labels on an arc, so
            //     singular chain of inputs can take one arc (on
            //     wikipedia terms this could save another ~6%)
            //   - in the ord case, the output '1' is presumably
            //     very common (after NO_OUTPUT)... maybe use a bit
            //     for it..?
            //   - use spare bits in flags.... for top few labels /
            //     outputs / targets

            if (NodeAddress == null)
            {
                throw new System.ArgumentException("this FST was not built with willPackFST=true");
            }

            Arc<T> arc = new Arc<T>();

            BytesReader r = BytesReader;

            int topN = Math.Min(maxDerefNodes, InCounts.Size());

            // Find top nodes with highest number of incoming arcs:
            NodeQueue q = new NodeQueue(topN);

            // TODO: we could use more RAM efficient selection algo here...
            NodeAndInCount bottom = null;
            for (int node = 0; node < InCounts.Size(); node++)
            {
                if (InCounts.Get(node) >= minInCountDeref)
                {
                    if (bottom == null)
                    {
                        q.Add(new NodeAndInCount(node, (int)InCounts.Get(node)));
                        if (q.Size() == topN)
                        {
                            bottom = q.Top();
                        }
                    }
                    else if (InCounts.Get(node) > bottom.Count)
                    {
                        q.InsertWithOverflow(new NodeAndInCount(node, (int)InCounts.Get(node)));
                    }
                }
            }

            // Free up RAM:
            InCounts = null;

            IDictionary<int, int> topNodeMap = new Dictionary<int, int>();
            for (int downTo = q.Size() - 1; downTo >= 0; downTo--)
            {
                NodeAndInCount n = q.Pop();
                topNodeMap[n.Node] = downTo;
                //System.out.println("map node=" + n.Node + " inCount=" + n.count + " to newID=" + downTo);
            }

            // +1 because node ords start at 1 (0 is reserved as stop node):
            GrowableWriter newNodeAddress = new GrowableWriter(PackedInts.BitsRequired(this.Bytes.Position), (int)(1 + nodeCount), acceptableOverheadRatio);

            // Fill initial coarse guess:
            for (int node = 1; node <= nodeCount; node++)
            {
                newNodeAddress.Set(node, 1 + this.Bytes.Position - NodeAddress.Get(node));
            }

            int absCount;
            int deltaCount;
            int topCount;
            int nextCount;

            FST<T> fst;

            // Iterate until we converge:
            while (true)
            {
                //System.out.println("\nITER");
                bool changed = false;

                // for assert:
                bool negDelta = false;

                fst = new FST<T>(inputType, Outputs, Bytes.BlockBits);

                BytesStore writer = fst.Bytes;

                // Skip 0 byte since 0 is reserved target:
                writer.WriteByte(0);

                fst.arcWithOutputCount = 0;
                fst.nodeCount = 0;
                fst.arcCount = 0;

                absCount = deltaCount = topCount = nextCount = 0;

                int changedCount = 0;

                long addressError = 0;

                //int totWasted = 0;

                // Since we re-reverse the bytes, we now write the
                // nodes backwards, so that BIT_TARGET_NEXT is
                // unchanged:
                for (int node = (int)nodeCount; node >= 1; node--)
                {
                    fst.nodeCount++;
                    long address = writer.Position;

                    //System.out.println("  node: " + node + " address=" + address);
                    if (address != newNodeAddress.Get(node))
                    {
                        addressError = address - newNodeAddress.Get(node);
                        //System.out.println("    change: " + (address - newNodeAddress[node]));
                        changed = true;
                        newNodeAddress.Set(node, address);
                        changedCount++;
                    }

                    int nodeArcCount = 0;
                    int bytesPerArc = 0;

                    bool retry = false;

                    // for assert:
                    bool anyNegDelta = false;

                    // Retry loop: possibly iterate more than once, if
                    // this is an array'd node and bytesPerArc changes:
                    while (true) // retry writing this node
                    {
                        //System.out.println("  cycle: retry");
                        ReadFirstRealTargetArc(node, arc, r);

                        bool useArcArray = arc.BytesPerArc != 0;
                        if (useArcArray)
                        {
                            // Write false first arc:
                            if (bytesPerArc == 0)
                            {
                                bytesPerArc = arc.BytesPerArc;
                            }
                            writer.WriteByte((byte)ARCS_AS_FIXED_ARRAY);
                            writer.WriteVInt(arc.NumArcs);
                            writer.WriteVInt(bytesPerArc);
                            //System.out.println("node " + node + ": " + arc.numArcs + " arcs");
                        }

                        int maxBytesPerArc = 0;
                        //int wasted = 0;
                        while (true) // iterate over all arcs for this node
                        {
                            //System.out.println("    cycle next arc");

                            long arcStartPos = writer.Position;
                            nodeArcCount++;

                            sbyte flags = 0;

                            if (arc.Last)
                            {
                                flags += (sbyte)BIT_LAST_ARC;
                            }
                            /*
                            if (!useArcArray && nodeUpto < nodes.length-1 && arc.Target == nodes[nodeUpto+1]) {
                              flags += BIT_TARGET_NEXT;
                            }
                            */
                            if (!useArcArray && node != 1 && arc.Target == node - 1)
                            {
                                flags += (sbyte)BIT_TARGET_NEXT;
                                if (!retry)
                                {
                                    nextCount++;
                                }
                            }
                            if (arc.Final)
                            {
                                flags += (sbyte)BIT_FINAL_ARC;
                                if (!arc.NextFinalOutput.Equals(NO_OUTPUT))
                                {
                                    flags += (sbyte)BIT_ARC_HAS_FINAL_OUTPUT;
                                }
                            }
                            else
                            {
                                Debug.Assert(arc.NextFinalOutput.Equals(NO_OUTPUT));
                            }
                            if (!TargetHasArcs(arc))
                            {
                                flags += (sbyte)BIT_STOP_NODE;
                            }

                            if (!arc.Output.Equals(NO_OUTPUT))
                            {
                                flags += (sbyte)BIT_ARC_HAS_OUTPUT;
                            }

                            long absPtr;
                            bool doWriteTarget = TargetHasArcs(arc) && (flags & BIT_TARGET_NEXT) == 0;
                            if (doWriteTarget)
                            {
                                int ptr;
                                if (topNodeMap.TryGetValue((int)arc.Target, out ptr))
                                {
                                    absPtr = ptr;
                                }
                                else
                                {
                                    absPtr = topNodeMap.Count + newNodeAddress.Get((int)arc.Target) + addressError;
                                }

                                long delta = newNodeAddress.Get((int)arc.Target) + addressError - writer.Position - 2;
                                if (delta < 0)
                                {
                                    //System.out.println("neg: " + delta);
                                    anyNegDelta = true;
                                    delta = 0;
                                }

                                if (delta < absPtr)
                                {
                                    flags |= BIT_TARGET_DELTA;
                                }
                            }
                            else
                            {
                                absPtr = 0;
                            }

                            Debug.Assert(flags != ARCS_AS_FIXED_ARRAY);
                            writer.WriteByte((byte)(sbyte)flags);

                            fst.WriteLabel(writer, arc.Label);

                            if (!arc.Output.Equals(NO_OUTPUT))
                            {
                                Outputs.Write(arc.Output, writer);
                                if (!retry)
                                {
                                    fst.arcWithOutputCount++;
                                }
                            }
                            if (!arc.NextFinalOutput.Equals(NO_OUTPUT))
                            {
                                Outputs.WriteFinalOutput(arc.NextFinalOutput, writer);
                            }

                            if (doWriteTarget)
                            {
                                long delta = newNodeAddress.Get((int)arc.Target) + addressError - writer.Position;
                                if (delta < 0)
                                {
                                    anyNegDelta = true;
                                    //System.out.println("neg: " + delta);
                                    delta = 0;
                                }

                                if (Flag(flags, BIT_TARGET_DELTA))
                                {
                                    //System.out.println("        delta");
                                    writer.WriteVLong(delta);
                                    if (!retry)
                                    {
                                        deltaCount++;
                                    }
                                }
                                else
                                {
                                    /*
                                    if (ptr != null) {
                                      System.out.println("        deref");
                                    } else {
                                      System.out.println("        abs");
                                    }
                                    */
                                    writer.WriteVLong(absPtr);
                                    if (!retry)
                                    {
                                        if (absPtr >= topNodeMap.Count)
                                        {
                                            absCount++;
                                        }
                                        else
                                        {
                                            topCount++;
                                        }
                                    }
                                }
                            }

                            if (useArcArray)
                            {
                                int arcBytes = (int)(writer.Position - arcStartPos);
                                //System.out.println("  " + arcBytes + " bytes");
                                maxBytesPerArc = Math.Max(maxBytesPerArc, arcBytes);
                                // NOTE: this may in fact go "backwards", if
                                // somehow (rarely, possibly never) we use
                                // more bytesPerArc in this rewrite than the
                                // incoming FST did... but in this case we
                                // will retry (below) so it's OK to ovewrite
                                // bytes:
                                //wasted += bytesPerArc - arcBytes;
                                writer.SkipBytes((int)(arcStartPos + bytesPerArc - writer.Position));
                            }

                            if (arc.Last)
                            {
                                break;
                            }

                            ReadNextRealArc(arc, r);
                        }

                        if (useArcArray)
                        {
                            if (maxBytesPerArc == bytesPerArc || (retry && maxBytesPerArc <= bytesPerArc))
                            {
                                // converged
                                //System.out.println("  bba=" + bytesPerArc + " wasted=" + wasted);
                                //totWasted += wasted;
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }

                        //System.out.println("  retry this node maxBytesPerArc=" + maxBytesPerArc + " vs " + bytesPerArc);

                        // Retry:
                        bytesPerArc = maxBytesPerArc;
                        writer.Truncate(address);
                        nodeArcCount = 0;
                        retry = true;
                        anyNegDelta = false;
                        //writeNodeContinue:;
                    }
                    //writeNodeBreak:

                    negDelta |= anyNegDelta;

                    fst.arcCount += nodeArcCount;
                }

                if (!changed)
                {
                    // We don't renumber the nodes (just reverse their
                    // order) so nodes should only point forward to
                    // other nodes because we only produce acyclic FSTs
                    // w/ nodes only pointing "forwards":
                    Debug.Assert(!negDelta);
                    //System.out.println("TOT wasted=" + totWasted);
                    // Converged!
                    break;
                }
                //System.out.println("  " + changedCount + " of " + fst.nodeCount + " changed; retry");
            }

            long maxAddress = 0;
            foreach (long key in topNodeMap.Keys)
            {
                maxAddress = Math.Max(maxAddress, newNodeAddress.Get((int)key));
            }

            PackedInts.Mutable nodeRefToAddressIn = PackedInts.GetMutable(topNodeMap.Count, PackedInts.BitsRequired(maxAddress), acceptableOverheadRatio);
            foreach (KeyValuePair<int, int> ent in topNodeMap)
            {
                nodeRefToAddressIn.Set(ent.Value, newNodeAddress.Get(ent.Key));
            }
            fst.NodeRefToAddress = nodeRefToAddressIn;

            fst.StartNode = newNodeAddress.Get((int)StartNode);
            //System.out.println("new startNode=" + fst.startNode + " old startNode=" + startNode);

            if (emptyOutput != null)
            {
                fst.EmptyOutput = emptyOutput;
            }

            Debug.Assert(fst.nodeCount == nodeCount, "fst.nodeCount=" + fst.nodeCount + " nodeCount=" + nodeCount);
            Debug.Assert(fst.arcCount == arcCount);
            Debug.Assert(fst.arcWithOutputCount == arcWithOutputCount, "fst.arcWithOutputCount=" + fst.arcWithOutputCount + " arcWithOutputCount=" + arcWithOutputCount);

            fst.Bytes.Finish();
            fst.CacheRootArcs();

            //final int size = fst.sizeInBytes();
            //System.out.println("nextCount=" + nextCount + " topCount=" + topCount + " deltaCount=" + deltaCount + " absCount=" + absCount);

            return fst;
        }

        private class NodeAndInCount : IComparable<NodeAndInCount>
        {
            internal readonly int Node;
            internal readonly int Count;

            public NodeAndInCount(int node, int count)
            {
                this.Node = node;
                this.Count = count;
            }

            public virtual int CompareTo(NodeAndInCount other)
            {
                if (Count > other.Count)
                {
                    return 1;
                }
                else if (Count < other.Count)
                {
                    return -1;
                }
                else
                {
                    // Tie-break: smaller node compares as greater than
                    return other.Node - Node;
                }
            }
        }

        private class NodeQueue : PriorityQueue<NodeAndInCount>
        {
            public NodeQueue(int topN)
                : base(topN, false)
            {
            }

            public override bool LessThan(NodeAndInCount a, NodeAndInCount b)
            {
                int cmp = a.CompareTo(b);
                Debug.Assert(cmp != 0);
                return cmp < 0;
            }
        }
    }

    /// <summary>
    /// .NET Port: This new base class is to mimic Java's ability to use nested types without specifying
    /// a type parameter. i.e. FST.BytesReader instead of FST&lt;BytesRef&gt;.BytesReader
    /// </summary>
    public class FST
    {
        internal const int BIT_FINAL_ARC = 1 << 0;
        internal const int BIT_LAST_ARC = 1 << 1;
        internal const int BIT_TARGET_NEXT = 1 << 2;

        // TODO: we can free up a bit if we can nuke this:
        internal const int BIT_STOP_NODE = 1 << 3;

        internal const int BIT_ARC_HAS_OUTPUT = 1 << 4;
        internal const int BIT_ARC_HAS_FINAL_OUTPUT = 1 << 5;

        // Arcs are stored as fixed-size (per entry) array, so
        // that we can find an arc using binary search.  We do
        // this when number of arcs is > NUM_ARCS_ARRAY:

        // If set, thie target node is delta coded vs current position:
        internal const int BIT_TARGET_DELTA = 1 << 6;

        // We use this as a marker (because this one flag is
        // illegal by itself ...):
        internal const sbyte ARCS_AS_FIXED_ARRAY = BIT_ARC_HAS_FINAL_OUTPUT;

        /// <summary>
        /// <see cref="Builder{T}.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_SHALLOW_DISTANCE = 3;

        /// <summary>
        /// <see cref="Builder{T}.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_NUM_ARCS_SHALLOW = 5;

        /// <summary>
        /// <see cref="Builder{T}.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_NUM_ARCS_DEEP = 10;

        // Increment version to change it
        internal const string FILE_FORMAT_NAME = "FST";

        internal const int VERSION_START = 0;

        /// <summary>
        /// Changed numBytesPerArc for array'd case from byte to int.
        /// </summary>
        internal const int VERSION_INT_NUM_BYTES_PER_ARC = 1;

        /// <summary>
        /// Write BYTE2 labels as 2-byte short, not vInt.
        /// </summary>
        internal const int VERSION_SHORT_BYTE2_LABELS = 2;

        /// <summary>
        /// Added optional packed format.
        /// </summary>
        internal const int VERSION_PACKED = 3;

        /// <summary>
        /// Changed from int to vInt for encoding arc targets.
        /// Also changed maxBytesPerArc from int to vInt in the array case.
        /// </summary>
        internal const int VERSION_VINT_TARGET = 4;

        internal const int VERSION_CURRENT = VERSION_VINT_TARGET;

        // Never serialized; just used to represent the virtual
        // final node w/ no arcs:
        internal const long FINAL_END_NODE = -1;

        // Never serialized; just used to represent the virtual
        // non-final node w/ no arcs:
        internal const long NON_FINAL_END_NODE = 0;

        /// <summary>
        /// Reads bytes stored in an FST.
        /// </summary>
        public abstract class BytesReader : DataInput
        {
            /// <summary>
            /// Current read position
            /// </summary>
            public abstract long Position { get; set; }

            /// <summary>
            /// Returns true if this reader uses reversed bytes
            /// under-the-hood.
            /// </summary>
            /// <returns></returns>
            public abstract bool Reversed();

            /// <summary>
            /// Skips bytes.
            /// </summary>
            /// <param name="count"></param>
            public abstract void SkipBytes(int count);
        }

        /// <summary>
        /// Specifies allowed range of each int input label for this FST.
        /// </summary>
        public enum INPUT_TYPE { BYTE1, BYTE2, BYTE4 }

        /// <summary>
        /// Represents a single arc.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class Arc<T>
        {
            public int Label { get; set; }

            public T Output { get; set; }

            // From node (ord or address); currently only used when
            // building an FST w/ willPackFST=true:
            internal long Node { get; set; }

            /// <summary>
            /// To node (ord or address)
            /// </summary>
            public long Target { get; set; }

            internal sbyte Flags { get; set; }

            public T NextFinalOutput { get; set; }

            // address (into the byte[]), or ord/address if label == END_LABEL
            internal long NextArc { get; set; }

            // This is non-zero if current arcs are fixed array:
            internal long PosArcsStart { get; set; }

            internal int BytesPerArc { get; set; }

            internal int ArcIdx { get; set; }

            internal int NumArcs { get; set; }

            /// <summary>
            /// Return this
            /// </summary>
            /// <param name="other"></param>
            /// <returns></returns>
            public Arc<T> CopyFrom(Arc<T> other)
            {
                Node = other.Node;
                Label = other.Label;
                Target = other.Target;
                Flags = other.Flags;
                Output = other.Output;
                NextFinalOutput = other.Output;
                NextFinalOutput = other.NextFinalOutput;
                NextArc = other.NextArc;
                BytesPerArc = other.BytesPerArc;
                if (BytesPerArc != 0)
                {
                    PosArcsStart = other.PosArcsStart;
                    ArcIdx = other.ArcIdx;
                    NumArcs = other.NumArcs;
                }
                return this;
            }

            internal bool Flag(int flag)
            {
                return FST<T>.Flag(Flags, flag);
            }

            public bool Last
            {
                get { return Flag(BIT_LAST_ARC); }
            }

            public bool Final
            {
                get { return Flag(BIT_FINAL_ARC); }
            }

            public override string ToString()
            {
                var b = new StringBuilder();
                b.Append("node=" + Node);
                b.Append(" target=" + Target);
                b.Append(" label=" + Label);
                if (Flag(BIT_LAST_ARC)) b.Append(" last");
                if (Flag(BIT_FINAL_ARC)) b.Append(" final");
                if (Flag(BIT_TARGET_NEXT)) b.Append(" targetNext");
                if (Flag(BIT_ARC_HAS_OUTPUT)) b.Append(" output=" + Output);
                if (Flag(BIT_ARC_HAS_FINAL_OUTPUT)) b.Append(" nextFinalOutput=" + NextFinalOutput);
                if (BytesPerArc != 0) b.Append(" arcArray(idx=" + ArcIdx + " of " + NumArcs + ")");
                return b.ToString();
            }
        }

        internal class ArcAndState<T>
            where T : class
        {
            private readonly Arc<T> _arc;

            public Arc<T> Arc { get { return _arc; } }

            private readonly IntsRef _chain;

            public IntsRef Chain { get { return _chain; } }

            public ArcAndState(Arc<T> arc, IntsRef chain)
            {
                _arc = arc;
                _chain = chain;
            }
        }

        internal class NodeAndInCount : IComparable<NodeAndInCount>
        {
            private readonly int _node;

            public int Node { get { return _node; } }

            private readonly int _count;

            public int Count { get { return _count; } }

            public NodeAndInCount(int node, int count)
            {
                _node = node;
                _count = count;
            }

            public int CompareTo(NodeAndInCount other)
            {
                if (Count > other.Count)
                    return 1;
                if (Count < other.Count)
                    return -1;
                // Tie-break: smaller node compares as greater than
                return other.Node - Node;
            }
        }

        internal class NodeQueue : PriorityQueue<NodeAndInCount>
        {
            public NodeQueue(int topN)
                : base(topN, false)
            {
            }

            public override bool LessThan(NodeAndInCount a, NodeAndInCount b)
            {
                var cmp = a.CompareTo(b);
                // assert cmp != 0;
                return cmp < 0;
            }
        }
    }
}