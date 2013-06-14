using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Util.Fst
{
    public class FST<T>
    {
        public enum INPUT_TYPE { BYTE1, BYTE2, BYTE4 }

        private readonly INPUT_TYPE inputType;
        public INPUT_TYPE InputType { get { return inputType; } }

        internal const int BIT_FINAL_ARC = 1 << 0;
        internal const int BIT_LAST_ARC = 1 << 1;
        internal const int BIT_TARGET_NEXT = 1 << 2;
                 
        internal const int BIT_STOP_NODE = 1 << 3;
        internal const int BIT_ARC_HAS_OUTPUT = 1 << 4;
        internal const int BIT_ARC_HAS_FINAL_OUTPUT = 1 << 5;

        private const int BIT_TARGET_DELTA = 1 << 6;

        private const sbyte ARCS_AS_FIXED_ARRAY = BIT_ARC_HAS_FINAL_OUTPUT;

        internal const int FIXED_ARRAY_SHALLOW_DISTANCE = 3;
         
        internal const int FIXED_ARRAY_NUM_ARCS = 5;
         
        internal const int FIXED_ARRAY_NUM_ARCS_DEEP = 10;

        private int[] bytesPerArc = new int[0];

        private const string FILE_FORMAT_NAME = "FST";
        private const int VERSION_START = 0;

        private const int VERSION_INT_NUM_BYTES_PER_ARC = 1;

        private const int VERSION_SHORT_BYTE2_LABELS = 2;

        private const int VERSIONpacked = 3;

        private const int VERSION_VINT_TARGET = 4;

        private const int VERSION_CURRENT = VERSION_VINT_TARGET;

        private const long FINAL_END_NODE = -1;

        private const long NON_FINAL_END_NODE = 0;


        private T emptyOutput;
        public T EmptyOutput
        {
            get { return emptyOutput; }
            set {
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

        private long lastFrozenNode;

        private readonly T NO_OUTPUT;

        internal long NodeCount { get; set; }

        public long ArcCount { get; set; }
        public long ArcWithOutputCount { get; set; }

        private readonly bool packed;
        private PackedInts.IReader nodeRefToAddress;

        public const int END_LABEL = -1;

        private readonly bool allowArrayArcs;
        public bool AllowArrayArcs { get { return allowArrayArcs; } }

        private Arc<T>[] cachedRootArcs;

        public sealed class Arc<T>
        {
            public int Label { get; set; }
            public T Output { get; set; }

            internal long Node { get; set; }

            public long Target { get; set; }

            internal sbyte Flags { get; set; }

            public T NextFinalOutput { get; set; }

            internal long NextArc { get; set; }

            internal long PosArcsStart { get; set; }
            internal int BytesPerArc { get; set; }
            internal int ArcIdx { get; set; }
            internal int NumArcs { get; set; }

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

        private static bool Flag(int flags, int bits)
        {
            return (flags & bits) != 0;
        }

        private GrowableWriter NodeAddress;

        private GrowableWriter InCounts;

        private readonly int Version;

        internal FST(INPUT_TYPE inputType, Outputs<T> outputs, bool willPackFST, float acceptableOverheadRatio,
            bool allowArrayArcs, int bytesPageBits)
        {
            this.inputType = inputType;
            this.outputs = outputs;
            this.allowArrayArcs = allowArrayArcs;
            Version = VERSION_CURRENT;
            bytes = new BytesStore(bytesPageBits);
            bytes.WriteByte((Byte) 0);
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

        public const int DEFAULT_MAX_BLOCK_BITS = Constants.JRE_IS_64BIT ? 30 : 28;

        public FST(DataInput dataInput, Outputs<T> outputs, int maxBlockBits)
        {
            this.outputs = outputs;

            if (maxBlockBits < 1 || maxBlockBits > 30) throw new ArgumentException("maxBlockBits should be 1 .. 30; got " + maxBlockBits, "maxBlockBits");
            Version = CodecUtil.CheckHeader(dataInput, FILE_FORMAT_NAME, VERSIONpacked, VERSION_VINT_TARGET);
            packed = dataInput.ReadByte() == 1;

            if (dataInput.ReadByte() == 1)
            {
                var emptyBytes = new BytesStore(10);
                int numBytes = dataInput.ReadVInt();
                emptyBytes.CopyBytes(dataInput, numBytes);

                FST.BytesReader reader;
                if (packed)
                    reader = emptyBytes.GetForwardReader();
                else
                {
                    reader = emptyBytes.GetReverseReader();
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

            allowArrayArcs = false;
        }

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
                return NodeAddress.Get((int) node);
            return node;
        }

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

        private void Save(DataOutput output)
        {
            if (startNode != -1)
                throw new InvalidOperationException("call finish first");
            if (NodeAddress != null)
                throw new InvalidOperationException("cannot save an FST pre-packed FST; it must first be packed");
            if (packed && !(nodeRefToAddress is PackedInts.Mutable))
                throw new InvalidOperationException("cannot save a FST which has been loaded from disk");

            CodecUtil.WriteHeader(output, FILE_FORMAT_NAME, VERSION_CURRENT);

            if (packed)
                output.WriteByte((sbyte) 1);
            else
                output.WriteByte((sbyte) 0);

            if (emptyOutput != null)
            {
                // Accepts empty string
                output.WriteByte((sbyte) 1);

                // Serialize empty-string output
                var ros = new RAMOutputStream();
                Outputs.WriteFinalOutput(emptyOutput, ros);

                var emptyOutputBytes = new sbyte[(int) ros.FilePointer];
                ros.WriteTo(emptyOutputBytes, 0);

                if (!packed)
                {
                    // reverse
                    var stopAt = emptyOutputBytes.Length/2;
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
                output.WriteByte((sbyte) 0);
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
                ((PackedInts.IMutable) nodeRefToAddress).Save(output);

            output.WriteVLong(startNode);
            output.WriteVLong(NodeCount);
            output.WriteVLong(ArcCount);
            output.WriteVLong(ArcWithOutputCount);
            var numBytes = bytes.GetPosition();
            output.WriteVLong(numBytes);
            bytes.WriteTo(output);
        }

        public void Save(FileStream fileStream)
        {
            var success = false;
            var bs = new BufferedStream(fileStream);
            try
            {
                Save(new OutputStreamDataOuput(bs));
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

        public static FST<TMethod> Read<TMethod>(FileStream fileStream, Outputs<T> outputs) where TMethod : class
        {
            var bs = new BufferedStream(fileStream);
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
                    output.WriteByte((sbyte) v);
                    break;
                case INPUT_TYPE.BYTE2:
                    if (!(v <= 65535)) throw new InvalidOperationException("v=" + v);
                    output.WriteShort((Int16) v);
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

        public static bool TargetHasArcs<TMethod>(Arc<TMethod> arc)
        {
            return arc.Target > 0;
        }

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
                    flags += BIT_TARGET_NEXT;

                if (arc.IsFinal)
                {
                    flags += BIT_FINAL_ARC;
                    if (arc.NextFinalOutput != NO_OUTPUT)
                        flags += BIT_ARC_HAS_FINAL_OUTPUT;
                }
                else
                {
                    // TODO: Assert is correct here?
                    Debug.Assert(arc.NextFinalOutput == NO_OUTPUT);
                }

                var targetHasArcs = target.Node > 0;

                if (!targetHasArcs)
                    flags += BIT_STOP_NODE;
                else if (InCounts != null)
                    InCounts.Set((int) target.Node, InCounts.Get((int) target.Node) + 1);

                if (arc.Output != NO_OUTPUT)
                    flags += BIT_ARC_HAS_OUTPUT;

                bytes.WriteByte((sbyte) flags);
                WriteLabel(bytes, arc.Label);

                if (arc.Output != NO_OUTPUT)
                {
                    Outputs.Write(arc.Output, bytes);
                    ArcWithOutputCount++;
                }

                if (arc.NextFinalOutput != NO_OUTPUT)
                {
                    Outputs.WriteFinalOutput(arc.NextFinalOutput, bytes);
                }

                if (targetHasArcs && (flags & BIT_TARGET_NEXT) == 0)
                {
                    // TODO: is assert correct here?
                    Debug.Assert(target.Node > 0);
                    bytes.WriteVLong(target.Node);
                }

                if (doFixedArray)
                {
                    bytesPerArc[arcIdx] = (int) (bytes.GetPosition() - lastArcStart);
                    lastArcStart = bytes.GetPosition();
                    maxBytesPerArc = Math.Max(maxBytesPerArc, bytesPerArc[arcIdx]);
                }
            }

            if (doFixedArray)
            {
                var MAX_HEADER_SIZE = 11;
                // TODO: assert correct here?
                Debug.Assert(maxBytesPerArc > 0);

                var header = new sbyte[MAX_HEADER_SIZE];
                var bad = new ByteArrayDataOutput(header);
                bad.WriteByte(ARCS_AS_FIXED_ARRAY);
                bad.WriteVInt(nodeIn.NumArcs);
                bad.WriteVInt(maxBytesPerArc);
                var headerLen = bad.GetPosition();

                long fixedArrayStart = startAddress + headerLen;

                var srcPos = bytes.GetPosition();
                var destPos = fixedArrayStart + nodeIn.NumArcs*maxBytesPerArc;
                // TODO: assert correct here?
                Debug.Assert(destPos >= srcPos);
                if (destPos > srcPos)
                {
                    bytes.SkipBytes((int) (destPos - srcPos));
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
                bytes.WriteBytes(startAddress, header, 0, headerLen);
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
                if ((int) NodeCount == NodeAddress.Size)
                {
                    NodeAddress =
                        NodeAddress.Resize(ArrayUtil.Oversize(NodeAddress.Size + 1, NodeAddress.GetBitsPerValue()));
                    InCounts = InCounts.Resize(ArrayUtil.Oversize(InCounts.Size + 1, InCounts.GetBitsPerValue()));
                }

                NodeAddress.Set((int) NodeCount, thisNodeAddress);
                node = NodeCount;
            }
            else
            {
                node = thisNodeAddress;
            }
            lastFrozenNode = node;

            return node;
        }

        public Arc<T> GetFirstArc(Arc<T> arc)
        {
            if (EmptyOutput != null)
            {
                arc.Flags = (sbyte) (BIT_FINAL_ARC | BIT_LAST_ARC);
                arc.NextFinalOutput = EmptyOutput;
            }
            else
            {
                arc.Flags = (sbyte) BIT_LAST_ARC;
                arc.NextFinalOutput = NO_OUTPUT;
            }
            arc.Output = NO_OUTPUT;

            return arc;
        }

        public Arc<T> ReadLastTargetArc(Arc<T> follow, Arc<T> arc, FST.BytesReader input)
        {
            if (!TargetHasArcs(follow))
            {
                // TODO: assert correct here?
                Debug.Assert(follow.IsFinal());
                arc.Label = END_LABEL;
                arc.Target = FINAL_END_NODE;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte) BIT_LAST_ARC;
                return arc;
            }
            else
            {
                input.Position = GetNodeAddress(follow.Target);
                arc.Node = follow.Target;
                var b = input.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
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
                    arc.Flags = b;
                    arc.BytesPerArc = 0;

                    while (!arc.IsLast())
                    {
                        ReadLabel(input);
                        if (arc.Flag(BIT_ARC_HAS_OUTPUT))
                            Outputs.Read(input);
                        if (arc.Flag(BIT_ARC_HAS_FINAL_OUTPUT))
                            Outputs.ReadFinalOutput(input);
                        if (arc.Flag(BIT_STOP_NODE)) {
                        } else if (arc.Flag(BIT_TARGET_NEXT)) {
                        } else if (packed)
                            input.ReadVLong();
                        else
                            ReadUnpackedNodeTarget(input);

                        arc.Flags = (sbyte)input.ReadByte();
                    }

                    input.SkipBytes(-1);
                    arc.NextArc = input.Position;
                }

                ReadNextRealArc(arc, input);
                // TODO: assert is correct here?
                //Debug.Assert(arc.IsLast());
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

        public Arc<T> ReadFirstTargetArc(Arc<T> follow, Arc<T> arc, FST.BytesReader input)
        {
            if (follow.IsFinal())
            {
                arc.Label = END_LABEL;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (sbyte) BIT_FINAL_ARC;
                if (follow.Target <= 0)
                    arc.Flags |= (sbyte) BIT_LAST_ARC;
                else
                {
                    arc.Node = follow.Target;
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

        public Arc<T> ReadNextArc(Arc<T> arc, FST.BytesReader input)
        {
            if (arc.Label == END_LABEL)
            {
                if (arc.NextArc <= 0)
                    throw new ArgumentException("cannot readNextArc when arc.isLast()=true");
                return ReadFirstRealTargetArc(arc.NextArc, arc, input);
            }
            else
            {
                return ReadNextRealArc(arc, input);
            }
        }

        public int ReadNextArcLabel(Arc<T> arc, FST.BytesReader input)
        {
            // TODO: assert correct here?
            Debug.Assert(!arc.IsLast());

            if (arc.Label == END_LABEL)
            {
                var pos = GetNodeAddress(arc.NextArc);
                input.Position = pos;

                var b = input.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
                    input.ReadVInt();

                    if (packed || Version >= VERSION_VINT_TARGET)
                        input.ReadVInt();
                    else
                        input.ReadInt();
                }
                else
                {
                    input.Position = arc.NextArc;
                }
            }

            input.ReadByte();
            return ReadLabel(input);
        }

        public Arc<T> ReadNextRealArc(Arc<T> arc, FST.BytesReader input)
        {
            if (arc.BytesPerArc != 0)
            {
                arc.ArcIdx++;
                // TODO: assert correct here?
                Debug.Assert(arc.ArcIdx < arc.NumArcs);
                input.Position = arc.PosArcsStart;
                input.SkipBytes(arc.ArcIdx*arc.BytesPerArc);
            }
            else
            {
                input.Position = arc.NextArc;
            }
            arc.Flags = (sbyte)input.ReadByte();
            arc.Label = ReadLabel(input);

            if (arc.Flag(BIT_ARC_HAS_OUTPUT))
                arc.Output = Outputs.Read(input);
            else
                arc.Output = Outputs.GetNoOutput();

            if (arc.Flag(BIT_ARC_HAS_FINAL_OUTPUT))
                arc.NextFinalOutput = Outputs.ReadFinalOutput(input);
            else
                arc.NextFinalOutput = Outputs.GetNoOutput();

            if (arc.Flag(BIT_STOP_NODE))
            {
                if (arc.Flag(BIT_FINAL_ARC))
                    arc.Target = FINAL_END_NODE;
                else
                    arc.Target = NON_FINAL_END_NODE;

                arc.NextArc = input.Position;
            }
            else if (arc.Flag(BIT_TARGET_NEXT))
            {
                arc.NextArc = input.Position;

                if (NodeAddress == null)
                {
                    if (!arc.Flag(BIT_LAST_ARC))
                    {
                        if (arc.BytesPerArc == 0)
                        {
                            SeekToNextNode(input);
                        }
                        else
                        {
                            input.Position = arc.PosArcsStart;
                            input.SkipBytes(arc.BytesPerArc*arc.NumArcs);
                        }
                    }
                    arc.Target = input.Position;
                }
                else
                {
                    arc.Target = arc.Node - 1;
                    // TODO: assert correct here?
                    Debug.Assert(arc.Target > 0);
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
                        arc.Target = pos + code;
                    }
                    else if (code < nodeRefToAddress.Size)
                    {
                        arc.Target = nodeRefToAddress.Get((int) code);
                    }
                    else
                    {
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

        public Arc<T> FindTargetArc(int labelToMatch, Arc<T> follow, Arc<T> arc, FST.BytesReader input)
        {
            // TODO: appropriate error message
            if (cachedRootArcs == null) throw new InvalidOperationException("cachedRootArcs cannot be null");

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
                return null;

            input.Position = GetNodeAddress(follow.Target);

            arc.Node = follow.Target;

            if (input.ReadByte() == ARCS_AS_FIXED_ARRAY)
            {
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
                    input.SkipBytes(arc.BytesPerArc*mid + 1);
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

        private bool ShouldExpand(Builder<T>.UnCompiledNode<T> node)
        {
            return allowArrayArcs &&
                   ((node.Depth <= FIXED_ARRAY_SHALLOW_DISTANCE && node.NumArcs >= FIXED_ARRAY_NUM_ARCS_SHALLOW) ||
                    node.NumArcs >= FIXED_ARRAY_NUM_ARCS_DEEP);
        }

        public FST.BytesReader GetBytesReader()
        {
            return packed ?
                bytes.GetForwardReader() :
                bytes.GetReverseReader();
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

        private class ArcAndState<T>
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

        private FST(INPUT_TYPE inputType, Outputs<T> outputs, int bytesPageBits)
        {
            Version = VERSION_CURRENT;
            packed = true;
            inputType = inputType;
            bytes = new BytesStore(bytesPageBits);
            outputs = outputs;
            NO_OUTPUT = outputs.GetNoOutput();

            AllowArrayArcs = false;
        }

        internal FST<T> Pack(int minInCountDeref, int maxDerefNodes, float acceptableOverheadRatio)
        {
            if (NodeAddress == null) throw new ArgumentException("this FST was not built with willPackFST=true");

            var arc = new Arc<T>();

            var r = GetBytesReader();

            var topN = Math.Min(maxDerefNodes, InCounts.Size);

            var q = new NodeQueue(topN);

            NodeAndInCount bottom = null;
            for (var node = 0; node < InCounts.Size; node++)
            {
                if (InCounts.Get(node) >= minInCountDeref)
                {
                    if (bottom == null)
                    {
                        q.Add(new NodeAndInCount(node, (int) InCounts.Get(node)));
                        if (q.Size == topN)
                            bottom = q.Top();
                    }
                    else if (InCounts.Get(node) > bottom.Count)
                    {
                        q.InsertWithOverflow(new NodeAndInCount(node, (int) InCounts.Get(node)));
                    }
                }
            }

            // Free up RAM
            InCounts = null;
            var topNodeMap = new HashMap<long, long>();
            for (var downTo = q.Size - 1; downTo >= 0; downTo--)
            {
                var n = q.Pop();
                topNodeMap.Add(n.Node, downTo);
            }

            var newNodeAddress = new GrowableWriter(PackedInts.BitsRequired(bytes.GetPosition()),
                                                    (int) (1 + NodeCount), acceptableOverheadRatio);

            for (var node = 1; node <= NodeCount; node++)
                newNodeAddress.Set(node, 1 + bytes.GetPosition() - NodeAddress.Get(node));

            int absCount;
            int deltaCount;
            int topCount;
            int nextCount;

            FST<T> fst;

            while (true)
            {
                var changed = false;

                var negDelta = false;

                fst = new FST<T>(InputType, Outputs, bytes.GetBlockBits());

                var writer = fst.bytes;

                writer.WriteByte((sbyte) 0);

                fst.ArcWithOutputCount = 0;
                fst.NodeCount = 0;
                fst.ArcCount = 0;

                absCount = deltaCount = topCount = nextCount = 0;

                var changedCount = 0;

                long addressError = 0;

                for (var node = (int) NodeCount; node >= 1; node--)
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

                    var anyNegDelta = false;

                    // in Java Lucene there is a label 'writeNode:'
                    // writeNode:
                    while (true)
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
                                flags += (sbyte) BIT_LAST_ARC;

                            if (!useArcArray && node != 1 && arc.Target == node - 1)
                            {
                                flags += (sbyte) BIT_TARGET_NEXT;
                                if (!retry)
                                    nextCount++;
                            }

                            if (arc.IsFinal())
                            {
                                flags += (sbyte) BIT_FINAL_ARC;
                                if (arc.NextFinalOutput != NO_OUTPUT)
                                    flags += (sbyte) BIT_ARC_HAS_FINAL_OUTPUT;
                            }
                            else
                            {
                                // TODO: assert is correct here?
                                Debug.Assert(arc.NextFinalOutput == NO_OUTPUT);
                            }

                            if (!TargetHasArcs(arc))
                                flags += (sbyte) BIT_STOP_NODE;

                            if (arc.Output != NO_OUTPUT)
                                flags += (sbyte) BIT_ARC_HAS_OUTPUT;

                            long absPtr;
                            var doWriteTarget = TargetHasArcs(arc) && (flags & BIT_TARGET_NEXT) == 0;
                            if (doWriteTarget)
                            {
                                long ptr;
                                var found = topNodeMap.TryGetValue(arc.Target, out ptr);
                                if (found)
                                    absPtr = ptr;
                                else
                                    absPtr = topNodeMap.Count + newNodeAddress.Get((int) arc.Target) + addressError;

                                var delta = newNodeAddress.Get((int) arc.Target) + addressError - writer.GetPosition() -
                                            2;
                                if (delta < 0)
                                {
                                    anyNegDelta = true;
                                    delta = 0;
                                }

                                if (delta < absPtr)
                                    flags += (sbyte) BIT_TARGET_DELTA;
                            }
                            else
                            {
                                absPtr = 0;
                            }

                            // TODO: assert correct here?
                            Debug.Assert(flags != ARCS_AS_FIXED_ARRAY);
                            writer.WriteByte(flags);

                            fst.WriteLabel(writer, arc.Label);

                            if (arc.Output != NO_OUTPUT)
                            {
                                Outputs.Write(arc.Output, writer);
                                if (!retry)
                                    fst.ArcWithOutputCount++;
                            }

                            if (arc.NextFinalOutput != NO_OUTPUT)
                                Outputs.WriteFinalOutput(arc.NextFinalOutput, writer);

                            if (doWriteTarget)
                            {
                                var delta = newNodeAddress.Get((int) arc.Target) + addressError - writer.GetPosition();
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
                                var arcBytes = (int) (writer.GetPosition() - arcStartPos);
                                maxBytesPerArc = Math.Max(maxBytesPerArc, arcBytes);
                                writer.SkipBytes((int) (arcStartPos + bytesPerArc - writer.GetPosition()));
                            }

                            if (arc.IsLast())
                                break;

                            ReadNextRealArc(arc, r);
                        }

                        if (useArcArray)
                        {
                            if (maxBytesPerArc == bytesPerArc || (retry && maxBytesPerArc <= bytesPerArc))
                                break;
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
                    // TODO: assert correct here?
                    Debug.Assert(!negDelta);
                    break;
                }
            }

            long maxAddress = 0;
            foreach (var key in topNodeMap.Keys)
                maxAddress = Math.Max(maxAddress, newNodeAddress.Get((int) key));

            var nodeRefToAddressIn = PackedInts.GetMutable(topNodeMap.Count,
                PackedInts.BitsRequired(maxAddress), acceptableOverheadRatio);
            foreach (var pair in topNodeMap)
                nodeRefToAddressIn.Set(pair.Value, newNodeAddress.Get(pair.Key));

            fst.nodeRefToAddress = nodeRefToAddressIn;

            fst.startNode = newNodeAddress.Get((int) startNode);

            if (emptyOutput != null)
                fst.emptyOutput = emptyOutput;

            // TODO: assert correct here?
            Debug.Assert(fst.NodeCount == NodeCount, "fst.NodeCount=" + fst.NodeCount + " NodeCount=" + NodeCount);
            Debug.Assert(fst.ArcCount == ArcCount);
            Debug.Assert(fst.ArcWithOutputCount == ArcWithOutputCount, "fst.ArcWithOutputCount=" + fst.ArcWithOutputCount + " ArcWithOutputCount=" + ArcWithOutputCount);

            fst.bytes.Finish();
            fst.CacheRootArcs();

            return fst;
        }

        private class NodeAndInCount : IComparable<NodeAndInCount>
        {
            private readonly int _node;
            public int Node { get { return _node; }}

            private readonly int _count;
            public int Count { get { return _count; } }

            public NodeAndInCount(int node, int count)
            {
                _node = node;
                _count = count;
            }

            public int CompareTo(NodeAndInCount other)
            {
                if (Count > other.Count) return 1;
                if (Count < other.Count) return -1;
                return other.Node - Node;
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
                var cmp = a.CompareTo(b);
                // TODO: assert correct here?
                Debug.Assert(cmp != 0);
                return cmp < 0;
            }
        }
    }


    public static class FST
    {
        public abstract class BytesReader : DataInput
        {
            public abstract long Position { get; set; }

            public abstract bool Reversed();

            public abstract void SkipBytes(int count);
        }
    }
}
