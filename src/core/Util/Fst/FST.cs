using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util.Packed;
using Lucene.Net.Codecs;

namespace Lucene.Net.Util.Fst
{
    public sealed class FST<T> : FST
    {
        private readonly INPUT_TYPE inputType;
        public INPUT_TYPE InputType { get { return inputType; } }

        private int[] bytesPerArc = new int[0];

        // if non-null, this FST accepts the empty string and
        // produces this output
        private T emptyOutput;
        public T EmptyOutput
        {
            get { return emptyOutput; }
            set
            {
                emptyOutput =
                    emptyOutput != null ?
                    Outputs.Merge(emptyOutput, value) :
                    value;
            }
        }

        private readonly BytesStore bytes;
        public BytesStore Bytes { get { return bytes; } }

        private long startNode = -1;

        private readonly Outputs<T> outputs;
        public Outputs<T> Outputs { get { return outputs; } }

        // Used for the BIT_TARGET_NEXT optimization (whereby
        // instead of storing the address of the target node for
        // a given arc, we mark a single bit noting that the next
        // node in the byte[] is the target node):
        private long lastFrozenNode;

        private readonly T NO_OUTPUT;

        internal long NodeCount { get; set; }
        public long ArcCount { get; set; }
        public long ArcWithOutputCount { get; set; }

        private readonly bool packed;
        private PackedInts.IReader nodeRefToAddress;

        /// <summary>
        /// If arc has this label then that arc is final/accepted
        /// </summary>
        public const int END_LABEL = -1;

        private readonly bool allowArrayArcs;
        public bool AllowArrayArcs { get { return allowArrayArcs; } }

        private Arc<T>[] cachedRootArcs;

        internal static bool Flag(int flags, int bits)
        {
            return (flags & bits) != 0;
        }

        private GrowableWriter NodeAddress;

        // TODO: we could be smarter here, and prune periodically
        // as we go; high in-count nodes will "usually" become
        // clear early on:
        private GrowableWriter InCounts;

        private readonly int Version;

        // make a new empty FST, for building; Builder invokes
        // this ctor
        internal FST(INPUT_TYPE inputType, Outputs<T> outputs, bool willPackFST, float acceptableOverheadRatio,
            bool allowArrayArcs, int bytesPageBits)
        {
            this.inputType = inputType;
            this.outputs = outputs;
            this.allowArrayArcs = allowArrayArcs;
            Version = VERSION_CURRENT;
            bytes = new BytesStore(bytesPageBits);
            // pad: ensure no node gets address 0 which is reserved to mean
            // the stop state w/ no arcs
            bytes.WriteByte((Byte)0);
            NO_OUTPUT = outputs.GetNoOutput();
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

            EmptyOutput = default(T);
            packed = false;
            nodeRefToAddress = null;
        }

        public static readonly int DEFAULT_MAX_BLOCK_BITS = Constants.JRE_IS_64BIT ? 30 : 28;

        /// <summary>
        /// Load a previously saved FST.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="outputs"></param>
        public FST(DataInput input, Outputs<T> outputs)
            : this(input, outputs, DEFAULT_MAX_BLOCK_BITS)
        {
        }

        /// <summary>
        /// Load a previously saved FST; maxBlockBits allows you to
        /// control the size of the byte[] pages used to hold the FST bytes.
        /// </summary>
        /// <param name="dataInput"></param>
        /// <param name="outputs"></param>
        /// <param name="maxBlockBits"></param>
        public FST(DataInput dataInput, Outputs<T> outputs, int maxBlockBits)
        {
            this.outputs = outputs;

            if (maxBlockBits < 1 || maxBlockBits > 30)
                throw new ArgumentException("maxBlockBits should be 1 .. 30; got " + maxBlockBits, "maxBlockBits");

            // NOTE: only reads most recent format; we don't have
            // back-compat promise for FSTs (they are experimental):
            Version = CodecUtil.CheckHeader(dataInput, FILE_FORMAT_NAME, VERSION_PACKED, VERSION_VINT_TARGET);
            packed = dataInput.ReadByte() == 1;

            if (dataInput.ReadByte() == 1)
            {
                // accepts empty string
                // 1 KB blocks:
                var emptyBytes = new BytesStore(10);
                int numBytes = dataInput.ReadVInt();
                emptyBytes.CopyBytes(dataInput, numBytes);

                // De-serialize empty-string output:
                FST.BytesReader reader;
                if (packed)
                    reader = emptyBytes.GetForwardReader();
                else
                {
                    reader = emptyBytes.GetReverseReader();
                    // NoOutputs uses 0 bytes when writing its output,
                    // so we have to check here else BytesStore gets
                    // angry:
                    if (numBytes > 0)
                        reader.Position = numBytes - 1;
                }
                EmptyOutput = Outputs.ReadFinalOutput(reader);
            }
            else
                EmptyOutput = default(T);

            var t = dataInput.ReadByte();
            switch (t)
            {
                case 0: inputType = INPUT_TYPE.BYTE1; break;
                case 1: inputType = INPUT_TYPE.BYTE2; break;
                case 2: inputType = INPUT_TYPE.BYTE4; break;
                default:
                    throw new InvalidOperationException("invalid input type " + t);
            }

            if (packed)
            {
                nodeRefToAddress = PackedInts.GetReader(dataInput);
            }
            else
            {
                nodeRefToAddress = null;
            }

            startNode = dataInput.ReadVLong();
            NodeCount = dataInput.ReadVLong();
            ArcCount = dataInput.ReadVLong();
            ArcWithOutputCount = dataInput.ReadVLong();

            var numBytes2 = dataInput.ReadVLong();
            bytes = new BytesStore(dataInput, numBytes2, 1 << maxBlockBits);

            NO_OUTPUT = outputs.GetNoOutput();

            CacheRootArcs();

            // NOTE: bogus because this is only used during
            // building; we need to break out mutable FST from
            // immutable
            allowArrayArcs = false;
        }

