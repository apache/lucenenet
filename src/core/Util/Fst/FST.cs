using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Single = System.Single;

namespace Lucene.Net.Util.Fst
{
    public class FST<T> where T : class
    {
        public enum INPUT_TYPE { BYTE1, BYTE2, BYTE4 }

        private readonly INPUT_TYPE _inputType;
        public INPUT_TYPE InputType { get { return _inputType; } }

        static readonly Int32 BIT_FINAL_ARC = 1 << 0;
        static readonly Int32 BIT_LAST_ARC = 1 << 1;
        static readonly Int32 BIT_TARGET_NEXT = 1 << 2;

        static readonly Int32 BIT_STOP_NODE = 1 << 3;
        static readonly Int32 BIT_ARC_HAS_OUTPUT = 1 << 4;
        static readonly Int32 BIT_ARC_HAS_FINAL_OUTPUT = 1 << 5;

        private static readonly Int32 BIT_TARGET_DELTA = 1 << 6;

        private static readonly SByte ARCS_AS_FIXED_ARRAY = (SByte)BIT_ARC_HAS_FINAL_OUTPUT;

        static readonly Int32 FIXED_ARRAY_SHALLOW_DISTANCE = 3;

        static readonly Int32 FIXED_ARRAY_NUM_ARCS = 5;

        static readonly Int32 FIXED_ARRAY_NUM_ARCS_DEEP = 10;

        private Int32[] _bytesPerArc = new Int32[0];

        private static readonly String FILE_FORMAT_NAME = "FST";
        private static readonly Int32 VERSION_START = 0;

        private static readonly Int32 VERSION_INT_NUM_BYTES_PER_ARC = 1;

        private static readonly Int32 VERSION_SHORT_BYTE2_LABELS = 2;

        private static readonly Int32 VERSION_PACKED = 3;

        private static readonly Int32 VERSION_VINT_TARGET = 4;

        private static readonly Int32 VERSION_CURRENT = VERSION_VINT_TARGET;

        private static readonly Int64 FINAL_END_NODE = -1;

        private static readonly Int64 NON_FINAL_END_NODE = 0;


        private T _emptyOutput;
        public T EmptyOutput
        {
            get { return _emptyOutput; }
            set {
                _emptyOutput = 
                    _emptyOutput != null ? 
                    Outputs.Merge(_emptyOutput, value) :
                    value;
            }
        }

        private readonly BytesStore _bytes;
        public BytesStore Bytes { get { return _bytes; } }

        private Int64 _startNode = -1;

        private readonly Outputs<T> _outputs;
        public Outputs<T> Outputs { get { return _outputs; } }

        private Int64 _lastFrozenNode;

        private readonly T NO_OUTPUT;

        internal Int64 NodeCount { get; set; }

        public Int64 ArcCount { get; set; }
        public Int64 ArcWithOutputCount { get; set; }

        private readonly Boolean _packed;
        private PackedInts.Reader _nodeRefToAddress;

        public static readonly Int32 END_LABEL = -1;

        private readonly Boolean _allowArrayArcs;
        public Boolean AllowArrayArcs { get { return _allowArrayArcs; } }

        private Arc<T>[] _cachedRootArcs;

        public sealed class Arc<T>
        {
            public Int32 Label { get; set; }
            public T Output { get; set; }

            internal Int64 Node { get; set; };

            public Int64 Target { get; set; }

            internal SByte Flags { get; set; };

            public T NextFinalOutput { get; set; }

            internal Int64 NextArc { get; set; }

            internal Int64 PosArcsStart { get; set; }
            internal Int32 BytesPerArc { get; set; }
            internal Int32 ArcIdx { get; set; }
            internal Int32 NumArcs { get; set; }

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

            internal Boolean Flag(Int32 flag)
            {
                return Fst.Flag(Flags, flag);
            }

            public Boolean IsLast()
            {
                return Flag(BIT_LAST_ARC);
            }

            public Boolean IsFinal()
            {
                return Flag(BIT_FINAL_ARC);
            }

