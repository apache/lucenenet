using J2N.Collections;
using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Util.Fst
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

    using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;
    using CodecUtil = Lucene.Net.Codecs.CodecUtil;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using GrowableWriter = Lucene.Net.Util.Packed.GrowableWriter;
    using InputStreamDataInput = Lucene.Net.Store.InputStreamDataInput;
    using OutputStreamDataOutput = Lucene.Net.Store.OutputStreamDataOutput;
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
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
    /// compact <see cref="T:byte[]"/> format.
    /// <para/> The format is similar to what's used by Morfologik
    /// (http://sourceforge.net/projects/morfologik).
    ///
    /// <para/> See the <a href="https://lucene.apache.org/core/4_8_0/core/org/apache/lucene/util/fst/package-summary.html">
    /// FST package documentation</a> for some simple examples.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class FST<T>
        where T : class // LUCENENET specific - added class constraint, since we compare reference equality
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
        private readonly FST.INPUT_TYPE inputType;

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

        private int[] bytesPerArc = Arrays.Empty<int>();

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

        internal readonly BytesStore bytes;

        private long startNode = -1;

        public Outputs<T> Outputs { get; private set; }

        // Used for the BIT_TARGET_NEXT optimization (whereby
        // instead of storing the address of the target node for
        // a given arc, we mark a single bit noting that the next
        // node in the byte[] is the target node):
        private long lastFrozenNode;

        private readonly T NO_OUTPUT;

        // LUCENENET NOTE: changed accessibility of the following 3 fields
        // because we already have public properties that can read them.
        // This class is sealed, so it is unclear what the benefit of setting
        // them from outside is (if any).
        internal long nodeCount;
        private long arcCount;
        private long arcWithOutputCount;

        private readonly bool packed;
        private PackedInt32s.Reader nodeRefToAddress;

        ///// <summary>
        ///// If arc has this label then that arc is final/accepted </summary>
        //public static readonly int END_LABEL = -1;

        private readonly bool allowArrayArcs;

        private FST.Arc<T>[] cachedRootArcs;
        private FST.Arc<T>[] assertingCachedRootArcs; // only set wit assert

        // LUCENENET NOTE: Arc<T> moved into FST class

        internal static bool Flag(int flags, int bit)
        {
            return (flags & bit) != 0;
        }

        private GrowableWriter nodeAddress;

        // TODO: we could be smarter here, and prune periodically
        // as we go; high in-count nodes will "usually" become
        // clear early on:
        private GrowableWriter inCounts;

        private readonly int version;

        // make a new empty FST, for building; Builder invokes
        // this ctor
        internal FST(FST.INPUT_TYPE inputType, Outputs<T> outputs, bool willPackFST, float acceptableOverheadRatio, bool allowArrayArcs, int bytesPageBits)
        {
            this.inputType = inputType;
            this.Outputs = outputs;
            this.allowArrayArcs = allowArrayArcs;
            version = FST.VERSION_CURRENT;
            bytes = new BytesStore(bytesPageBits);
            // pad: ensure no node gets address 0 which is reserved to mean
            // the stop state w/ no arcs
            bytes.WriteByte(0);
            NO_OUTPUT = outputs.NoOutput;
            if (willPackFST)
            {
                nodeAddress = new GrowableWriter(15, 8, acceptableOverheadRatio);
                inCounts = new GrowableWriter(1, 8, acceptableOverheadRatio);
            }
            else
            {
                nodeAddress = null;
                inCounts = null;
            }

            emptyOutput = default;
            packed = false;
            nodeRefToAddress = null;
        }

        

        /// <summary>
        /// Load a previously saved FST. </summary>
        public FST(DataInput @in, Outputs<T> outputs)
            : this(@in, outputs, FST.DEFAULT_MAX_BLOCK_BITS)
        {
        }

        /// <summary>
        /// Load a previously saved FST; <paramref name="maxBlockBits"/> allows you to
        /// control the size of the <see cref="T:byte[]"/> pages used to hold the FST bytes.
        /// </summary>
        public FST(DataInput @in, Outputs<T> outputs, int maxBlockBits)
        {
            this.Outputs = outputs;

            if (maxBlockBits < 1 || maxBlockBits > 30)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBlockBits), "maxBlockBits should be 1 .. 30; got " + maxBlockBits); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            // NOTE: only reads most recent format; we don't have
            // back-compat promise for FSTs (they are experimental):
            version = CodecUtil.CheckHeader(@in, FST.FILE_FORMAT_NAME, FST.VERSION_PACKED, FST.VERSION_VINT32_TARGET);
            packed = @in.ReadByte() == 1;
            if (@in.ReadByte() == 1)
            {
                // accepts empty string
                // 1 KB blocks:
                BytesStore emptyBytes = new BytesStore(10);
                int numBytes = @in.ReadVInt32();
                emptyBytes.CopyBytes(@in, numBytes);

                // De-serialize empty-string output:
                FST.BytesReader reader;
                if (packed)
                {
                    reader = emptyBytes.GetForwardReader();
                }
                else
                {
                    reader = emptyBytes.GetReverseReader();
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
                emptyOutput = default;
            }
            var t = @in.ReadByte();
            inputType = t switch
            {
                0 => FST.INPUT_TYPE.BYTE1,
                1 => FST.INPUT_TYPE.BYTE2,
                2 => FST.INPUT_TYPE.BYTE4,
                _ => throw IllegalStateException.Create("invalid input type " + t),
            };
            if (packed)
            {
                nodeRefToAddress = PackedInt32s.GetReader(@in);
            }
            else
            {
                nodeRefToAddress = null;
            }
            startNode = @in.ReadVInt64();
            nodeCount = @in.ReadVInt64();
            arcCount = @in.ReadVInt64();
            arcWithOutputCount = @in.ReadVInt64();

            long numBytes_ = @in.ReadVInt64();
            bytes = new BytesStore(@in, numBytes_, 1 << maxBlockBits);

            NO_OUTPUT = outputs.NoOutput;

            CacheRootArcs();

            // NOTE: bogus because this is only used during
            // building; we need to break out mutable FST from
            // immutable
            allowArrayArcs = false;

            /*
            if (bytes.length == 665) {
              Writer w = new OutputStreamWriter(new FileOutputStream("out.dot"), StandardCharsets.UTF_8);
              Util.toDot(this, w, false, false);
              w.Dispose();
              System.out.println("Wrote FST to out.dot");
            }
            */
        }

        public FST.INPUT_TYPE InputType => inputType;

        /// <summary>
        /// Returns bytes used to represent the FST </summary>
        public long GetSizeInBytes()
        {
            long size = bytes.Position;
            if (packed)
            {
                size += nodeRefToAddress.RamBytesUsed();
            }
            else if (nodeAddress != null)
            {
                size += nodeAddress.RamBytesUsed();
                size += inCounts.RamBytesUsed();
            }
            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Finish(long newStartNode)
        {
            if (startNode != -1)
            {
                throw IllegalStateException.Create("already finished");
            }
            if (newStartNode == FST.FINAL_END_NODE && !EqualityComparer<T>.Default.Equals(emptyOutput, default))
            {
                newStartNode = 0;
            }
            startNode = newStartNode;
            bytes.Finish();

            CacheRootArcs();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long GetNodeAddress(long node)
        {
            if (nodeAddress != null)
            {
                // Deref
                return nodeAddress.Get((int)node);
            }
            else
            {
                // Straight
                return node;
            }
        }

        // Caches first 128 labels
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CacheRootArcs()
        {
            cachedRootArcs = (FST.Arc<T>[])new FST.Arc<T>[0x80];
            ReadRootArcs(cachedRootArcs);

            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(SetAssertingRootArcs(cachedRootArcs));
                Debugging.Assert(AssertRootArcs());
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReadRootArcs(FST.Arc<T>[] arcs)
        {
            FST.Arc<T> arc = new FST.Arc<T>();
            GetFirstArc(arc);
            FST.BytesReader @in = GetBytesReader();
            if (TargetHasArcs(arc))
            {
                ReadFirstRealTargetArc(arc.Target, arc, @in);
                while (true)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(arc.Label != FST.END_LABEL);
                    if (arc.Label < cachedRootArcs.Length)
                    {
                        arcs[arc.Label] = (new FST.Arc<T>()).CopyFrom(arc);
                    }
                    else
                    {
                        break;
                    }
                    if (arc.IsLast)
                    {
                        break;
                    }
                    ReadNextRealArc(arc, @in);
                }
            }
        }

        private bool SetAssertingRootArcs(FST.Arc<T>[] arcs) // Only called from assert
        {
            assertingCachedRootArcs = (FST.Arc<T>[])new FST.Arc<T>[arcs.Length];
            ReadRootArcs(assertingCachedRootArcs);
            return true;
        }

        private bool AssertRootArcs()
        {
            Debugging.Assert(cachedRootArcs != null);
            Debugging.Assert(assertingCachedRootArcs != null);
            for (int i = 0; i < cachedRootArcs.Length; i++)
            {
                FST.Arc<T> root = cachedRootArcs[i];
                FST.Arc<T> asserting = assertingCachedRootArcs[i];
                if (root != null)
                {
                    Debugging.Assert(root.ArcIdx == asserting.ArcIdx);
                    Debugging.Assert(root.BytesPerArc == asserting.BytesPerArc);
                    Debugging.Assert(root.Flags == asserting.Flags);
                    Debugging.Assert(root.Label == asserting.Label);
                    Debugging.Assert(root.NextArc == asserting.NextArc);

                    // LUCENENET NOTE: In .NET, IEnumerable will not equal another identical IEnumerable
                    // because it checks for reference equality, not that the list contents
                    // are the same. StructuralEqualityComparer.Default.Equals() will make that check.
                    Debugging.Assert(typeof(T).IsValueType 
                        ? JCG.EqualityComparer<T>.Default.Equals(root.NextFinalOutput, asserting.NextFinalOutput)
                        : StructuralEqualityComparer.Default.Equals(root.NextFinalOutput, asserting.NextFinalOutput));
                    Debugging.Assert(root.Node == asserting.Node);
                    Debugging.Assert(root.NumArcs == asserting.NumArcs);
                    Debugging.Assert(typeof(T).IsValueType
                        ? JCG.EqualityComparer<T>.Default.Equals(root.Output, asserting.Output)
                        : StructuralEqualityComparer.Default.Equals(root.Output, asserting.Output));
                    Debugging.Assert(root.PosArcsStart == asserting.PosArcsStart);
                    Debugging.Assert(root.Target == asserting.Target);
                }
                else
                {
                    Debugging.Assert(root is null && asserting is null);
                }
            }
            return true;
        }

        public T EmptyOutput
        {
            get => emptyOutput;
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
            if (startNode == -1)
            {
                throw IllegalStateException.Create("call finish first");
            }
            if (nodeAddress != null)
            {
                throw IllegalStateException.Create("cannot save an FST pre-packed FST; it must first be packed");
            }
            if (packed && !(nodeRefToAddress is PackedInt32s.Mutable))
            {
                throw IllegalStateException.Create("cannot save a FST which has been loaded from disk ");
            }
            CodecUtil.WriteHeader(@out, FST.FILE_FORMAT_NAME, FST.VERSION_CURRENT);
            if (packed)
            {
                @out.WriteByte(1);
            }
            else
            {
                @out.WriteByte(0);
            }
            // TODO: really we should encode this as an arc, arriving
            // to the root node, instead of special casing here:
            if (!EqualityComparer<T>.Default.Equals(emptyOutput, default))
            {
                // Accepts empty string
                @out.WriteByte(1);

                // Serialize empty-string output:
                var ros = new RAMOutputStream();
                Outputs.WriteFinalOutput(emptyOutput, ros);

                var emptyOutputBytes = new byte[(int)ros.Position]; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                ros.WriteTo(emptyOutputBytes, 0);

                if (!packed)
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
                @out.WriteVInt32(emptyOutputBytes.Length);
                @out.WriteBytes(emptyOutputBytes, 0, emptyOutputBytes.Length);
            }
            else
            {
                @out.WriteByte(0);
            }
            sbyte t;
            if (inputType == FST.INPUT_TYPE.BYTE1)
            {
                t = 0;
            }
            else if (inputType == FST.INPUT_TYPE.BYTE2)
            {
                t = 1;
            }
            else
            {
                t = 2;
            }
            @out.WriteByte((byte)t);
            if (packed)
            {
                ((PackedInt32s.Mutable)nodeRefToAddress).Save(@out);
            }
            @out.WriteVInt64(startNode);
            @out.WriteVInt64(nodeCount);
            @out.WriteVInt64(arcCount);
            @out.WriteVInt64(arcWithOutputCount);
            long numBytes = bytes.Position;
            @out.WriteVInt64(numBytes);
            bytes.WriteTo(@out);
        }

        /// <summary>
        /// Writes an automaton to a file.
        /// </summary>
        public void Save(FileInfo file)
        {
            bool success = false;
            var bs = file.OpenWrite();
            try
            {
                Save(new OutputStreamDataOutput(bs));
                success = true;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(bs);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(bs);
                }
            }
        }

        // LUCENENET NOTE: static Read<T>() was moved into the FST class
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteLabel(DataOutput @out, int v)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(v >= 0,"v={0}", v);
            if (inputType == FST.INPUT_TYPE.BYTE1)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(v <= 255,"v={0}", v);
                @out.WriteByte((byte)v);
            }
            else if (inputType == FST.INPUT_TYPE.BYTE2)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(v <= 65535,"v={0}", v);
                @out.WriteInt16((short)v);
            }
            else
            {
                @out.WriteVInt32(v);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int ReadLabel(DataInput @in)
        {
            int v;
            if (inputType == FST.INPUT_TYPE.BYTE1)
            {
                // Unsigned byte:
                v = @in.ReadByte() & 0xFF;
            }
            else if (inputType == FST.INPUT_TYPE.BYTE2)
            {
                // Unsigned short:
                v = @in.ReadInt16() & 0xFFFF;
            }
            else
            {
                v = @in.ReadVInt32();
            }
            return v;
        }

        /// <summary>
        /// returns <c>true</c> if the node at this address has any
        /// outgoing arcs
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TargetHasArcs(FST.Arc<T> arc)
        {
            return arc.Target > 0;
        }

        // serializes new node by appending its bytes to the end
        // of the current byte[]
        internal long AddNode(Builder.UnCompiledNode<T> nodeIn)
        {
            //System.out.println("FST.addNode pos=" + bytes.getPosition() + " numArcs=" + nodeIn.numArcs);
            if (nodeIn.NumArcs == 0)
            {
                if (nodeIn.IsFinal)
                {
                    return FST.FINAL_END_NODE;
                }
                else
                {
                    return FST.NON_FINAL_END_NODE;
                }
            }

            long startAddress = bytes.Position;
            //System.out.println("  startAddr=" + startAddress);

            bool doFixedArray = ShouldExpand(nodeIn);
            if (doFixedArray)
            {
                //System.out.println("  fixedArray");
                if (bytesPerArc.Length < nodeIn.NumArcs)
                {
                    bytesPerArc = new int[ArrayUtil.Oversize(nodeIn.NumArcs, 1)];
                }
            }

            arcCount += nodeIn.NumArcs;

            int lastArc = nodeIn.NumArcs - 1;

            long lastArcStart = bytes.Position;
            int maxBytesPerArc = 0;
            for (int arcIdx = 0; arcIdx < nodeIn.NumArcs; arcIdx++)
            {
                Builder.Arc<T> arc = nodeIn.Arcs[arcIdx];
                var target = (Builder.CompiledNode)arc.Target;
                int flags = 0;
                //System.out.println("  arc " + arcIdx + " label=" + arc.Label + " -> target=" + target.Node);

                if (arcIdx == lastArc)
                {
                    flags += FST.BIT_LAST_ARC;
                }

                if (lastFrozenNode == target.Node && !doFixedArray)
                {
                    // TODO: for better perf (but more RAM used) we
                    // could avoid this except when arc is "near" the
                    // last arc:
                    flags += FST.BIT_TARGET_NEXT;
                }

                if (arc.IsFinal)
                {
                    flags += FST.BIT_FINAL_ARC;
                    if (arc.NextFinalOutput != NO_OUTPUT)
                    {
                        flags += FST.BIT_ARC_HAS_FINAL_OUTPUT;
                    }
                }
                else if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(arc.NextFinalOutput == NO_OUTPUT);
                }

                bool targetHasArcs = target.Node > 0;

                if (!targetHasArcs)
                {
                    flags += FST.BIT_STOP_NODE;
                }
                else if (inCounts != null)
                {
                    inCounts.Set((int)target.Node, inCounts.Get((int)target.Node) + 1);
                }

                if (arc.Output != NO_OUTPUT)
                {
                    flags += FST.BIT_ARC_HAS_OUTPUT;
                }

                bytes.WriteByte((byte)flags);
                WriteLabel(bytes, arc.Label);

                // System.out.println("  write arc: label=" + (char) arc.Label + " flags=" + flags + " target=" + target.Node + " pos=" + bytes.getPosition() + " output=" + outputs.outputToString(arc.Output));

                if (arc.Output != NO_OUTPUT)
                {
                    Outputs.Write(arc.Output, bytes);
                    //System.out.println("    write output");
                    arcWithOutputCount++;
                }

                if (arc.NextFinalOutput != NO_OUTPUT)
                {
                    //System.out.println("    write final output");
                    Outputs.WriteFinalOutput(arc.NextFinalOutput, bytes);
                }

                if (targetHasArcs && (flags & FST.BIT_TARGET_NEXT) == 0)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(target.Node > 0);
                    //System.out.println("    write target");
                    bytes.WriteVInt64(target.Node);
                }

                // just write the arcs "like normal" on first pass,
                // but record how many bytes each one took, and max
                // byte size:
                if (doFixedArray)
                {
                    bytesPerArc[arcIdx] = (int)(bytes.Position - lastArcStart);
                    lastArcStart = bytes.Position;
                    maxBytesPerArc = Math.Max(maxBytesPerArc, bytesPerArc[arcIdx]);
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
                if (Debugging.AssertsEnabled) Debugging.Assert(maxBytesPerArc > 0);
                // 2nd pass just "expands" all arcs to take up a fixed
                // byte size

                //System.out.println("write int @pos=" + (fixedArrayStart-4) + " numArcs=" + nodeIn.numArcs);
                // create the header
                // TODO: clean this up: or just rewind+reuse and deal with it
                byte[] header = new byte[MAX_HEADER_SIZE];
                var bad = new ByteArrayDataOutput(header);
                // write a "false" first arc:
                bad.WriteByte((byte)FST.ARCS_AS_FIXED_ARRAY);
                bad.WriteVInt32(nodeIn.NumArcs);
                bad.WriteVInt32(maxBytesPerArc);
                int headerLen = bad.Position;

                long fixedArrayStart = startAddress + headerLen;

                // expand the arcs in place, backwards
                long srcPos = bytes.Position;
                long destPos = fixedArrayStart + nodeIn.NumArcs * maxBytesPerArc;
                if (Debugging.AssertsEnabled) Debugging.Assert(destPos >= srcPos);
                if (destPos > srcPos)
                {
                    bytes.SkipBytes((int)(destPos - srcPos));
                    for (int arcIdx = nodeIn.NumArcs - 1; arcIdx >= 0; arcIdx--)
                    {
                        destPos -= maxBytesPerArc;
                        srcPos -= bytesPerArc[arcIdx];
                        //System.out.println("  repack arcIdx=" + arcIdx + " srcPos=" + srcPos + " destPos=" + destPos);
                        if (srcPos != destPos)
                        {
                            //System.out.println("  copy len=" + bytesPerArc[arcIdx]);
                            if (Debugging.AssertsEnabled) Debugging.Assert(destPos > srcPos, "destPos={0} srcPos={1} arcIdx={2} maxBytesPerArc={3} bytesPerArc[arcIdx]={4} nodeIn.numArcs={5}", destPos, srcPos, arcIdx, maxBytesPerArc, bytesPerArc[arcIdx], nodeIn.NumArcs);
                            bytes.CopyBytes(srcPos, destPos, bytesPerArc[arcIdx]);
                        }
                    }
                }

                // now write the header
                bytes.WriteBytes(startAddress, header, 0, headerLen);
            }

            long thisNodeAddress = bytes.Position - 1;

            bytes.Reverse(startAddress, thisNodeAddress);

            // PackedInts uses int as the index, so we cannot handle
            // > 2.1B nodes when packing:
            if (nodeAddress != null && nodeCount == int.MaxValue)
            {
                throw IllegalStateException.Create("cannot create a packed FST with more than 2.1 billion nodes");
            }

            nodeCount++;
            long node;
            if (nodeAddress != null)
            {
                // Nodes are addressed by 1+ord:
                if ((int)nodeCount == nodeAddress.Count)
                {
                    nodeAddress = nodeAddress.Resize(ArrayUtil.Oversize(nodeAddress.Count + 1, nodeAddress.BitsPerValue));
                    inCounts = inCounts.Resize(ArrayUtil.Oversize(inCounts.Count + 1, inCounts.BitsPerValue));
                }
                nodeAddress.Set((int)nodeCount, thisNodeAddress);
                // System.out.println("  write nodeAddress[" + nodeCount + "] = " + endAddress);
                node = nodeCount;
            }
            else
            {
                node = thisNodeAddress;
            }
            lastFrozenNode = node;

            //System.out.println("  ret node=" + node + " address=" + thisNodeAddress + " nodeAddress=" + nodeAddress);
            return node;
        }

        /// <summary>
        /// Fills virtual 'start' arc, ie, an empty incoming arc to
        /// the FST's start node
        /// </summary>
        public FST.Arc<T> GetFirstArc(FST.Arc<T> arc)
        {
            if (null != emptyOutput) // LUCENENET: intentionally putting null on the left to avoid custom equality overrides
            {
                arc.Flags = FST.BIT_FINAL_ARC | FST.BIT_LAST_ARC;
                arc.NextFinalOutput = emptyOutput;
                if (emptyOutput != NO_OUTPUT)
                {
                    arc.Flags |= FST.BIT_ARC_HAS_FINAL_OUTPUT;
                }
            }
            else
            {
                arc.Flags = FST.BIT_LAST_ARC;
                arc.NextFinalOutput = NO_OUTPUT;
            }
            arc.Output = NO_OUTPUT;

            // If there are no nodes, ie, the FST only accepts the
            // empty string, then startNode is 0
            arc.Target = startNode;
            return arc;
        }

        /// <summary>
        /// Follows the <paramref name="follow"/> arc and reads the last
        /// arc of its target; this changes the provided
        /// <paramref name="arc"/> (2nd arg) in-place and returns it.
        /// </summary>
        /// <returns> Returns the second argument
        /// (<paramref name="arc"/>).  </returns>
        public FST.Arc<T> ReadLastTargetArc(FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in)
        {
            //System.out.println("readLast");
            if (!TargetHasArcs(follow))
            {
                //System.out.println("  end node");
                if (Debugging.AssertsEnabled) Debugging.Assert(follow.IsFinal);
                arc.Label = FST.END_LABEL;
                arc.Target = FST.FINAL_END_NODE;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)FST.BIT_LAST_ARC;
                return arc;
            }
            else
            {
                @in.Position = GetNodeAddress(follow.Target);
                arc.Node = follow.Target;
                var b = (sbyte)@in.ReadByte();
                if (b == FST.ARCS_AS_FIXED_ARRAY)
                {
                    // array: jump straight to end
                    arc.NumArcs = @in.ReadVInt32();
                    if (packed || version >= FST.VERSION_VINT32_TARGET)
                    {
                        arc.BytesPerArc = @in.ReadVInt32();
                    }
                    else
                    {
                        arc.BytesPerArc = @in.ReadInt32();
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
                    while (!arc.IsLast)
                    {
                        // skip this arc:
                        ReadLabel(@in);
                        if (arc.Flag(FST.BIT_ARC_HAS_OUTPUT))
                        {
                            Outputs.Read(@in);
                        }
                        if (arc.Flag(FST.BIT_ARC_HAS_FINAL_OUTPUT))
                        {
                            Outputs.ReadFinalOutput(@in);
                        }
                        if (arc.Flag(FST.BIT_STOP_NODE))
                        {
                        }
                        else if (arc.Flag(FST.BIT_TARGET_NEXT))
                        {
                        }
                        else if (packed)
                        {
                            @in.ReadVInt64();
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
                if (Debugging.AssertsEnabled) Debugging.Assert(arc.IsLast);
                return arc;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long ReadUnpackedNodeTarget(FST.BytesReader @in)
        {
            long target;
            if (version < FST.VERSION_VINT32_TARGET)
            {
                target = @in.ReadInt32();
            }
            else
            {
                target = @in.ReadVInt64();
            }
            return target;
        }

        /// <summary>
        /// Follow the <paramref name="follow"/> arc and read the first arc of its target;
        /// this changes the provided <paramref name="arc"/> (2nd arg) in-place and returns
        /// it.
        /// </summary>
        /// <returns> Returns the second argument (<paramref name="arc"/>). </returns>
        public FST.Arc<T> ReadFirstTargetArc(FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in)
        {
            //int pos = address;
            //Debug.WriteLine("    readFirstTarget follow.target=" + follow.Target + " isFinal=" + follow.IsFinal);
            if (follow.IsFinal)
            {
                // Insert "fake" final first arc:
                arc.Label = FST.END_LABEL;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)FST.BIT_FINAL_ARC;
                if (follow.Target <= 0)
                {
                    arc.Flags |= (sbyte)FST.BIT_LAST_ARC;
                }
                else
                {
                    arc.Node = follow.Target;
                    // NOTE: nextArc is a node (not an address!) in this case:
                    arc.NextArc = follow.Target;
                }
                arc.Target = FST.FINAL_END_NODE;
                //Debug.WriteLine("    insert isFinal; nextArc=" + follow.Target + " isLast=" + arc.IsLast + " output=" + Outputs.OutputToString(arc.Output));
                return arc;
            }
            else
            {
                return ReadFirstRealTargetArc(follow.Target, arc, @in);
            }
        }

        public FST.Arc<T> ReadFirstRealTargetArc(long node, FST.Arc<T> arc, FST.BytesReader @in)
        {
            long address = GetNodeAddress(node);
            @in.Position = address;
            //System.out.println("  readFirstRealTargtArc address="
            //+ address);
            //System.out.println("   flags=" + arc.flags);
            arc.Node = node;

            if (@in.ReadByte() == FST.ARCS_AS_FIXED_ARRAY)
            {
                //System.out.println("  fixedArray");
                // this is first arc in a fixed-array
                arc.NumArcs = @in.ReadVInt32();
                if (packed || version >= FST.VERSION_VINT32_TARGET)
                {
                    arc.BytesPerArc = @in.ReadVInt32();
                }
                else
                {
                    arc.BytesPerArc = @in.ReadInt32();
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
        /// Checks if arc's target state is in expanded (or vector) format.
        /// </summary>
        /// <returns> Returns <c>true</c> if arc points to a state in an
        /// expanded array format. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool IsExpandedTarget(FST.Arc<T> follow, FST.BytesReader @in)
        {
            if (!TargetHasArcs(follow))
            {
                return false;
            }
            else
            {
                @in.Position = GetNodeAddress(follow.Target);
                return @in.ReadByte() == FST.ARCS_AS_FIXED_ARRAY;
            }
        }

        /// <summary>
        /// In-place read; returns the arc. </summary>
        public FST.Arc<T> ReadNextArc(FST.Arc<T> arc, FST.BytesReader @in)
        {
            if (arc.Label == FST.END_LABEL)
            {
                // this was a fake inserted "final" arc
                if (arc.NextArc <= 0)
                {
                    throw new ArgumentException("cannot readNextArc when arc.IsLast=true");
                }
                return ReadFirstRealTargetArc(arc.NextArc, arc, @in);
            }
            else
            {
                return ReadNextRealArc(arc, @in);
            }
        }

        /// <summary>
        /// Peeks at next arc's label; does not alter <paramref name="arc"/>.  Do
        /// not call this if arc.IsLast!
        /// </summary>
        public int ReadNextArcLabel(FST.Arc<T> arc, FST.BytesReader @in)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!arc.IsLast);

            if (arc.Label == FST.END_LABEL)
            {
                //Debug.WriteLine("    nextArc fake " + arc.NextArc);

                long pos = GetNodeAddress(arc.NextArc);
                @in.Position = pos;

                var b = (sbyte)@in.ReadByte();
                if (b == FST.ARCS_AS_FIXED_ARRAY)
                {
                    //System.out.println("    nextArc fixed array");
                    @in.ReadVInt32();

                    // Skip bytesPerArc:
                    if (packed || version >= FST.VERSION_VINT32_TARGET)
                    {
                        @in.ReadVInt32();
                    }
                    else
                    {
                        @in.ReadInt32();
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
        /// Never returns <c>null</c>, but you should never call this if
        /// arc.IsLast is <c>true</c>.
        /// </summary>
        public FST.Arc<T> ReadNextRealArc(FST.Arc<T> arc, FST.BytesReader @in)
        {
            // TODO: can't assert this because we call from readFirstArc
            // assert !flag(arc.flags, BIT_LAST_ARC);

            // this is a continuing arc in a fixed array
            if (arc.BytesPerArc != 0)
            {
                // arcs are at fixed entries
                arc.ArcIdx++;
                if (Debugging.AssertsEnabled) Debugging.Assert(arc.ArcIdx < arc.NumArcs);
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

            if (arc.Flag(FST.BIT_ARC_HAS_OUTPUT))
            {
                arc.Output = Outputs.Read(@in);
            }
            else
            {
                arc.Output = Outputs.NoOutput;
            }

            if (arc.Flag(FST.BIT_ARC_HAS_FINAL_OUTPUT))
            {
                arc.NextFinalOutput = Outputs.ReadFinalOutput(@in);
            }
            else
            {
                arc.NextFinalOutput = Outputs.NoOutput;
            }

            if (arc.Flag(FST.BIT_STOP_NODE))
            {
                if (arc.Flag(FST.BIT_FINAL_ARC))
                {
                    arc.Target = FST.FINAL_END_NODE;
                }
                else
                {
                    arc.Target = FST.NON_FINAL_END_NODE;
                }
                arc.NextArc = @in.Position;
            }
            else if (arc.Flag(FST.BIT_TARGET_NEXT))
            {
                arc.NextArc = @in.Position;
                // TODO: would be nice to make this lazy -- maybe
                // caller doesn't need the target and is scanning arcs...
                if (nodeAddress is null)
                {
                    if (!arc.Flag(FST.BIT_LAST_ARC))
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(arc.Target > 0);
                }
            }
            else
            {
                if (packed)
                {
                    long pos = @in.Position;
                    long code = @in.ReadVInt64();
                    if (arc.Flag(FST.BIT_TARGET_DELTA))
                    {
                        // Address is delta-coded from current address:
                        arc.Target = pos + code;
                        //System.out.println("    delta pos=" + pos + " delta=" + code + " target=" + arc.target);
                    }
                    else if (code < nodeRefToAddress.Count)
                    {
                        // Deref
                        arc.Target = nodeRefToAddress.Get((int)code);
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
        /// Finds an arc leaving the incoming <paramref name="arc"/>, replacing the arc in place.
        /// this returns <c>null</c> if the arc was not found, else the incoming <paramref name="arc"/>.
        /// </summary>
        public FST.Arc<T> FindTargetArc(int labelToMatch, FST.Arc<T> follow, FST.Arc<T> arc, FST.BytesReader @in)
        {
            if (labelToMatch == FST.END_LABEL)
            {
                if (follow.IsFinal)
                {
                    if (follow.Target <= 0)
                    {
                        arc.Flags = (sbyte)FST.BIT_LAST_ARC;
                    }
                    else
                    {
                        arc.Flags = 0;
                        // NOTE: nextArc is a node (not an address!) in this case:
                        arc.NextArc = follow.Target;
                        arc.Node = follow.Target;
                    }
                    arc.Output = follow.NextFinalOutput;
                    arc.Label = FST.END_LABEL;
                    return arc;
                }
                else
                {
                    return null;
                }
            }

            // Short-circuit if this arc is in the root arc cache:
            if (follow.Target == startNode && labelToMatch < cachedRootArcs.Length)
            {
                // LUCENE-5152: detect tricky cases where caller
                // modified previously returned cached root-arcs:
                if (Debugging.AssertsEnabled) Debugging.Assert(AssertRootArcs());
                FST.Arc<T> result = cachedRootArcs[labelToMatch];
                if (result is null)
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

            if (@in.ReadByte() == FST.ARCS_AS_FIXED_ARRAY)
            {
                // Arcs are full array; do binary search:
                arc.NumArcs = @in.ReadVInt32();
                if (packed || version >= FST.VERSION_VINT32_TARGET)
                {
                    arc.BytesPerArc = @in.ReadVInt32();
                }
                else
                {
                    arc.BytesPerArc = @in.ReadInt32();
                }
                arc.PosArcsStart = @in.Position;
                int low = 0;
                int high = arc.NumArcs - 1;
                while (low <= high)
                {
                    //System.out.println("    cycle");
                    int mid = (low + high).TripleShift(1);
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
                else if (arc.IsLast)
                {
                    return null;
                }
                else
                {
                    ReadNextRealArc(arc, @in);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SeekToNextNode(FST.BytesReader @in)
        {
            while (true)
            {
                int flags = @in.ReadByte();
                ReadLabel(@in);

                if (Flag(flags, FST.BIT_ARC_HAS_OUTPUT))
                {
                    Outputs.Read(@in);
                }

                if (Flag(flags, FST.BIT_ARC_HAS_FINAL_OUTPUT))
                {
                    Outputs.ReadFinalOutput(@in);
                }

                if (!Flag(flags, FST.BIT_STOP_NODE) && !Flag(flags, FST.BIT_TARGET_NEXT))
                {
                    if (packed)
                    {
                        @in.ReadVInt64();
                    }
                    else
                    {
                        ReadUnpackedNodeTarget(@in);
                    }
                }

                if (Flag(flags, FST.BIT_LAST_ARC))
                {
                    return;
                }
            }
        }

        public long NodeCount =>
            // 1+ in order to count the -1 implicit final node
            1 + nodeCount;

        public long ArcCount => arcCount;

        public long ArcWithOutputCount => arcWithOutputCount;

        /// <summary>
        /// Nodes will be expanded if their depth (distance from the root node) is
        /// &lt;= this value and their number of arcs is &gt;=
        /// <see cref="FST.FIXED_ARRAY_NUM_ARCS_SHALLOW"/>.
        ///
        /// <para/>
        /// Fixed array consumes more RAM but enables binary search on the arcs
        /// (instead of a linear scan) on lookup by arc label.
        /// </summary>
        /// <returns> <c>true</c> if <paramref name="node"/> should be stored in an
        ///         expanded (array) form.
        /// </returns>
        /// <seealso cref="FST.FIXED_ARRAY_NUM_ARCS_DEEP"/>
        /// <seealso cref="Builder.UnCompiledNode{S}.Depth"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool ShouldExpand(Builder.UnCompiledNode<T> node)
        {
            return allowArrayArcs && ((node.Depth <= FST.FIXED_ARRAY_SHALLOW_DISTANCE && node.NumArcs >= FST.FIXED_ARRAY_NUM_ARCS_SHALLOW) || node.NumArcs >= FST.FIXED_ARRAY_NUM_ARCS_DEEP);
        }

        /// <summary>
        /// Returns a <see cref="FST.BytesReader"/> for this FST, positioned at
        /// position 0.
        /// </summary>
        public FST.BytesReader GetBytesReader()
        {
            FST.BytesReader @in;
            if (packed)
            {
                @in = bytes.GetForwardReader();
            }
            else
            {
                @in = bytes.GetReverseReader();
            }
            return @in;
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
                public abstract bool IsReversed { get; }

                /// <summary>
                /// Skips bytes. </summary>
                public abstract void SkipBytes(int count);
              }*/


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

        /// <summary>
        /// Creates a packed FST
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private FST(FST.INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            version = FST.VERSION_CURRENT;
            packed = true;
            this.inputType = inputType;
            bytes = new BytesStore(bytesPageBits);
            this.Outputs = outputs;
            NO_OUTPUT = outputs.NoOutput;

            // NOTE: bogus because this is only used during
            // building; we need to break out mutable FST from
            // immutable
            allowArrayArcs = false;
        }

        /// <summary>
        /// Expert: creates an FST by packing this one.  This
        /// process requires substantial additional RAM (currently
        /// up to ~8 bytes per node depending on
        /// <c>acceptableOverheadRatio</c>), but then should
        /// produce a smaller FST.
        ///
        /// <para/>The implementation of this method uses ideas from
        /// <a target="_blank" href="http://www.cs.put.poznan.pl/dweiss/site/publications/download/fsacomp.pdf">Smaller Representation of Finite State Automata</a>,
        /// which describes techniques to reduce the size of a FST.
        /// However, this is not a strict implementation of the
        /// algorithms described in this paper.
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

            if (nodeAddress is null)
            {
                throw new ArgumentException("this FST was not built with willPackFST=true");
            }

            FST.Arc<T> arc = new FST.Arc<T>();

            FST.BytesReader r = GetBytesReader();

            int topN = Math.Min(maxDerefNodes, inCounts.Count);

            // Find top nodes with highest number of incoming arcs:
            IDictionary<int, int> topNodeMap = null;

#nullable enable

            // LUCENENET: Refactored PriorityQueue<T> subclass into PriorityComparer<T>
            // implementation, which can be passed into ValuePriorityQueue. ValuePriorityQueue
            // lives on the stack, and if the array size is small enough, we also allocate the
            // array on the stack. Fallback to the array pool if it is beyond MaxStackByteLimit.
            int bufferSize = PriorityQueue.GetArrayHeapSize(topN);
            bool usePool = FST.NodeAndInCount.ByteSize * bufferSize > Constants.MaxStackByteLimit;
            FST.NodeAndInCount?[]? arrayToReturnToPool = usePool ? ArrayPool<FST.NodeAndInCount?>.Shared.Rent(bufferSize) : null;
            try
            {
                Span<FST.NodeAndInCount?> buffer = usePool ? arrayToReturnToPool : stackalloc FST.NodeAndInCount?[bufferSize];
                var q = new ValuePriorityQueue<FST.NodeAndInCount?>(buffer, FST.NodeComparer.Default);
#nullable restore

                // TODO: we could use more RAM efficient selection algo here...
                FST.NodeAndInCount? bottom = null;

                for (int node = 0; node < inCounts.Count; node++)
                {
                    if (inCounts.Get(node) >= minInCountDeref)
                    {
                        if (!bottom.HasValue)
                        {
                            q.Add(new FST.NodeAndInCount(node, (int)inCounts.Get(node)));
                            if (q.Count == topN)
                            {
                                bottom = q.Top;
                            }
                        }
                        else if (inCounts.Get(node) > bottom.Value.Count)
                        {
                            q.InsertWithOverflow(new FST.NodeAndInCount(node, (int)inCounts.Get(node)));
                        }
                    }
                }

                // Free up RAM:
                inCounts = null;

                topNodeMap = new Dictionary<int, int>(q.Count); // LUCENENET: Allocate the dictionary prior to the loop
                for (int downTo = q.Count - 1; downTo >= 0; downTo--)
                {
                    FST.NodeAndInCount? n = q.Pop();
                    topNodeMap[n.Value.Node] = downTo; // LUCENENET: we are bound in the loop by Count, so we won't go past the end and get null here.
                    //System.out.println("map node=" + n.Node + " inCount=" + n.count + " to newID=" + downTo);
                }
            }
            finally
            {
                if (arrayToReturnToPool is not null)
                    ArrayPool<FST.NodeAndInCount?>.Shared.Return(arrayToReturnToPool);
            }

            // +1 because node ords start at 1 (0 is reserved as stop node):
            GrowableWriter newNodeAddress = new GrowableWriter(PackedInt32s.BitsRequired(this.bytes.Position), (int)(1 + nodeCount), acceptableOverheadRatio);

            // Fill initial coarse guess:
            for (int node = 1; node <= nodeCount; node++)
            {
                newNodeAddress.Set(node, 1 + this.bytes.Position - nodeAddress.Get(node));
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

                fst = new FST<T>(inputType, Outputs, bytes.BlockBits);

                BytesStore writer = fst.bytes;

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
                            writer.WriteByte((byte)FST.ARCS_AS_FIXED_ARRAY);
                            writer.WriteVInt32(arc.NumArcs);
                            writer.WriteVInt32(bytesPerArc);
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

                            if (arc.IsLast)
                            {
                                flags += (sbyte)FST.BIT_LAST_ARC;
                            }
                            /*
                            if (!useArcArray && nodeUpto < nodes.length-1 && arc.Target == nodes[nodeUpto+1]) {
                              flags += BIT_TARGET_NEXT;
                            }
                            */
                            if (!useArcArray && node != 1 && arc.Target == node - 1)
                            {
                                flags += (sbyte)FST.BIT_TARGET_NEXT;
                                if (!retry)
                                {
                                    nextCount++;
                                }
                            }
                            if (arc.IsFinal)
                            {
                                flags += (sbyte)FST.BIT_FINAL_ARC;
                                if (arc.NextFinalOutput != NO_OUTPUT)
                                {
                                    flags += (sbyte)FST.BIT_ARC_HAS_FINAL_OUTPUT;
                                }
                            }
                            else
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(arc.NextFinalOutput == NO_OUTPUT);
                            }
                            if (!TargetHasArcs(arc))
                            {
                                flags += (sbyte)FST.BIT_STOP_NODE;
                            }

                            if (arc.Output != NO_OUTPUT)
                            {
                                flags += (sbyte)FST.BIT_ARC_HAS_OUTPUT;
                            }

                            long absPtr;
                            bool doWriteTarget = TargetHasArcs(arc) && (flags & FST.BIT_TARGET_NEXT) == 0;
                            if (doWriteTarget)
                            {
                                if (topNodeMap.TryGetValue((int)arc.Target, out int ptr))
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
                                    flags |= FST.BIT_TARGET_DELTA;
                                }
                            }
                            else
                            {
                                absPtr = 0;
                            }

                            if (Debugging.AssertsEnabled) Debugging.Assert(flags != FST.ARCS_AS_FIXED_ARRAY);
                            writer.WriteByte((byte)flags);

                            fst.WriteLabel(writer, arc.Label);

                            if (arc.Output != NO_OUTPUT)
                            {
                                Outputs.Write(arc.Output, writer);
                                if (!retry)
                                {
                                    fst.arcWithOutputCount++;
                                }
                            }
                            if (arc.NextFinalOutput != NO_OUTPUT)
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

                                if (Flag(flags, FST.BIT_TARGET_DELTA))
                                {
                                    //System.out.println("        delta");
                                    writer.WriteVInt64(delta);
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
                                    writer.WriteVInt64(absPtr);
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

                            if (arc.IsLast)
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
                    if (Debugging.AssertsEnabled) Debugging.Assert(!negDelta);
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

            PackedInt32s.Mutable nodeRefToAddressIn = PackedInt32s.GetMutable(topNodeMap.Count, PackedInt32s.BitsRequired(maxAddress), acceptableOverheadRatio);
            foreach (KeyValuePair<int, int> ent in topNodeMap)
            {
                nodeRefToAddressIn.Set(ent.Value, newNodeAddress.Get(ent.Key));
            }
            fst.nodeRefToAddress = nodeRefToAddressIn;

            fst.startNode = newNodeAddress.Get((int)startNode);
            //System.out.println("new startNode=" + fst.startNode + " old startNode=" + startNode);

            if (!JCG.EqualityComparer<T>.Default.Equals(emptyOutput, default))
            {
                fst.EmptyOutput = emptyOutput;
            }

            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(fst.nodeCount == nodeCount,"fst.nodeCount={0} nodeCount={1}", fst.nodeCount, nodeCount);
                Debugging.Assert(fst.arcCount == arcCount);
                Debugging.Assert(fst.arcWithOutputCount == arcWithOutputCount,"fst.arcWithOutputCount={0} arcWithOutputCount={1}", fst.arcWithOutputCount, arcWithOutputCount);
            }

            fst.bytes.Finish();
            fst.CacheRootArcs();

            //final int size = fst.sizeInBytes();
            //System.out.println("nextCount=" + nextCount + " topCount=" + topCount + " deltaCount=" + deltaCount + " absCount=" + absCount);

            return fst;
        }
    }

    /// <summary>
    /// LUCENENET specific: This new base class is to mimic Java's ability to use nested types without specifying
    /// a type parameter. i.e. FST.BytesReader instead of FST&lt;BytesRef&gt;.BytesReader
    /// </summary>
    public sealed class FST
    {
        public static readonly int DEFAULT_MAX_BLOCK_BITS = Constants.RUNTIME_IS_64BIT ? 30 : 28;

        public FST()
        { }

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
        /// <see cref="Builder.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_SHALLOW_DISTANCE = 3;

        /// <summary>
        /// <see cref="Builder.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_NUM_ARCS_SHALLOW = 5;

        /// <summary>
        /// <see cref="Builder.UnCompiledNode{S}"/>
        /// </summary>
        public const int FIXED_ARRAY_NUM_ARCS_DEEP = 10;

        // Increment version to change it
        internal const string FILE_FORMAT_NAME = "FST";

        internal const int VERSION_START = 0;

        /// <summary>
        /// Changed numBytesPerArc for array'd case from byte to <see cref="int"/>.
        /// <para/>
        /// NOTE: This was VERSION_INT_NUM_BYTES_PER_ARC in Lucene
        /// </summary>
        internal const int VERSION_INT32_NUM_BYTES_PER_ARC = 1;

        /// <summary>
        /// Write BYTE2 labels as 2-byte <see cref="short"/>, not v<see cref="int"/>.
        /// <para/>
        /// NOTE: This was VERSION_SHORT_BYTE2_LABELS in Lucene
        /// </summary>
        internal const int VERSION_INT16_BYTE2_LABELS = 2;

        /// <summary>
        /// Added optional packed format.
        /// </summary>
        internal const int VERSION_PACKED = 3;

        /// <summary>
        /// Changed from <see cref="int"/> to v<see cref="int"/> for encoding arc targets.
        /// Also changed maxBytesPerArc from int to v<see cref="int"/> in the array case.
        /// <para/>
        /// NOTE: This was VERSION_VINT_TARGET in Lucene
        /// </summary>
        internal const int VERSION_VINT32_TARGET = 4;

        internal const int VERSION_CURRENT = VERSION_VINT32_TARGET;

        /// <summary>
        /// Never serialized; just used to represent the virtual
        /// final node w/ no arcs:
        /// </summary>
        internal const long FINAL_END_NODE = -1;

        /// <summary>
        /// Never serialized; just used to represent the virtual
        /// non-final node w/ no arcs:
        /// </summary>
        internal const long NON_FINAL_END_NODE = 0;

        /// <summary>
        /// If arc has this label then that arc is final/accepted </summary>
        public static readonly int END_LABEL = -1;

        /// <summary>
        /// returns <c>true</c> if the node at this address has any
        /// outgoing arcs
        /// </summary>
        public static bool TargetHasArcs<T>(Arc<T> arc) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            return arc.Target > 0;
        }

        /// <summary>
        /// Reads an automaton from a file.
        /// </summary>
        public static FST<T> Read<T>(FileInfo file, Outputs<T> outputs) where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            var bs = new BufferedStream(file.OpenRead());
            bool success = false;
            try
            {
                FST<T> fst = new FST<T>(new InputStreamDataInput(bs), outputs);
                success = true;
                return fst;
            }
            finally
            {
                if (success)
                {
                    IOUtils.Dispose(bs);
                }
                else
                {
                    IOUtils.DisposeWhileHandlingException(bs);
                }
            }
        }

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
            /// Returns <c>true</c> if this reader uses reversed bytes
            /// under-the-hood.
            /// </summary>
            /// <returns></returns>
            public abstract bool IsReversed { get; }

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
            where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            public int Label { get; set; }

            public T Output { get; set; }

            /// <summary>
            /// From node (ord or address); currently only used when
            /// building an FST w/ willPackFST=true:
            /// </summary>
            internal long Node { get; set; }

            /// <summary>
            /// To node (ord or address)
            /// </summary>
            public long Target { get; set; }

            internal sbyte Flags { get; set; }

            public T NextFinalOutput { get; set; }

            /// <summary>
            /// address (into the byte[]), or ord/address if label == END_LABEL
            /// </summary>
            internal long NextArc { get; set; }

            /// <summary>
            /// This is non-zero if current arcs are fixed array:
            /// </summary>
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

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual bool Flag(int flag)
            {
                return FST<T>.Flag(Flags, flag);
            }

            public virtual bool IsLast => Flag(BIT_LAST_ARC);

            public virtual bool IsFinal => Flag(BIT_FINAL_ARC);

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

        internal class ArcAndState<T> where T : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            internal Arc<T> Arc { get; private set; }
            internal Int32sRef Chain { get; private set; }

            public ArcAndState(Arc<T> arc, Int32sRef chain)
            {
                this.Arc = arc;
                this.Chain = chain;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct NodeAndInCount : IComparable<NodeAndInCount> // LUCENENET specific - made into a struct
        {
            public const int ByteSize = sizeof(int) + sizeof(int);

            [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
            [SuppressMessage("Major Code Smell", "S2933:Fields that are only assigned in the constructor should be \"readonly\"", Justification = "Structs are known to have performance issues with readonly fields")]
            [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Structs are known to have performance issues with readonly fields")]
            private int node;
            [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
            [SuppressMessage("Major Code Smell", "S2933:Fields that are only assigned in the constructor should be \"readonly\"", Justification = "Structs are known to have performance issues with readonly fields")]
            [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "Structs are known to have performance issues with readonly fields")]
            private int count;

            internal int Node => node;
            internal int Count => count;

            public NodeAndInCount(int node, int count)
            {
                this.node = node;
                this.count = count;
            }

            public int CompareTo(NodeAndInCount other)
            {
                if (count > other.count)
                {
                    return 1;
                }
                else if (count < other.count)
                {
                    return -1;
                }
                else
                {
                    // Tie-break: smaller node compares as greater than
                    return other.node - node;
                }
            }
        }

#nullable enable
        // LUCENENET: Refactored NodeQueue into NodeComparer
        // implementation, which can be passed into ValuePriorityQueue.
        internal class NodeComparer : PriorityComparer<NodeAndInCount?>
        {
            public static NodeComparer Default { get; } = new NodeComparer();

            protected internal override bool LessThan(NodeAndInCount? a, NodeAndInCount? b)
            {
                    // LUCENENET specific - added guard clauses
                    if (a is null)
                        throw new ArgumentNullException(nameof(a));
                    if (b is null)
                        throw new ArgumentNullException(nameof(b));

                    int cmp = a.Value.CompareTo(b.Value);
                if (Debugging.AssertsEnabled) Debugging.Assert(cmp != 0);
                return cmp < 0;
            }
        }
    }
}