        /// <summary>
        /// Returns bytes used to represent the FST.
        /// </summary>
        /// <returns></returns>
        public long SizeInBytes()
        {
            var size = bytes.GetPosition();
            if (packed)
            {
                size += nodeRefToAddress.RamBytesUsed();
            }
            else if (NodeAddress != null)
            {
                size += NodeAddress.RamBytesUsed();
                size += InCounts.RamBytesUsed();
            }
            return size;
        }

        internal void Finish(long startNode)
        {
            if (startNode != -1)
                throw new InvalidOperationException("already finished");
            if (startNode == FINAL_END_NODE && EmptyOutput != null)
                startNode = 0;

            this.startNode = startNode;
            bytes.Finish();

            CacheRootArcs();
        }

        private long GetNodeAddress(long node)
        {
            if (NodeAddress != null)
                return NodeAddress.Get((int)node);
            return node;
        }

        // Caches first 128 labels
        private void CacheRootArcs()
        {
            cachedRootArcs = new Arc<T>[0x80];
            var arc = new Arc<T>();
            GetFirstArc(arc);
            var inReader = GetBytesReader();
            if (TargetHasArcs(arc))
            {
                ReadFirstRealTargetArc(arc.Target, arc, inReader);
                while (true)
                {
                    // TODO: is assert correct here?
                    Debug.Assert(arc.Label != END_LABEL);
                    if (arc.Label < cachedRootArcs.Length)
                        cachedRootArcs[arc.Label] = new Arc<T>().CopyFrom(arc);
                    else
                        break;

                    if (arc.IsLast()) break;

                    ReadNextRealArc(arc, inReader);
                }
            }
        }