            public override String ToString()
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

        private static Boolean Flag(Int32 flags, Int32 bits)
        {
            return (flags & bits) != 0;
        }

        private GrowableWriter NodeAddress;

        private GrowableWriter InCounts;

        private readonly Int32 Version;

        FST(INPUT_TYPE inputType, Outputs<T> outputs, Boolean willPackFST, Single acceptableOverheadRatio,
            Boolean allowArrayArcs, Int32 bytesPageBits)
        {
            _inputType = inputType;
            _outputs = outputs;
            _allowArrayArcs = allowArrayArcs;
            Version = VERSION_CURRENT;
            _bytes = new BytesStore(bytesPageBits);
            _bytes.WriteByte((Byte) 0);
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

            EmptyOutput = null;
            _packed = false;
            _nodeRefToAddress = null;
        }

        public static readonly Int32 DEFAULT_MAX_BLOCK_BITS = Constants.JRE_IS_64BIT ? 30 : 28;

        public FST(DataInput dataInput, Outputs<T> outputs, Int32 maxBlockBits)
        {
            _outputs = outputs;

            if (maxBlockBits < 1 || maxBlockBits > 30) throw new ArgumentException("maxBlockBits should be 1 .. 30; got " + maxBlockBits, "maxBlockBits");
            Version = CodecUtil.CheckHeader(dataInput, FILE_FORMAT_NAME, VERSION_PACKED, VERSION_VINT_TARGET);
            _packed = dataInput.ReadByte() == 1;

            if (dataInput.ReadByte() == 1)
            {
                var emptyBytes = new BytesStore(10);
                Int32 numBytes = dataInput.ReadVInt();
                emptyBytes.CopyBytes(dataInput, numBytes);

                BytesReader reader;
                if (_packed)
                    reader = emptyBytes.GetForwardReader();
                else
                {
                    reader = emptyBytes.GetReverseReader();
                    if (numBytes > 0)
                        reader.SetPosition(numBytes - 1);
                }
                EmptyOutput = Outputs.ReadFinalOutput(reader);
            }
            else
                EmptyOutput = null;

            var t = dataInput.ReadByte();
            switch (t)
            {
                case 0: _inputType = INPUT_TYPE.BYTE1; break;
                case 1: _inputType = INPUT_TYPE.BYTE2; break;
                case 2: _inputType = INPUT_TYPE.BYTE4; break;
                default:
                    throw new InvalidOperationException("invalid input type " + t);
            }

            if (_packed)
                _nodeRefToAddress = PackedInts.getReader(dataInput);
            else
                _nodeRefToAddress = null;

            _startNode = dataInput.ReadVLong();
            NodeCount = dataInput.ReadVLong();
            ArcCount = dataInput.ReadVLong();
            ArcWithOutputCount = dataInput.ReadVLong();

            var numBytes = dataInput.ReadVLong();
            _bytes = new BytesStore(dataInput, numBytes, 1 << maxBlockBits);

            NO_OUTPUT = outputs.GetNoOutput();

            CacheRootArcs();

            _allowArrayArcs = false;
        }

        public Int64 SizeInBytes()
        {
            var size = _bytes.GetPosition();
            if (_packed)
            {
                size += _nodeRefToAddress.RamBytesUsed();
            }
            else if (NodeAddress != null)
            {
                size += NodeAddress.RamBytesUsed();
                size += InCounts.RamBytesUsed();
            }
            return size;
        }

        internal void Finish(Int64 startNode)
        {
            if (_startNode != -1)
                throw new InvalidOperationException("already finished");
            if (startNode == FINAL_END_NODE && EmptyOutput != null)
                startNode = 0;

            _startNode = startNode;
            _bytes.Finish();

            CacheRootArcs();
        }

        private Int64 GetNodeAddress(Int64 node)
        {
            if (NodeAddress != null)
                return NodeAddress.Get((Int32) node);
            return node;
        }

