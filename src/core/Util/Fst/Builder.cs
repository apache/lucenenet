using Lucene.Net.Util.Packed;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Fst
{
    public class Builder<T>
    {
        private readonly NodeHash<T> _dedupHash;
        private readonly FST<T> _fst;
        private FST<T> Fst { get { return _fst; } }
        private readonly T NO_OUTPUT;

        // simplistic pruning: we prune node (and all following
        // nodes) if less than this number of terms go through it
        private readonly int _minSuffixCount1;

        // better pruning: we prune node (and all following
        // nodes) if the prior node has less than this number of
        // terms go through it
        private readonly int _minSuffixCount2;

        private readonly bool _doShareNonSingletonNodes;
        private readonly int _shareMaxTailLength;

        private readonly IntsRef _lastInput = new IntsRef();

        // for packing
        private readonly bool _doPackFST;
        private readonly float _acceptableOverheadRatio;

        // NOTE: cutting this over to ArrayList instead loses ~6%
        // in build performance on 9.8M Wikipedia terms; so we
        // left this as an array:
        // current "frontier"
        private UnCompiledNode<T>[] _frontier;

        /** Expert: this is invoked by Builder whenever a suffix
         *  is serialized. */
        public abstract class FreezeTail<T>
        {
            public abstract void Freeze(UnCompiledNode<T>[] frontier, int prefixLenPlus1, IntsRef prevInput);
        }

        private readonly FreezeTail<T> _freezeTail;

        public Builder(FST.INPUT_TYPE inputType, Outputs<T> outputs)
            : this(inputType, 0, 0, true, true, int.MaxValue, outputs, null, false, PackedInts.COMPACT, true, 15)
        {

        }

        public Builder(FST.INPUT_TYPE inputType, int minSuffixCount1, int minSuffixCount2, bool doShareSuffix,
                       bool doShareNonSingletonNodes, int shareMaxTailLength, Outputs<T> outputs,
                       FreezeTail<T> freezeTail, bool doPackFST, float acceptableOverheadRatio,
                       bool allowArrayArcs,
                       int bytesPageBits)
        {
            _minSuffixCount1 = minSuffixCount1;
            _minSuffixCount2 = minSuffixCount2;
            _freezeTail = freezeTail;
            _doShareNonSingletonNodes = doShareNonSingletonNodes;
            _shareMaxTailLength = shareMaxTailLength;
            _doPackFST = doPackFST;
            _acceptableOverheadRatio = acceptableOverheadRatio;
            _fst = new FST<T>(inputType, outputs, doPackFST, acceptableOverheadRatio, allowArrayArcs, bytesPageBits);
            if (doShareSuffix)
            {
                _dedupHash = new NodeHash<T>(_fst, _fst.Bytes.GetReverseReader(false));
            }
            else
            {
                _dedupHash = null;
            }

            NO_OUTPUT = outputs.GetNoOutput();

            var f = new UnCompiledNode<T>[10];
            _frontier = f;
            for (var idx = 0; idx < _frontier.Length; idx++)
                _frontier[idx] = new UnCompiledNode<T>(this, idx);
        }

        public long TotStateCount
        {
            get { return _fst.NodeCount; }
        }

        public long TermCount
        {
            get { return _frontier[0].InputCount; }
        }

        public long MappedStateCount
        {
            get { return _dedupHash == null ? 0 : _fst.NodeCount; }
        }

        private CompiledNode CompileNode(UnCompiledNode<T> nodeIn, int tailLength)
        {
            Int64 node;
            if (_dedupHash != null && (_doShareNonSingletonNodes || nodeIn.NumArcs <= 1) &&
                tailLength <= _shareMaxTailLength)
                node = nodeIn.NumArcs == 0 ? _fst.AddNode(nodeIn) : _dedupHash.Add(nodeIn);
            else
                node = _fst.AddNode(nodeIn);

            //Debug.Assert(node != -2);

            nodeIn.Clear();

            return new CompiledNode { Node = node };
        }

        private void DoFreezeTail(int prefixLenPlus1)
        {
            if (_freezeTail != null)
            {
                _freezeTail.Freeze(_frontier, prefixLenPlus1, _lastInput);
            }
            else
            {
                var downTo = Math.Max(1, prefixLenPlus1);
                for (var idx = _lastInput.length; idx >= downTo; idx--)
                {
                    var doPrune = false;
                    var doCompile = false;

                    var node = _frontier[idx];
                    var parent = _frontier[idx - 1];

                    if (node.InputCount < _minSuffixCount1)
                    {
                        doPrune = true;
                        doCompile = true;
                    }
                    else if (idx > prefixLenPlus1)
                    {
                        if (parent.InputCount < _minSuffixCount2 ||
                            (_minSuffixCount2 == 1 && parent.InputCount == 1 && idx > 1))
                        {
                            doPrune = true;
                        }
                        else
                        {
                            doPrune = false;
                        }
                        doCompile = true;
                    }
                    else
                    {
                        doCompile = _minSuffixCount2 == 0;
                    }

                    if (node.InputCount < _minSuffixCount2 || (_minSuffixCount2 == 1 && node.InputCount == 1 && idx > 1))
                    {
                        for (var arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
                        {
                            UnCompiledNode<T> target = (UnCompiledNode<T>)node.Arcs[arcIdx].Target;
                            target.Clear();
                        }
                        node.NumArcs = 0;
                    }

                    if (doPrune)
                    {
                        node.Clear();
                        parent.DeleteLast(_lastInput.ints[_lastInput.offset + idx - 1], node);
                    }
                    else
                    {
                        if (_minSuffixCount2 != 0)
                        {
                            CompileAllTargets(node, _lastInput.length - idx);
                        }
                        var nextFinalOutput = node.Output;

                        var isFinal = node.IsFinal || node.NumArcs == 0;

                        if (doCompile)
                        {
                            parent.ReplaceLast(_lastInput.ints[_lastInput.offset + idx - 1],
                                               CompileNode(node, 1 + _lastInput.length - idx),
                                               nextFinalOutput,
                                               isFinal);
                        }
                        else
                        {
                            parent.ReplaceLast(_lastInput.ints[_lastInput.offset + idx - 1],
                                               node,
                                               nextFinalOutput,
                                               isFinal);
                            _frontier[idx] = new UnCompiledNode<T>(this, idx);
                        }
                    }
                }
            }
        }

#if DEBUG
        private String ToString(BytesRef b)
        {
            try
            {
                return b.Utf8ToString() + " " + b;
            }
            catch
            {
                return b.ToString();
            }
        }
#endif

        public void Add(IntsRef input, T output)
        {
            //#if DEBUG
            //            var b = new BytesRef(input.length);
            //            for (var x = 0; x < input.length; x++)
            //            {
            //                b.bytes[x] = (sbyte) input.ints[x];
            //            }
            //            b.length = input.length;
            //            if (output.Equals(NO_OUTPUT))
            //            {
            //                Console.WriteLine("\nFST ADD: input=" + ToString(b) + " " + b);
            //            }
            //            else
            //            {
            //                Console.WriteLine("\nFST ADD: input=" + ToString(b) + " " + b + " output=" + _fst.Outputs.OutputToString(output));
            //            }
            //#endif

            if (output.Equals(NO_OUTPUT))
            {
                output = NO_OUTPUT;
            }

            //Debug.Assert(_lastInput.length == 0 || input.CompareTo(_lastInput) >= 0, "inputs are added out of order lastInput=" + _lastInput + " vs input=" + input);
            //Debug.Assert(ValidOutput(output));

            if (input.length == 0)
            {
                _frontier[0].InputCount++;
                _frontier[0].IsFinal = true;
                _fst.EmptyOutput = output;
                return;
            }

            var pos1 = 0;
            var pos2 = input.offset;

            var pos1Stop = Math.Min(_lastInput.length, input.length);
            while (true)
            {
                _frontier[pos1].InputCount++;
                if (pos1 >= pos1Stop || _lastInput.ints[pos1] != input.ints[pos2])
                    break;
                pos1++;
                pos2++;
            }

            var prefixLenPlus1 = pos1 + 1;

            if (_frontier.Length < input.length + 1)
            {
                var next =
                    new UnCompiledNode<T>[ArrayUtil.Oversize(input.length + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(_frontier, 0, next, 0, _frontier.Length);
                for (var idx = _frontier.Length; idx < next.Length; idx++)
                    next[idx] = new UnCompiledNode<T>(this, idx);
                _frontier = next;
            }

            DoFreezeTail(prefixLenPlus1);

            for (var idx = prefixLenPlus1; idx <= input.length; idx++)
            {
                _frontier[idx - 1].AddArc(input.ints[input.offset + idx - 1],
                                          _frontier[idx]);
                _frontier[idx].InputCount++;
            }

            var lastNode = _frontier[input.length];
            if (_lastInput.length != input.length || prefixLenPlus1 != input.length + 1)
            {
                lastNode.IsFinal = true;
                lastNode.Output = NO_OUTPUT;
            }

            for (var idx = 1; idx < prefixLenPlus1; idx++)
            {
                var node = _frontier[idx];
                var parentNode = _frontier[idx - 1];

                var lastOutput = parentNode.GetLastOutput(input.ints[input.offset + idx - 1]);
                //Debug.Assert(ValidOutput(lastOutput));

                T commonOutputPrefix;
                T wordSuffix;

                if ((object)lastOutput != (object)NO_OUTPUT)
                {
                    commonOutputPrefix = _fst.Outputs.Common(output, lastOutput);
                    //Debug.Assert(ValidOutput(commonOutputPrefix));
                    wordSuffix = _fst.Outputs.Subtract(lastOutput, commonOutputPrefix);
                    //Debug.Assert(ValidOutput(wordSuffix));
                    parentNode.SetLastOutput(input.ints[input.offset + idx - 1], commonOutputPrefix);
                    node.PrependOutput(wordSuffix);
                }
                else
                    commonOutputPrefix = wordSuffix = NO_OUTPUT;

                output = _fst.Outputs.Subtract(output, commonOutputPrefix);
                //Debug.Assert(ValidOutput(output));
            }

            if (_lastInput.length == input.length && prefixLenPlus1 == 1 + input.length)
            {
                lastNode.Output = _fst.Outputs.Merge(lastNode.Output, output);
            }
            else
            {
                _frontier[prefixLenPlus1 - 1].SetLastOutput(input.ints[input.offset + prefixLenPlus1 - 1], output);
            }

            _lastInput.CopyInts(input);
        }

        private bool ValidOutput(T output)
        {
            return (output.Equals(NO_OUTPUT) || !output.Equals(NO_OUTPUT));
        }

        public FST<T> Finish()
        {
            var root = _frontier[0];

            DoFreezeTail(0);
            if (root.InputCount < _minSuffixCount1 || root.InputCount < _minSuffixCount2 || root.NumArcs == 0)
            {
                if (_fst.EmptyOutput == null) return null;
                if (_minSuffixCount1 > 0 || _minSuffixCount2 > 0) return null;
            }
            else
            {
                if (_minSuffixCount2 != 0)
                    CompileAllTargets(root, _lastInput.length);
            }

            _fst.Finish(CompileNode(root, _lastInput.length).Node);

            return _doPackFST ? _fst.Pack(3, Math.Max(10, (int)_fst.GetNodeCount() / 4), _acceptableOverheadRatio) : _fst;
        }

        private void CompileAllTargets(UnCompiledNode<T> node, int tailLength)
        {
            for (var arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
            {
                var arc = node.Arcs[arcIdx];
                if (!arc.Target.IsCompiled())
                {
                    var n = (UnCompiledNode<T>)arc.Target;
                    if (n.NumArcs == 0)
                    {
                        arc.IsFinal = n.IsFinal = true;
                    }
                    arc.Target = CompileNode(n, tailLength - 1);
                }
            }
        }

        public class Arc<T>
        {
            public int Label { get; set; }
            public INode Target { get; set; }
            public bool IsFinal { get; set; }
            public T Output { get; set; }
            public T NextFinalOutput { get; set; }
        }

        public interface INode
        {
            bool IsCompiled();
        }

        public Int64 FstSizeInBytes()
        {
            return _fst.SizeInBytes();
        }

        internal sealed class CompiledNode : INode
        {
            public Int64 Node { get; set; }

            public bool IsCompiled()
            {
                return true;
            }
        }

        public sealed class UnCompiledNode<T> : INode
        {
            private readonly Builder<T> _owner;
            Builder<T> Owner { get { return _owner; } }

            public int NumArcs { get; set; }
            public Arc<T>[] Arcs { get; set; }
            public T Output { get; set; }
            public bool IsFinal { get; set; }
            public Int64 InputCount { get; set; }

            private readonly int _depth;
            public int Depth { get { return _depth; } }

            public UnCompiledNode(Builder<T> owner, int depth)
            {
                _owner = owner;
                Arcs = new Arc<T>[1];
                Arcs[0] = new Arc<T>();
                Output = owner.NO_OUTPUT;
                _depth = depth;
            }

            public bool IsCompiled()
            {
                return false;
            }

            public void Clear()
            {
                NumArcs = 0;
                IsFinal = false;
                Output = Owner.NO_OUTPUT;
                InputCount = 0;
            }

            public T GetLastOutput(int labelToMatch)
            {
                // TODO: is debug.assert correct here? or is this validation? ...
                Debug.Assert(NumArcs > 0);
                Debug.Assert(Arcs[NumArcs - 1].Label == labelToMatch);
                return Arcs[NumArcs - 1].Output;
            }

            public void AddArc(int label, INode target)
            {
                if (!(label >= 0)) throw new ArgumentException("label must be greater than or equal to zero");

                // TODO: is debug.assert correct here? or is this validation? ...
                //Debug.Assert(NumArcs == 0 || label > Arcs[NumArcs - 1].Label, "arc[-1].label=" + Arcs[NumArcs - 1].Label + " new label=" + label + " numArcs=" + NumArcs);

                if (NumArcs == Arcs.Length)
                {
                    var newArcs = new Arc<T>[ArrayUtil.Oversize(NumArcs + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(Arcs, 0, newArcs, 0, Arcs.Length);
                    for (var arcIdx = NumArcs; arcIdx < newArcs.Length; arcIdx++)
                    {
                        newArcs[arcIdx] = new Arc<T>();
                    }
                    Arcs = newArcs;
                }
                var arc = Arcs[NumArcs++];
                arc.Label = label;
                arc.Target = target;
                arc.Output = arc.NextFinalOutput = Owner.NO_OUTPUT;
                arc.IsFinal = false;
            }

            public void ReplaceLast(int labelToMatch, INode target, T nextFinalOutput, bool isFinal)
            {
                // TODO: is debug.assert correct here? or is this validation? ...
                Debug.Assert(NumArcs > 0);

                var arc = Arcs[NumArcs - 1];
                Debug.Assert(arc.Label == labelToMatch, "arc.label=" + arc.Label + " vs " + labelToMatch);
                arc.Target = target;
                arc.NextFinalOutput = nextFinalOutput;
                arc.IsFinal = isFinal;
            }

            public void DeleteLast(int label, INode target)
            {
                // TODO: is debug.assert correct here? or is this validation? ...
                Debug.Assert(NumArcs > 0);
                Debug.Assert(label == Arcs[NumArcs - 1].Label);
                Debug.Assert(target == Arcs[NumArcs - 1].Target);
                NumArcs--;
            }

            public void SetLastOutput(int labelToMatch, T newOutput)
            {
                // TODO: is debug.assert correct here? or is this validation? ...
                Debug.Assert(Owner.ValidOutput(newOutput));
                Debug.Assert(NumArcs > 0);
                var arc = Arcs[NumArcs - 1];
                Debug.Assert(arc.Label == labelToMatch);
                arc.Output = newOutput;
            }

            public void PrependOutput(T outputPrefix)
            {
                // TODO: is debug.assert correct here? or is this validation? ...
                Debug.Assert(Owner.ValidOutput(outputPrefix));

                for (var arcIdx = 0; arcIdx < NumArcs; arcIdx++)
                {
                    Arcs[arcIdx].Output = Owner.Fst.Outputs.Add(outputPrefix, Arcs[arcIdx].Output);
                    Debug.Assert(Owner.ValidOutput(Arcs[arcIdx].Output));
                }

                if (IsFinal)
                {
                    Output = Owner.Fst.Outputs.Add(outputPrefix, Output);
                    Debug.Assert(Owner.ValidOutput(Output));
                }
            }
        }
    }
}