        internal void Save(DataOutput output)
        {
            if (startNode != -1)
                throw new InvalidOperationException("call finish first");
            if (NodeAddress != null)
                throw new InvalidOperationException("cannot save an FST pre-packed FST; it must first be packed");
            if (packed && !(nodeRefToAddress is PackedInts.Mutable))
                throw new InvalidOperationException("cannot save a FST which has been loaded from disk");

            CodecUtil.WriteHeader(output, FILE_FORMAT_NAME, VERSION_CURRENT);

            if (packed)
                output.WriteByte((sbyte)1);
            else
                output.WriteByte((sbyte)0);

            // TODO: really we should encode this as an arc, arriving
            // to the root node, instead of special casing here:
            if (emptyOutput != null)
            {
                // Accepts empty string
                output.WriteByte((sbyte)1);

                // Serialize empty-string output
                var ros = new RAMOutputStream();
                Outputs.WriteFinalOutput(emptyOutput, ros);

                var emptyOutputBytes = new byte[(int)ros.FilePointer];
                ros.WriteTo(emptyOutputBytes, 0);

                if (!packed)
                {
                    // reverse
                    var stopAt = emptyOutputBytes.Length / 2;
                    var upTo = 0;
                    while (upTo < stopAt)
                    {
                        var b = emptyOutputBytes[upTo];
                        emptyOutputBytes[upTo] = emptyOutputBytes[emptyOutputBytes.Length - upTo - 1];
                        emptyOutputBytes[emptyOutputBytes.Length - upTo - 1] = b;
                        upTo++;
                    }
                }
                output.WriteVInt(emptyOutputBytes.Length);
                output.WriteBytes(emptyOutputBytes, 0, emptyOutputBytes.Length);
            }
            else
            {
                output.WriteByte((sbyte)0);
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

            output.WriteByte(t);
            if (packed)
                ((PackedInts.IMutable)nodeRefToAddress).Save(output);

            output.WriteVLong(startNode);
            output.WriteVLong(NodeCount);
            output.WriteVLong(ArcCount);
            output.WriteVLong(ArcWithOutputCount);
            var numBytes = bytes.GetPosition();
            output.WriteVLong(numBytes);
            bytes.WriteTo(output);
        }

        /// <summary>
        /// Writes an automaton to a file
        /// </summary>
        /// <param name="file"></param>
        public void Save(FileInfo fileInfo)
        {
            var success = false;
            var bs = new BufferedStream(new FileStream(fileInfo.FullName));
            try
            {
                Save(new OutputStreamDataOutput(bs));
                success = true;
            }
            finally
            {
                if (success)
                    IOUtils.Close(bs);
                else
                    IOUtils.CloseWhileHandlingException(bs as IDisposable);
            }
        }

        /// <summary>
        /// Reads an automaton from a file
        /// </summary>
        /// <typeparam name="TMethod"></typeparam>
        /// <param name="fileInfo"></param>
        /// <param name="outputs"></param>
        /// <returns></returns>
        public static FST<TMethod> Read<TMethod>(FileStream fileInfo, Outputs<TMethod> outputs) where TMethod : class
        {
            var bs = new BufferedStream(new FileStream(fileInfo));
            var success = false;
            try
            {
                var fst = new FST<TMethod>(new InputStreamDataInput(bs), outputs);
                success = true;
                return fst;
            }
            finally
            {
                if (success)
                    IOUtils.Close(bs);
                else
                    IOUtils.CloseWhileHandlingException(bs as IDisposable);
            }
        }

        private void WriteLabel(DataOutput output, int v)
        {
            if (!(v >= 0)) throw new ArgumentException("v must be greater than or equal to zero. got v=" + v);

            switch (inputType)
            {
                case INPUT_TYPE.BYTE1:
                    if (!(v <= 255)) throw new InvalidOperationException("v=" + v);
                    output.WriteByte((sbyte)v);
                    break;
                case INPUT_TYPE.BYTE2:
                    if (!(v <= 65535)) throw new InvalidOperationException("v=" + v);
                    output.WriteShort((Int16)v);
                    break;
                default:
                    output.WriteVInt(v);
                    break;
            }
        }

        internal int ReadLabel(DataInput input)
        {
            int v;
            switch (inputType)
            {
                case INPUT_TYPE.BYTE1:
                    v = input.ReadByte() & 0xFF;
                    break;
                case INPUT_TYPE.BYTE2:
                    v = input.ReadShort() & 0xFFFF;
                    break;
                default:
                    v = input.ReadVInt();
                    break;
            }
            return v;
        }

        /// <summary>
        /// Returns true if the node at this address
        /// has any outgoing arcs
        /// </summary>
        /// <typeparam name="TMethod"></typeparam>
        /// <param name="arc"></param>
        /// <returns></returns>
        public static bool TargetHasArcs<TMethod>(Arc<TMethod> arc)
        {
            return arc.Target > 0;
        }

        // Serializes new node by appending its bytes to the end
        // of the current byte[]
        internal long AddNode(Builder<T>.UnCompiledNode<T> nodeIn)
        {
            if (nodeIn.NumArcs == 0)
            {
                return nodeIn.IsFinal ? FINAL_END_NODE : NON_FINAL_END_NODE;
            }

            var startAddress = bytes.GetPosition();

            var doFixedArray = ShouldExpand(nodeIn);
            if (doFixedArray)
                if (bytesPerArc.Length < nodeIn.NumArcs)
                    bytesPerArc = new int[ArrayUtil.Oversize(nodeIn.NumArcs, 1)];

            ArcCount += nodeIn.NumArcs;

            var lastArc = nodeIn.NumArcs - 1;

            var lastArcStart = bytes.GetPosition();
            var maxBytesPerArc = 0;
            for (var arcIdx = 0; arcIdx < nodeIn.NumArcs; arcIdx++)
            {
                var arc = nodeIn.Arcs[arcIdx];
                var target = arc.Target as Builder<T>.CompiledNode;
                var flags = 0;

                if (arcIdx == lastArc)
                    flags += BIT_LAST_ARC;

                if (lastFrozenNode == target.Node && !doFixedArray)
                    // TODO: for better perf (but more RAM used) we
                    // could avoid this except when arc is "near" the
                    // last arc:
                    flags += BIT_TARGET_NEXT;

                if (arc.IsFinal)
                {
                    flags += BIT_FINAL_ARC;
                    if ((object)arc.NextFinalOutput != (object)NO_OUTPUT)
                        flags += BIT_ARC_HAS_FINAL_OUTPUT;
                }
                else
                {
                    //Debug.Assert(arc.NextFinalOutput == NO_OUTPUT);
                }

                var targetHasArcs = target.Node > 0;

                if (!targetHasArcs)
                    flags += BIT_STOP_NODE;
                else if (InCounts != null)
                    InCounts.Set((int)target.Node, InCounts.Get((int)target.Node) + 1);

                if ((object)arc.Output != (object)NO_OUTPUT)
                    flags += BIT_ARC_HAS_OUTPUT;

                bytes.WriteByte((sbyte)flags);
                WriteLabel(bytes, arc.Label);

                if ((object)arc.Output != (object)NO_OUTPUT)
                {
                    Outputs.Write(arc.Output, bytes);
                    ArcWithOutputCount++;
                }

                if ((object)arc.NextFinalOutput != (object)NO_OUTPUT)
                {
                    Outputs.WriteFinalOutput(arc.NextFinalOutput, bytes);
                }

                if (targetHasArcs && (flags & BIT_TARGET_NEXT) == 0)
                {
                    // TODO: is assert correct here?
                    Debug.Assert(target.Node > 0);
                    bytes.WriteVLong(target.Node);
                }

                // just write the arcs "like normal" on first pass,
                // but record how many bytes each one took, and max
                // byte size:
                if (doFixedArray)
                {
                    bytesPerArc[arcIdx] = (int)(bytes.GetPosition() - lastArcStart);
                    lastArcStart = bytes.GetPosition();
                    maxBytesPerArc = Math.Max(maxBytesPerArc, bytesPerArc[arcIdx]);
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
                var MAX_HEADER_SIZE = 11;
                // assert maxBytesPerArc > 0;

                // create the header
                // TODO: clean this up: or just rewind+reuse and deal with it
                var header = new byte[MAX_HEADER_SIZE];
                var bad = new ByteArrayDataOutput(header);
                // write a "false" first arc:
                bad.WriteByte(ARCS_AS_FIXED_ARRAY);
                bad.WriteVInt(nodeIn.NumArcs);
                bad.WriteVInt(maxBytesPerArc);
                var headerLen = bad.Position;

                long fixedArrayStart = startAddress + headerLen;

                // expand the arcs in place, backwards
                var srcPos = bytes.GetPosition();
                var destPos = fixedArrayStart + nodeIn.NumArcs * maxBytesPerArc;
                // assert destPos >= srcPos
                if (destPos > srcPos)
                {
                    bytes.SkipBytes((int)(destPos - srcPos));
                    for (var arcIdx = nodeIn.NumArcs - 1; arcIdx >= 0; arcIdx--)
                    {
                        destPos -= maxBytesPerArc;
                        srcPos -= bytesPerArc[arcIdx];
                        if (srcPos != destPos)
                        {
                            if (!(destPos > srcPos))
                                throw new InvalidOperationException("destPos=" + destPos + " srcPos=" + srcPos +
                                                                    " arcIdx=" + arcIdx + " maxBytesPerArc=" +
                                                                    maxBytesPerArc + " bytesPerArc[arcIdx]=" +
                                                                    bytesPerArc[arcIdx] + " nodeIn.numArcs=" +
                                                                    nodeIn.NumArcs);
                            bytes.CopyBytes(srcPos, destPos, bytesPerArc[arcIdx]);
                        }
                    }
                }

                // now write the header
                bytes.WriteBytes(startAddress, (sbyte[])(Array)header, 0, headerLen);
            }

            var thisNodeAddress = bytes.GetPosition() - 1;

            bytes.Reverse(startAddress, thisNodeAddress);

            // PackedInts uses int as the index, so we cannot handle
            // > 2.1B nodes when packing
            if (NodeAddress != null && NodeCount == int.MaxValue)
                throw new InvalidOperationException("cannot create a packed FST with more than 2.1 billion nodes");

            NodeCount++;
            long node;
            if (NodeAddress != null)
            {
                // Nodes are addressed by 1+ord:
                if ((int)NodeCount == NodeAddress.Size())
                {
                    NodeAddress =
                        NodeAddress.Resize(ArrayUtil.Oversize(NodeAddress.Size() + 1, NodeAddress.GetBitsPerValue()));
                    InCounts = InCounts.Resize(ArrayUtil.Oversize(InCounts.Size() + 1, InCounts.GetBitsPerValue()));
                }

                NodeAddress.Set((int)NodeCount, thisNodeAddress);
                node = NodeCount;
            }
            else
            {
                node = thisNodeAddress;
            }
            lastFrozenNode = node;

            return node;
        }

        /// <summary>
        /// Fills virtual 'start' arc, ie, an empty incoming arc
        /// to the FST's start node
        /// </summary>
        /// <param name="arc"></param>
        /// <returns></returns>
        public Arc<T> GetFirstArc(Arc<T> arc)
        {
            if (EmptyOutput != null)
            {
                arc.Flags = (sbyte)(BIT_FINAL_ARC | BIT_LAST_ARC);
                arc.NextFinalOutput = EmptyOutput;
            }
            else
            {
                arc.Flags = (sbyte)BIT_LAST_ARC;
                arc.NextFinalOutput = NO_OUTPUT;
            }
            arc.Output = NO_OUTPUT;

            // If there are no nodes, ie, the FST only accepts the
            // empty string, then startNode is 0
            arc.Target = startNode;
            return arc;
        }

        /// <summary>
        /// Follows the <code>follow</code> arc and reads the last
        /// arc of its target; this changes the provided
        /// <code>arc</code> (2nd arg) in-place and returns it
        /// </summary>
        /// <param name="follow"></param>
        /// <param name="arc"></param>
        /// <param name="input"></param>
        /// <returns>Returns the second argument</returns>
        public Arc<T> ReadLastTargetArc(Arc<T> follow, Arc<T> arc, FST.BytesReader input)
        {
            if (!TargetHasArcs(follow))
            {
                // assert follow.isFinal();
                arc.Label = END_LABEL;
                arc.Target = FINAL_END_NODE;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)BIT_LAST_ARC;
                return arc;
            }
            else
            {
                input.Position = GetNodeAddress(follow.Target);
                arc.Node = follow.Target;
                var b = input.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
                    // array: jump straight to end
                    arc.NumArcs = input.ReadVInt();
                    if (packed || Version >= VERSION_VINT_TARGET)
                        arc.BytesPerArc = input.ReadVInt();
                    else
                        arc.BytesPerArc = input.ReadInt();

                    arc.PosArcsStart = input.Position;
                    arc.ArcIdx = arc.NumArcs - 2;
                }
                else
                {
                    arc.Flags = (sbyte)b;
                    // non-array: linear scan
                    arc.BytesPerArc = 0;

                    while (!arc.IsLast())
                    {
                        // skip this arc:
                        ReadLabel(input);
                        if (arc.Flag(BIT_ARC_HAS_OUTPUT))
                        {
                            Outputs.Read(input);
                        }
                        if (arc.Flag(BIT_ARC_HAS_FINAL_OUTPUT))
                        {
                            Outputs.ReadFinalOutput(input);
                        }
                        if (arc.Flag(BIT_STOP_NODE))
                        {
                        }
                        else if (arc.Flag(BIT_TARGET_NEXT))
                        {
                        }
                        else if (packed)
                            input.ReadVLong();
                        else
                            ReadUnpackedNodeTarget(input);

                        arc.Flags = (sbyte)input.ReadByte();
                    }
                    // Undo the byte flags we read:
                    input.SkipBytes(-1);
                    arc.NextArc = input.Position;
                }

                ReadNextRealArc(arc, input);
                // assert arc.isLast();
                return arc;
            }
        }