        private void CacheRootArcs()
        {
            _cachedRootArcs = new Arc<T>[0x80];
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
                    if (arc.Label < _cachedRootArcs.Length)
                        _cachedRootArcs[arc.Label] = new Arc<T>().CopyFrom(arc);
                    else
                        break;

                    if (arc.IsLast()) break;

                    ReadNextRealArc(arc, inReader);
                }
            }
        }

        private void Save(DataOutput output)
        {
            if (_startNode != -1)
                throw new InvalidOperationException("call finish first");
            if (NodeAddress != null)
                throw new InvalidOperationException("cannot save an FST pre-packed FST; it must first be packed");
            if (_packed && !(_nodeRefToAddress is PackedInts.Mutable))
                throw new InvalidOperationException("cannot save a FST which has been loaded from disk");

            CodecUtil.WriteHeader(output, FILE_FORMAT_NAME, VERSION_CURRENT);

            if (_packed)
                output.WriteByte((SByte) 1);
            else
                output.WriteByte((SByte) 0);

            if (_emptyOutput != null)
            {
                // Accepts empty string
                output.WriteByte((SByte) 1);

                // Serialize empty-string output
                var ros = new RAMOutputStream();
                Outputs.WriteFinalOutput(_emptyOutput, ros);

                var emptyOutputBytes = new SByte[(Int32) ros.GetFilePointer()];
                Outputs.WriteFinalOutput(emptyOutputBytes, 0);

                if (!_packed)
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
                output.WriteByte((SByte) 0);
            }

            SByte t;
            if (_inputType == INPUT_TYPE.BYTE1) t = 0;
            else if (_inputType == INPUT_TYPE.BYTE2) t = 1;
            else t = 2;

            output.WriteByte(t);
            if (_packed)
                ((PackedInts.Mutable) _nodeRefToAddress).Save(output);

            output.WriteVLong(_startNode);
            output.WriteVLong(NodeCount);
            output.WriteVLong(ArcCount);
            output.WriteVLong(ArcWithOutputCount);
            var numBytes = _bytes.GetPosition();
            output.WriteVLong(numBytes);
            _bytes.WriteTo(output);
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

        private void WriteLabel(DataOutput output, Int32 v)
        {
            if (!(v >= 0)) throw new ArgumentException("v must be greater than or equal to zero. got v=" + v);
            
            switch (_inputType)
            {
                case INPUT_TYPE.BYTE1:
                    if (!(v <= 255)) throw new InvalidOperationException("v=" + v);
                    output.WriteByte((SByte) v);
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

        private Int32 ReadLabel(DataInput input)
        {
            Int32 v;
            switch (_inputType)
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

        public static Boolean TargetHasArcs<TMethod>(Arc<TMethod> arc) where TMethod : class
        {
            return arc.Target > 0;
        }

        internal Int64 AddNode(Builder<T>.UnCompiledNode<T> nodeIn)
        {
            if (nodeIn.NumArcs == 0)
            {
                return nodeIn.IsFinal ? FINAL_END_NODE : NON_FINAL_END_NODE;
            }

            var startAddress = _bytes.GetPosition();

            var doFixedArray = ShouldExpand(nodeIn);
            if (doFixedArray)
                if (_bytesPerArc.Length < nodeIn.NumArcs)
                    _bytesPerArc = new Int32[ArrayUtil.Oversize(nodeIn.NumArcs, 1)];

            ArcCount += nodeIn.NumArcs;

            var lastArc = nodeIn.NumArcs - 1;

            var lastArcStart = _bytes.GetPosition();
            var maxBytesPerArc = 0;
            for (var arcIdx = 0; arcIdx < nodeIn.NumArcs; arcIdx++)
            {
                var arc = nodeIn.Arcs[arcIdx];
                var target = arc.Target as Builder<T>.CompiledNode;
                var flags = 0;

                if (arcIdx == lastArc)
                    flags += BIT_LAST_ARC;

                if (_lastFrozenNode == target.Node && !doFixedArray)
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
                    InCounts.Set((Int32) target.Node, InCounts.Get((Int32) target.Node) + 1);

                if (arc.Output != NO_OUTPUT)
                    flags += BIT_ARC_HAS_OUTPUT;

                _bytes.WriteByte((SByte) flags);
                WriteLabel(_bytes, arc.Label);

                if (arc.Output != NO_OUTPUT)
                {
                    Outputs.Write(arc.Output, _bytes);
                    ArcWithOutputCount++;
                }

                if (arc.NextFinalOutput != NO_OUTPUT)
                {
                    Outputs.WriteFinalOutput(arc.NextFinalOutput, _bytes);
                }

                if (targetHasArcs && (flags & BIT_TARGET_NEXT) == 0)
                {
                    // TODO: is assert correct here?
                    Debug.Assert(target.Node > 0);
                    _bytes.WriteVLong(target.Node);
                }

                if (doFixedArray)
                {
                    _bytesPerArc[arcIdx] = (Int32) (_bytes.GetPosition() - lastArcStart);
                    lastArcStart = _bytes.GetPosition();
                    maxBytesPerArc = Math.Max(maxBytesPerArc, _bytesPerArc[arcIdx]);
                }
            }

            if (doFixedArray)
            {
                var MAX_HEADER_SIZE = 11;
                // TODO: assert correct here?
                Debug.Assert(maxBytesPerArc > 0);

                var header = new SByte[MAX_HEADER_SIZE];
                var bad = new ByteArrayDataOutput(header);
                bad.WriteByte(ARCS_AS_FIXED_ARRAY);
                bad.WriteVInt(nodeIn.NumArcs);
                bad.WriteVInt(maxBytesPerArc);
                var headerLen = bad.GetPosition();

                Int64 fixedArrayStart = startAddress + headerLen;

                var srcPos = _bytes.GetPosition();
                var destPos = fixedArrayStart + nodeIn.NumArcs*maxBytesPerArc;
                // TODO: assert correct here?
                Debug.Assert(destPos >= srcPos);
                if (destPos > srcPos)
                {
                    _bytes.SkipBytes((Int32) (destPos - srcPos));
                    for (var arcIdx = nodeIn.NumArcs - 1; arcIdx >= 0; arcIdx--)
                    {
                        destPos -= maxBytesPerArc;
                        srcPos -= _bytesPerArc[arcIdx];
                        if (srcPos != destPos)
                        {
                            if (!(destPos > srcPos))
                                throw new InvalidOperationException("destPos=" + destPos + " srcPos=" + srcPos +
                                                                    " arcIdx=" + arcIdx + " maxBytesPerArc=" +
                                                                    maxBytesPerArc + " bytesPerArc[arcIdx]=" +
                                                                    _bytesPerArc[arcIdx] + " nodeIn.numArcs=" +
                                                                    nodeIn.NumArcs);
                            _bytes.CopyBytes(srcPos, destPos, _bytesPerArc[arcIdx]);
                        }
                    }
                }

                // now write the header
                _bytes.WriteBytes(startAddress, header, 0, headerLen);
            }

            var thisNodeAddress = _bytes.GetPosition() - 1;

            _bytes.Reverse(startAddress, thisNodeAddress);

            // PackedInts uses Int32 as the index, so we cannot handle
            // > 2.1B nodes when packing
            if (NodeAddress != null && NodeCount == Int32.MaxValue)
                throw new InvalidOperationException("cannot create a packed FST with more than 2.1 billion nodes");

            NodeCount++;
            Int64 node;
            if (NodeAddress != null)
            {
                if ((Int32) NodeCount == NodeAddress.Size)
                {
                    NodeAddress =
                        NodeAddress.Resize(ArrayUtil.Oversize(NodeAddress.Size + 1, NodeAddress.GetBitsPerValue()));
                    InCounts = InCounts.Resize(ArrayUtil.Oversize(InCounts.Size + 1, InCounts.GetBitsPerValue()));
                }

                NodeAddress.Set((Int32) NodeCount, thisNodeAddress);
                node = NodeCount;
            }
            else
            {
                node = thisNodeAddress;
            }
            _lastFrozenNode = node;

            return node;
        }

        public Arc<T> GetFirstArc(Arc<T> arc)
        {
            if (EmptyOutput != null)
            {
                arc.Flags = (SByte) (BIT_FINAL_ARC | BIT_LAST_ARC);
                arc.NextFinalOutput = EmptyOutput;
            }
            else
            {
                arc.Flags = (SByte) BIT_LAST_ARC;
                arc.NextFinalOutput = NO_OUTPUT;
            }
            arc.Output = NO_OUTPUT;

            return arc;
        }

        public Arc<T> ReadLastTargetArc(Arc<T> follow, Arc<T> arc, BytesReader input)
        {
            if (!TargetHasArcs(follow))
            {
                // TODO: assert correct here?
                Debug.Assert(follow.IsFinal());
                arc.Label = END_LABEL;
                arc.Target = FINAL_END_NODE;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (SByte) BIT_LAST_ARC;
                return arc;
            }
            else
            {
                input.SetPosition(GetNodeAddress(follow.Target));
                arc.Node = follow.Target;
                var b = input.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
                    arc.NumArcs = input.ReadVInt();
                    if (_packed || Version >= VERSION_VINT_TARGET)
                        arc.BytesPerArc = input.ReadVInt();
                    else
                        arc.BytesPerArc = input.ReadInt();

                    arc.PosArcsStart = input.GetPosition();
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
                        if (arc.Flags(BIT_ARC_HAS_FINAL_OUTPUT))
                            Outputs.ReadFinalOutput(input);
                        if (arc.Flags(BIT_STOP_NODE)) {
                        } else if (arc.Flag(BIT_TARGET_NEXT)) {
                        } else if (_packed)
                            input.ReadVLong();
                        else
                            ReadUnpackedNodeTarget(input);

                        arc.Flags = input.ReadByte();
                    }

                    input.SkipBytes(-1);
                    arc.NextArc = input.GetPosition();
                }

                ReadNextRealArc(arc, input);
                // TODO: assert is correct here?
                Debug.Assert(arc.IsLast());
                return arc;
            }
        }

        private Int64 ReadUnpackedNodeTarget(BytesReader input)
        {
            Int64 target;
            if (Version < VERSION_VINT_TARGET)
                target = input.ReadInt();
            else
                target = input.ReadVLong();

            return target;
        }

        public Arc<T> ReadFirstTargetArc(Arc<T> follow, Arc<T> arc, ByteReader input)
        {
            if (follow.IsFinal())
            {
                arc.Label = END_LABEL;
                arc.Output = follow.NextFinalOutput;
                arc.Flags = (SByte) BIT_FINAL_ARC;
                if (follow.Target <= 0)
                    arc.Flags |= (SByte) BIT_LAST_ARC;
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

        public Arc<T> ReadFirstRealTargetArc(Int64 node, Arc<T> arc, BytesReader input)
        {
            var address = GetNodeAddress(node);
            input.SetPosition(address);

            arc.Node = node;

            if (input.ReadByte() == ARCS_AS_FIXED_ARRAY)
            {
                arc.NumArcs = input.ReadVInt();
                if (_packed || Version >= VERSION_VINT_TARGET)
                    arc.BytesPerArc = input.ReadVInt();
                else
                    arc.BytesPerArc = input.ReadInt();

                arc.ArcIdx = -1;
                arc.NextArc = arc.PosArcsStart = input.GetPosition();
            }
            else
            {
                arc.NextArc = address;
                arc.BytesPerArc = 0;
            }

            return ReadNextRealArc(arc, input);
        }

        internal Boolean IsExpandedTarget(Arc<T> follow, BytesReader input)
        {
            if (!TargetHasArcs(follow))
                return false;
            else
            {
                input.SetPosition(GetNodeAddress(follow.Target));
                return input.ReadByte() == ARCS_AS_FIXED_ARRAY;
            }
        }

        public Arc<T> ReadNextArc(Arc<T> arc, BytesReader input)
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

        public Int32 ReadNextArcLabel(Arc<T> arc, BytesReader input)
        {
            // TODO: assert correct here?
            Debug.Assert(!arc.IsLast());

            if (arc.Label == END_LABEL)
            {
                var pos = GetNodeAddress(arc.NextArc);
                input.SetPosition(pos);

                var b = input.ReadByte();
                if (b == ARCS_AS_FIXED_ARRAY)
                {
                    input.ReadVInt();

                    if (_packed || Version >= VERSION_VINT_TARGET)
                        input.ReadVInt();
                    else
                        input.ReadInt();
                }
                else
                {
                    input.SetPosition(arc.NextArc);
                }
            }

            input.ReadByte();
            return ReadLabel(input);
        }

        public Arc<T> ReadNextRealArc(Arc<T> arc, BytesReader input)
        {
            if (arc.BytesPerArc != 0)
            {
                arc.ArcIdx++;
                // TODO: assert correct here?
                Debug.Assert(arc.ArcIdx < arc.NumArcs);
                input.SetPosition(arc.PosArcsStart);
                input.SkipBytes(arc.ArcIdx*arc.BytesPerArc);
            }
            else
            {
                input.SetPosition(arc.NextArc);
            }
            arc.Flags = input.ReadByte();
            arc.Label = ReadLabel(input);

            if (arc.Flags(BIT_ARC_HAS_OUTPUT))
                arc.Output = Outputs.Read(input);
            else
                arc.Output = Outputs.GetNoOutput();

            if (arc.Flags(BIT_ARC_HAS_FINAL_OUTPUT))
                arc.NextFinalOutput = Outputs.ReadFinalOutput(input);
            else
                arc.NextFinalOutput = Outputs.GetNoOutput();

            if (arc.Flag(BIT_STOP_NODE))
            {
                if (arc.Flag(BIT_FINAL_ARC))
                    arc.Target = FINAL_END_NODE;
                else
                    arc.Target = NON_FINAL_END_NODE;

                arc.NextArc = input.GetPosition();
            }
            else if (arc.Flag(BIT_TARGET_NEXT))
            {
                arc.NextArc = input.GetPosition();

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
                            input.SetPosition(arc.PosArcsStart);
                            input.SkipBytes(arc.BytesPerArc*arc.NumArcs);
                        }
                    }
                    arc.Target = input.GetPosition();
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
                if (_packed)
                {
                    var pos = input.GetPosition();
                    var code = input.ReadVLong();
                    if (arc.Flag(BIT_TARGET_DELTA))
                    {
                        arc.Target = pos + code;
                    }
                    else if (code < _nodeRefToAddress.Size)
                    {
                        arc.Target = _nodeRefToAddress.Get((Int32) code);
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
                arc.NextArc = input.GetPosition();
            }
            return arc;
        }

        public Arc<T> FindTargetArc(Int32 labelToMatch, Arc<T> follow, Arc<T> arc, BytesReader input)
        {
            // TODO: appropriate error message
            if (_cachedRootArcs == null) throw new InvalidOperationException("cachedRootArcs cannot be null");

            if (labelToMatch == END_LABEL)
            {
                if (follow.IsFinal())
                {
                    if (follow.Target <= 0)
                    {
                        arc.Flags = (SByte) BIT_LAST_ARC;
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

            if (follow.Target == _startNode && labelToMatch < _cachedRootArcs.Length)
            {
                var result = _cachedRootArcs[labelToMatch];
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

            input.SetPosition(GetNodeAddress(follow.Target));

            arc.Node = follow.Target;

            if (input.ReadByte() == ARCS_AS_FIXED_ARRAY)
            {
                arc.NumArcs = input.ReadVInt();
                if (_packed || Version >= VERSION_VINT_TARGET)
                    arc.BytesPerArc = input.ReadVInt();
                else
                    arc.BytesPerArc = input.ReadInt();

                arc.PosArcsStart = input.GetPosition();
                var low = 0;
                var high = arc.NumArcs - 1;
                while (low <= high)
                {
                    var mid = Support.Number.URShift((low + high), 1);
                    input.SetPosition(arc.PosArcsStart);
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

        private void SeekToNextNode(BytesReader input)
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
                    if (_packed)
                        input.ReadVLong();
                    else
                        ReadUnpackedNodeTarget(input);

                if (Flag(flags, BIT_LAST_ARC))
                    return;

                
            }
        }

        public Int64 GetNodeCount()
        {
            // 1+ in order to count the -1 implicit final node
            return 1 + NodeCount;
        }

        private Boolean ShouldExpand(Builder<T>.UnCompiledNode<T> node)
        {
            return _allowArrayArcs &&
                   ((node.Depth <= FIXED_ARRAY_SHALLOW_DISTANCE && node.NumArcs >= FIXED_ARRAY_NUM_ARCS_SHALLOW) ||
                    node.NumArcs >= FIXED_ARRAY_NUM_ARCS_DEEP);
        }

        public BytesReader GetBytesReader()
        {
            return _packed ?
                _bytes.GetForwardReader() :
                _bytes.GetReverseReader();
        }

        public abstract class BytesReader : DataInput
        {
            public abstract Int64 GetPosition();

            public abstract void SetPosition();

            public abstract Boolean Reversed();

            public abstract void SkipBytes(Int32 count);
        }

        private class ArcAndState<T>
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

        private FST(INPUT_TYPE inputType, Outputs<T> outputs, Int32 bytesPageBits)
        {
            Version = VERSION_CURRENT;
            _packed = true;
            _inputType = inputType;
            _bytes = new BytesStore(bytesPageBits);
            _outputs = outputs;
            NO_OUTPUT = outputs.GetNoOutput();

            AllowArrayArcs = false;
        }

        internal FST<T> Pack(Int32 minInCountDeref, Int32 maxDerefNodes, Single acceptableOverheadRatio)
        {
            if (NodeAddress == null) throw new ArgumentException("this FST was not built with willPackFST=true");

            var arc = new Arc<T>;

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
                        q.Add(new NodeAndInCount(node, (Int32) InCounts.Get(node)));
                        if (q.Size == topN)
                            bottom = q.Top();
                    }
                    else if (InCounts.Get(node) > bottom.Count)
                    {
                        q.InsertWithOverflow(new NodeAndInCount(node, (Int32) InCounts.Get(node)));
                    }
                }
            }

            // Free up RAM
            InCounts = null;
            var topNodeMap = new HashMap<Int64, Int64>();
            for (var downTo = q.Size - 1; downTo >= 0; downTo--)
            {
                var n = q.Pop();
                topNodeMap.Add(n.Node, downTo);
            }

            var newNodeAddress = new GrowableWriter(PackedInts.BitsRequired(_bytes.GetPosition()),
                                                    (Int32) (1 + NodeCount), acceptableOverheadRatio);

            for (var node = 1; node <= NodeCount; node++)
                newNodeAddress.Set(node, 1 + _bytes.GetPosition() - NodeAddress.Get(node));

            Int32 absCount;
            Int32 deltaCount;
            Int32 topCount;
            Int32 nextCount;

            FST<T> fst;

            while (true)
            {
                var changed = false;

                var negDelta = false;

                fst = new FST<T>(InputType, Outputs, _bytes.GetBlockBits());

                var writer = fst._bytes;

                writer.WriteBytes((SByte) 0);

                fst.ArcWithOutputCount = 0;
                fst.NodeCount = 0;
                fst.ArcCount = 0;

                absCount = deltaCount = topCount = nextCount = 0;

                var changedCount = 0;

                Int64 addressError = 0;

                for (var node = (Int32) NodeCount; node >= 1; node--)
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

                    // in the Java code there is a label 'writeNode:'
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

                            SByte flags = 0;

                            if (arc.IsLast())
                                flags += (SByte) BIT_LAST_ARC;

                            if (!useArcArray && node != 1 && arc.Target == node - 1)
                            {
                                flags += (SByte) BIT_TARGET_NEXT;
                                if (!retry)
                                    nextCount++;
                            }

                            if (arc.IsFinal())
                            {
                                flags += (SByte) BIT_FINAL_ARC;
                                if (arc.NextFinalOutput != NO_OUTPUT)
                                    flags += (SByte) BIT_ARC_HAS_FINAL_OUTPUT;
                            }
                            else
                            {
                                // TODO: assert is correct here?
                                Debug.Assert(arc.NextFinalOutput == NO_OUTPUT);
                            }

                            if (!TargetHasArcs(arc))
                                flags += (SByte) BIT_STOP_NODE;

                            if (arc.Output != NO_OUTPUT)
                                flags += (SByte) BIT_ARC_HAS_OUTPUT;

                            Int64 absPtr;
                            var doWriteTarget = TargetHasArcs(arc) && (flags & BIT_TARGET_NEXT) == 0;
                            if (doWriteTarget)
                            {
                                Int64 ptr;
                                var found = topNodeMap.TryGetValue(arc.Target, out ptr);
                                if (found)
                                    absPtr = ptr;
                                else
                                    absPtr = topNodeMap.Count + newNodeAddress.Get((Int32) arc.Target) + addressError;

                                var delta = newNodeAddress.Get((Int32) arc.Target) + addressError - writer.GetPosition() -
                                            2;
                                if (delta < 0)
                                {
                                    anyNegDelta = true;
                                    delta = 0;
                                }

                                if (delta < absPtr)
                                    flags += (SByte) BIT_TARGET_DELTA;
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
                                var delta = newNodeAddress.Get((Int32) arc.Target) + addressError - writer.GetPosition();
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
                                var arcBytes = (Int32) (writer.GetPosition() - arcStartPos);
                                maxBytesPerArc = Math.Max(maxBytesPerArc, arcBytes);
                                writer.SkipBytes((Int32) (arcStartPos + bytesPerArc - writer.GetPosition()));
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

            Int64 maxAddress = 0;
            foreach (var key in topNodeMap.Keys)
                maxAddress = Math.Max(maxAddress, newNodeAddress.Get((Int32) key));

            var nodeRefToAddressIn = PackedInts.GetMutable(topNodeMap.Count,
                PackedInts.BitsRequired(maxAddress), acceptableOverheadRatio);
            foreach (var pair in topNodeMap)
                nodeRefToAddressIn.Set(pair.Value, newNodeAddress.Get(pair.Key));

            fst._nodeRefToAddress = nodeRefToAddressIn;

            fst._startNode = newNodeAddress.Get((Int32) _startNode);

            if (_emptyOutput != null)
                fst._emptyOutput = _emptyOutput;

            // TODO: assert correct here?
            Debug.Assert(fst.NodeCount == NodeCount, "fst.NodeCount=" + fst.NodeCount + " NodeCount=" + NodeCount);
            Debug.Assert(fst.ArcCount == ArcCount);
            Debug.Assert(fst.ArcWithOutputCount == ArcWithOutputCount, "fst.ArcWithOutputCount=" + fst.ArcWithOutputCount + " ArcWithOutputCount=" + ArcWithOutputCount);

            fst._bytes.Finish();
            fst.CacheRootArcs();

            return fst;
        }

        private class NodeAndInCount : IComparable<NodeAndInCount>
        {
            private readonly Int32 _node;
            public Int32 Node { get { return _node; }}

            private readonly Int32 _count;
            public Int32 Count { get { return _count; } }

            public NodeAnInCount(Int32 node, Int32 count)
            {
                _node = node;
                _count = count;
            }

            public Int32 CompareTo(NodeAndInCount other)
            {
                if (Count > other.Count) return 1;
                if (Count < other.Count) return -1;
                return other.Node - Node;
            }
        }

        private class NodeQueue : PriorityQueue<NodeAndInCount>
        {
            public NodeQueue(Int32 topN)
            {
                Initialize(topN);
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
}