        private long ReadUnpackedNodeTarget(FST.BytesReader input)
        {
            long target;
            if (Version < VERSION_VINT_TARGET)
                target = input.ReadInt();
            else
                target = input.ReadVLong();

            return target;
        }

        /// <summary>
        /// Follow the <code>follow</code> arc and read the first arc of its target;
        /// this changes the provided <code>arc</code> (2nd arg) in-place and returns it.
        /// </summary>
        /// <param name="follow"></param>
        /// <param name="arc"></param>
        /// <param name="input"></param>
        /// <returns>Returns the second argument (<code>arc</code>)</returns>
        public Arc<T> ReadFirstTargetArc(Arc<T> follow, Arc<T> arc, FST.BytesReader input)
        {
            // int pos = address;
            if (follow.IsFinal())
            {
                // Insert "fake" final first arc:
                arc.Label = END_LABEL;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte)BIT_FINAL_ARC;
                if (follow.Target <= 0)
                    arc.Flags |= (sbyte)BIT_LAST_ARC;
                else
                {
                    arc.Node = follow.Target;
                    // NOTE: nextArc is a node (not an address!) in this case:
                    arc.NextArc = follow.Target;
                }
                arc.Target = FINAL_END_NODE;
                return arc;
            }
            else
            {
                return ReadFirstRealTargetArc(follow.Target, arc, input);
            }
        }

        public Arc<T> ReadFirstRealTargetArc(long node, Arc<T> arc, FST.BytesReader input)
        {
            var address = GetNodeAddress(node);
            input.Position = address;

            arc.Node = node;

            if (input.ReadByte() == ARCS_AS_FIXED_ARRAY)
            {
                arc.NumArcs = input.ReadVInt();
                if (packed || Version >= VERSION_VINT_TARGET)
                    arc.BytesPerArc = input.ReadVInt();
                else
                    arc.BytesPerArc = input.ReadInt();

                arc.ArcIdx = -1;
                arc.NextArc = arc.PosArcsStart = input.Position;
            }
            else
            {
                arc.NextArc = address;
                arc.BytesPerArc = 0;
            }

            return ReadNextRealArc(arc, input);
        }

        /// <summary>
        /// Checks if <code>arc</code>'s target state is in expanded (or vector) format.
        /// </summary>
        /// <param name="follow"></param>
        /// <param name="input"></param>
        /// <returns>Returns <code>true</code> if <code>arc</code> points to a state 
        /// in an expanded array format.</returns>
        internal bool IsExpandedTarget(Arc<T> follow, FST.BytesReader input)
        {
            if (!TargetHasArcs(follow))
                return false;
            else
            {
                input.Position = GetNodeAddress(follow.Target);
                return input.ReadByte() == ARCS_AS_FIXED_ARRAY;
            }
        }

        /// <summary>
        /// In-place read; returns the arc.
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public Arc<T> ReadNextArc(Arc<T> arc, FST.BytesReader input)
        {
            if (arc.Label == END_LABEL)
            {
                // This was a fake isnerted "final" arc
                if (arc.NextArc <= 0)
                    throw new ArgumentException("cannot readNextArc when arc.isLast()=true");
                return ReadFirstRealTargetArc(arc.NextArc, arc, input);
            }
            else
            {
                return ReadNextRealArc(arc, input);
            }
        }

        /// <summary>
        /// Peeks at next arc's label; does not alter arc. Do
        /// not call this if arc.IsLast()!
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public int ReadNextArcLabel(Arc<T> arc, FST.BytesReader input)
        {
            if (arc.IsLast)
                throw new ArgumentException("cannot readNextArc when arc.isLast()=true");

            if (arc.Label == END_LABEL)
            {
                var pos = GetNodeAddress(arc.NextArc);
                input.Position = pos;

                var b = input.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
                    input.ReadVInt();

                    // Skip bytesPerArc:
                    if (packed || Version >= VERSION_VINT_TARGET)
                        input.ReadVInt();
                    else
                        input.ReadInt();
                }
                else
                {
                    input.Position = pos;
                }
            }
            else
            {
                if (arc.BytesPerArc != 0)
                {
                    // arcs are at fixed entries
                    input.Position = arc.PosArcsStart;
                    input.SkipBytes((1 + arc.ArcIdx) * arc.BytesPerArc);
                }
                else
                {
                    // arcs are packed
                    input.Position = arc.NextArc;
                }
            }

            // skip flags
            input.ReadByte();
            return ReadLabel(input);
        }

        /// <summary>
        /// Never returns null, but you should never call this if
        /// arc.IsLast() is true.
        /// </summary>
        /// <param name="arc"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public Arc<T> ReadNextRealArc(Arc<T> arc, FST.BytesReader input)
        {
            // TODO: can't assert this because we call from readFirstArc
            // assert !flag(arc.flags, BIT_LAST_ARC);

            // this is a continuing arc in a fixed array
            if (arc.BytesPerArc != 0)
            {
                // arcs are at fixed entries
                arc.ArcIdx++;
                // assert arc.arcIdx < arc.numArcs;
                input.Position = arc.PosArcsStart;
                input.SkipBytes(arc.ArcIdx * arc.BytesPerArc);
            }
            else
            {
                // arcs are packed
                input.Position = arc.NextArc;
            }
            arc.Flags = (sbyte)input.ReadByte();
            arc.Label = ReadLabel(input);

            if (arc.Flag(BIT_ARC_HAS_OUTPUT))
            {
                arc.Output = Outputs.Read(input);
            }
            else
            {
                arc.Output = Outputs.GetNoOutput();
            }

            if (arc.Flag(BIT_ARC_HAS_FINAL_OUTPUT))
            {
                arc.NextFinalOutput = Outputs.ReadFinalOutput(input);
            }
            else
            {
                arc.NextFinalOutput = Outputs.GetNoOutput();
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
                arc.NextArc = input.Position;
            }
            else if (arc.Flag(BIT_TARGET_NEXT))
            {
                arc.NextArc = input.Position;

                // TODO: would be nice to make this lazy -- maybe
                // caller doesn't need the target and is scanning arcs...
                if (NodeAddress == null)
                {
                    if (!arc.Flag(BIT_LAST_ARC))
                    {
                        if (arc.BytesPerArc == 0)
                        {
                            // must scan
                            SeekToNextNode(input);
                        }
                        else
                        {
                            input.Position = arc.PosArcsStart;
                            input.SkipBytes(arc.BytesPerArc * arc.NumArcs);
                        }
                    }
                    arc.Target = input.Position;
                }
                else
                {
                    arc.Target = arc.Node - 1;
                    // assert arc.target > 0
                }
            }
            else
            {
                if (packed)
                {
                    var pos = input.Position;
                    var code = input.ReadVLong();
                    if (arc.Flag(BIT_TARGET_DELTA))
                    {
                        // Address is delta-coded from current address:
                        arc.Target = pos + code;
                    }
                    else if (code < nodeRefToAddress.Size())
                    {
                        // Deref
                        arc.Target = nodeRefToAddress.Get((int)code);
                    }
                    else
                    {
                        // Absolute
                        arc.Target = code;
                    }
                }
                else
                {
                    arc.Target = ReadUnpackedNodeTarget(input);
                }
                arc.NextArc = input.Position;
            }
            return arc;
        }

        // TODO: could we somehow [partially] tableize arc lookups
        // look automaton?

        /// <summary>
        /// Finds an arc leaving the incoming arc, replacing the arc in place.
        /// This returns null if the arc was not found, else the incoming arc.
        /// </summary>
        /// <param name="labelToMatch"></param>
        /// <param name="follow"></param>
        /// <param name="arc"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public Arc<T> FindTargetArc(int labelToMatch, Arc<T> follow, Arc<T> arc, FST.BytesReader input)
        {
            if (cachedRootArcs == null)
                throw new InvalidOperationException("cachedRootArcs cannot be null");

            if (labelToMatch == END_LABEL)
            {
                if (follow.IsFinal())
                {
                    if (follow.Target <= 0)
                    {
                        arc.Flags = BIT_LAST_ARC;
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
            if (follow.Target == startNode && labelToMatch < cachedRootArcs.Length)
            {
                var result = cachedRootArcs[labelToMatch];
                if (result == null)
                    return result;
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

            input.Position = GetNodeAddress(follow.Target);

            arc.Node = follow.Target;

            if (input.ReadByte() == ARCS_AS_FIXED_ARRAY)
            {
                // Arcs are full array; do binary search:
                arc.NumArcs = input.ReadVInt();
                if (packed || Version >= VERSION_VINT_TARGET)
                    arc.BytesPerArc = input.ReadVInt();
                else
                    arc.BytesPerArc = input.ReadInt();

                arc.PosArcsStart = input.Position;
                var low = 0;
                var high = arc.NumArcs - 1;
                while (low <= high)
                {
                    var mid = Support.Number.URShift((low + high), 1);
                    input.Position = arc.PosArcsStart;
                    input.SkipBytes(arc.BytesPerArc * mid + 1);
                    var midLabel = ReadLabel(input);
                    var cmp = midLabel - labelToMatch;
                    if (cmp < 0)
                        low = mid + 1;
                    else if (cmp > 0)
                        high = mid - 1;
                    else
                        arc.ArcIdx = mid - 1;
                    return ReadNextRealArc(arc, input);
                }

                return null;
            }

            // Linear scan
            ReadFirstRealTargetArc(follow.Target, arc, input);

            while (true)
            {
                if (arc.Label == labelToMatch)
                    return arc;
                if (arc.Label > labelToMatch)
                    return null;
                if (arc.IsLast())
                    return null;
                ReadNextRealArc(arc, input);
            }
        }

        private void SeekToNextNode(FST.BytesReader input)
        {
            while (true)
            {
                var flags = input.ReadByte();
                ReadLabel(input);

                if (Flag(flags, BIT_ARC_HAS_OUTPUT))
                    Outputs.Read(input);

                if (Flag(flags, BIT_ARC_HAS_FINAL_OUTPUT))
                    Outputs.ReadFinalOutput(input);

                if (!Flag(flags, BIT_STOP_NODE) && !Flag(flags, BIT_TARGET_NEXT))
                    if (packed)
                        input.ReadVLong();
                    else
                        ReadUnpackedNodeTarget(input);

                if (Flag(flags, BIT_LAST_ARC))
                    return;
            }
        }

        public long GetNodeCount()
        {
            // 1+ in order to count the -1 implicit final node
            return 1 + NodeCount;
        }


        /// <summary>
        /// Nodes will be expanded if their depth (distance from the root node) is
        /// &lt;= this value and their number of arcs is &gt=
        /// (FIXED_ARRAY_NUM_ARCS_SHALLOW).
        /// 
        /// Fixed array consumes more RAM but enables binary search on the arcs
        /// (instead of a linear scan) on lookup by arc label.
        /// </summary>
        /// <param name="node"></param>
        /// <returns><code>true</code> if <code>node</code> should be stored
        /// in an expanded (array) form.</returns>
        /// <see cref="FIXED_ARRAY_NUM_ARCS_DEEP"/>
        /// <see cref="Builder.UnCompiledNode"/>
        private bool ShouldExpand(Builder<T>.UnCompiledNode<T> node)
        {
            return allowArrayArcs &&
                   ((node.Depth <= FIXED_ARRAY_SHALLOW_DISTANCE && node.NumArcs >= FIXED_ARRAY_NUM_ARCS_SHALLOW) ||
                    node.NumArcs >= FIXED_ARRAY_NUM_ARCS_DEEP);
        }

        /// <summary>
        /// Returns a BytesReader for this FST, positioned at position 0.
        /// </summary>
        /// <returns></returns>
        public FST.BytesReader GetBytesReader()
        {
            BytesReader input;
            if (packed)
            {
                input = bytes.GetForwardReader();
            }
            else
            {
                input = bytes.GetReverseReader();
            }
            return input;
        }


        /* BytesReader usually goes here */
        /* Now located in non-generic FST class */
        //public abstract class BytesReader : DataInput
        //{
        //    public abstract long GetPosition();

        //    public abstract void SetPosition();

        //    public abstract bool Reversed();

        //    public abstract void SkipBytes(int count);
        //}

        // Creates a packed FST
        private FST(INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            Version = VERSION_CURRENT;
            packed = true;
            this.inputType = inputType;
            bytes = new BytesStore(bytesPageBits);
            this.outputs = outputs;
            NO_OUTPUT = outputs.GetNoOutput();

            allowArrayArcs = false;
        }

        /// <summary>
        /// Expert: creates an FST by packing this one. This
        /// process requires substantial additional RAM (currently
        /// up to ~8 bytes per node depending on 
        /// <code>acceptableOverheadRatio</code>, but then should
        /// produce a smaller FST.
        /// 
        /// The implementation of this method uses ideas from
        /// http://www.cs.put.poznan.pl/dweiss/site/publications/download/fsacomp.pdf
        /// Smaller representation 
        /// which describes this techniques to reduce the size of a FST.
        /// However, this is not a strict implementation of the
        /// algorithms described in this paper.
        /// </summary>
        /// <param name="minInCountDeref"></param>
        /// <param name="maxDerefNodes"></param>
        /// <param name="acceptableOverheadRatio"></param>
        /// <returns></returns>
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
                throw new ArgumentException("this FST was not built with willPackFST=true");

            var arc = new Arc<T>();

            var r = GetBytesReader();

            var topN = Math.Min(maxDerefNodes, InCounts.Size());

            // Find top nodes with highest number of incoming arcs:
            var q = new NodeQueue(topN);

            // TODO: we could use more RAM efficent solution algo here...
            NodeAndInCount bottom = null;
            for (var node = 0; node < InCounts.Size(); node++)
            {
                if (InCounts.Get(node) >= minInCountDeref)
                {
                    if (bottom == null)
                    {
                        q.Add(new NodeAndInCount(node, (int)InCounts.Get(node)));
                        if (q.Size() == topN)
                            bottom = q.Top();
                    }
                    else if (InCounts.Get(node) > bottom.Count)
                    {
                        q.InsertWithOverflow(new NodeAndInCount(node, (int)InCounts.Get(node)));
                    }
                }
            }

            // Free up RAM
            InCounts = null;

            var topNodeMap = new HashMap<long, long>();
            for (var downTo = q.Size() - 1; downTo >= 0; downTo--)
            {
                var n = q.Pop();
                topNodeMap.Add(n.Node, downTo);
            }

            // +1 because node ords start at 1 (0 is reserved as stop node):
            var newNodeAddress = new GrowableWriter(PackedInts.BitsRequired(bytes.GetPosition()),
                                                    (int)(1 + NodeCount), acceptableOverheadRatio);

            // Fill initial coarse guess:
            for (var node = 1; node <= NodeCount; node++)
                newNodeAddress.Set(node, 1 + bytes.GetPosition() - NodeAddress.Get(node));

            int absCount;
            int deltaCount;
            int topCount;
            int nextCount;

            FST<T> fst;

            // Iterate until we converge:
            while (true)
            {
                var changed = false;

                // for assert:
                var negDelta = false;

                fst = new FST<T>(InputType, Outputs, bytes.GetBlockBits());

                var writer = fst.bytes;

                // Skip 0 byte since 0 is reserved target:
                writer.WriteByte((sbyte)0);

                fst.ArcWithOutputCount = 0;
                fst.NodeCount = 0;
                fst.ArcCount = 0;

                absCount = deltaCount = topCount = nextCount = 0;

                var changedCount = 0;

                long addressError = 0;

                // Since we re-reverse the bytes, we now write the
                // nodes backwards, so taht BIT_TARGET_NEXT is
                // unchanged:
                for (var node = (int)NodeCount; node >= 1; node--)
                {
                    fst.NodeCount++;
                    var address = writer.GetPosition();

                    if (address != newNodeAddress.Get(node))
                    {
                        addressError = address - newNodeAddress.Get(node);
                        changed = true;
                        newNodeAddress.Set(node, address);
                        changedCount++;
                    }

                    var nodeArcCount = 0;
                    var bytesPerArc = 0;

                    var retry = false;

                    // for assert:
                    var anyNegDelta = false;

                    // in Java Lucene there is a label 'writeNode:'
                    //writeNode:

                    // Retry loop: possibly iterate more than once, if
                    // this is an array'd node and bytesPerArc changes
                    while (true) // retry writing this node
                    {
                        ReadFirstRealTargetArc(node, arc, r);

                        var useArcArray = arc.BytesPerArc != 0;
                        if (useArcArray)
                        {
                            if (bytesPerArc == 0)
                                bytesPerArc = arc.BytesPerArc;

                            writer.WriteByte(ARCS_AS_FIXED_ARRAY);
                            writer.WriteVInt(arc.NumArcs);
                            writer.WriteVInt(bytesPerArc);
                        }

                        var maxBytesPerArc = 0;

                        while (true) // iterate over all arcs for this node
                        {
                            var arcStartPos = writer.GetPosition();
                            nodeArcCount++;

                            sbyte flags = 0;

                            if (arc.IsLast())
                                flags += (sbyte)BIT_LAST_ARC;

                            if (!useArcArray && node != 1 && arc.Target == node - 1)
                            {
                                flags += (sbyte)BIT_TARGET_NEXT;
                                if (!retry)
                                    nextCount++;
                            }

                            if (arc.IsFinal())
                            {
                                flags += (sbyte)BIT_FINAL_ARC;
                                if ((object)arc.NextFinalOutput != (object)NO_OUTPUT)
                                    flags += (sbyte)BIT_ARC_HAS_FINAL_OUTPUT;
                            }
                            else
                            {
                                // assert arc.nextFinalOutput == NO_OUTPUT;
                            }

                            if (!TargetHasArcs(arc))
                                flags += (sbyte)BIT_STOP_NODE;

                            if ((object)arc.Output != (object)NO_OUTPUT)
                                flags += (sbyte)BIT_ARC_HAS_OUTPUT;

                            long absPtr;
                            var doWriteTarget = TargetHasArcs(arc) && (flags & BIT_TARGET_NEXT) == 0;
                            if (doWriteTarget)
                            {
                                long ptr;
                                var found = topNodeMap.TryGetValue(arc.Target, out ptr);
                                if (found)
                                    absPtr = ptr;
                                else
                                    absPtr = topNodeMap.Count + newNodeAddress.Get((int)arc.Target) + addressError;

                                var delta = newNodeAddress.Get((int)arc.Target) + addressError - writer.GetPosition() - 2;
                                if (delta < 0)
                                {
                                    anyNegDelta = true;
                                    delta = 0;
                                }

                                if (delta < absPtr)
                                {
                                    flags += (sbyte)BIT_TARGET_DELTA;
                                }
                            }
                            else
                            {
                                absPtr = 0;
                            }

                            // assert flags != ARCS_AS_FIXED_ARRAY
                            writer.WriteByte(flags);

                            fst.WriteLabel(writer, arc.Label);

                            if ((object)arc.Output != (object)NO_OUTPUT)
                            {
                                Outputs.Write(arc.Output, writer);
                                if (!retry)
                                    fst.ArcWithOutputCount++;
                            }

                            if ((object)arc.NextFinalOutput != (object)NO_OUTPUT)
                                Outputs.WriteFinalOutput(arc.NextFinalOutput, writer);

                            if (doWriteTarget)
                            {
                                var delta = newNodeAddress.Get((int)arc.Target) + addressError - writer.GetPosition();
                                if (delta < 0)
                                {
                                    anyNegDelta = true;
                                    delta = 0;
                                }

                                if (Flag(flags, BIT_TARGET_DELTA))
                                {
                                    writer.WriteVLong(delta);
                                    if (!retry)
                                        deltaCount++;
                                }
                                else
                                {
                                    writer.WriteVLong(absPtr);
                                    if (!retry)
                                    {
                                        if (absPtr >= topNodeMap.Count)
                                            absCount++;
                                        else
                                            topCount++;
                                    }
                                }
                            }

                            if (useArcArray)
                            {
                                var arcBytes = (int)(writer.GetPosition() - arcStartPos);
                                maxBytesPerArc = Math.Max(maxBytesPerArc, arcBytes);
                                // NOTE: this may in fact go "backwards", if
                                // somehow (rarely, possibly never) we use
                                // more bytesPerArc in this rewrite than the
                                // incoming FST did... but in this case we
                                // will retry (below) so it's OK to ovewrite
                                // bytes:
                                //wasted += bytesPerArc - arcBytes;
                                writer.SkipBytes((int)(arcStartPos + bytesPerArc - writer.GetPosition()));
                            }

                            if (arc.IsLast())
                                break;

                            ReadNextRealArc(arc, r);
                        }

                        if (useArcArray)
                        {
                            if (maxBytesPerArc == bytesPerArc || (retry && maxBytesPerArc <= bytesPerArc))
                            {
                                // converged
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }

                        // Retry:
                        bytesPerArc = maxBytesPerArc;
                        writer.Truncate(address);
                        nodeArcCount = 0;
                        retry = true;
                        anyNegDelta = false;
                    }

                    negDelta |= anyNegDelta;

                    fst.ArcCount += nodeArcCount;
                }

                if (!changed)
                {
                    // We don't renumber the nodes (just reverse their
                    // order) so nodes should only point forward to
                    // other nodes because we only produce acyclic FSTs
                    // w/ nodes only pointing "forwards":
                    // assert != negDelta

                    // Converged!
                    break;
                }
            }

            long maxAddress = 0;
            foreach (var key in topNodeMap.Keys)
                maxAddress = Math.Max(maxAddress, newNodeAddress.Get((int)key));

            var nodeRefToAddressIn = PackedInts.GetMutable(topNodeMap.Count,
                PackedInts.BitsRequired(maxAddress), acceptableOverheadRatio);
            foreach (var pair in topNodeMap)
                nodeRefToAddressIn.Set((int)pair.Value, newNodeAddress.Get((int)pair.Key));

            fst.nodeRefToAddress = nodeRefToAddressIn;

            fst.startNode = newNodeAddress.Get((int)startNode);

            if (emptyOutput != null)
            {
                fst.emptyOutput = emptyOutput;
            }

            //assert fst.nodeCount == nodeCount: "fst.nodeCount=" + fst.nodeCount + " nodeCount=" + nodeCount;
            //assert fst.arcCount == arcCount;
            //assert fst.arcWithOutputCount == arcWithOutputCount: "fst.arcWithOutputCount=" + fst.arcWithOutputCount + " arcWithOutputCount=" + arcWithOutputCount;

            fst.bytes.Finish();
            fst.CacheRootArcs();

            return fst;
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
        /// <see cref="UnCompiledNode"/>
        /// </summary>
        internal const int FIXED_ARRAY_SHALLOW_DISTANCE = 3;

        /// <summary>
        /// <see cref="UnCompiledNode"/>
        /// </summary>
        internal const int FIXED_ARRAY_NUM_ARCS_SHALLOW = 5;

        /// <summary>
        /// <see cref="UnCompiledNode"/>
        /// </summary>
        internal const int FIXED_ARRAY_NUM_ARCS_DEEP = 10;

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
        public sealed class Arc<T>
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

            public bool IsLast()
            {
                return Flag(BIT_LAST_ARC);
            }

            public bool IsFinal()
            {
                return Flag(BIT_FINAL_ARC);
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
