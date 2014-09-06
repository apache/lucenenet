using System;
using System.Diagnostics;

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

    //using INPUT_TYPE = Lucene.Net.Util.Fst.FST.INPUT_TYPE; // javadoc
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;

    // TODO: could we somehow stream an FST to disk while we
    // build it?

    /// <summary>
    /// Builds a minimal FST (maps an IntsRef term to an arbitrary
    /// output) from pre-sorted terms with outputs.  The FST
    /// becomes an FSA if you use NoOutputs.  The FST is written
    /// on-the-fly into a compact serialized format byte array, which can
    /// be saved to / loaded from a Directory or used directly
    /// for traversal.  The FST is always finite (no cycles).
    ///
    /// <p>NOTE: The algorithm is described at
    /// http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.24.3698</p>
    ///
    /// <p>The parameterized type T is the output type.  See the
    /// subclasses of <seealso cref="Outputs"/>.
    ///
    /// <p>FSTs larger than 2.1GB are now possible (as of Lucene
    /// 4.2).  FSTs containing more than 2.1B nodes are also now
    /// possible, however they cannot be packed.
    ///
    /// @lucene.experimental
    /// </summary>

    public class Builder<T>
    {
        private readonly NodeHash<T> DedupHash;
        private readonly FST<T> Fst;
        private readonly T NO_OUTPUT;

        // private static final boolean DEBUG = true;

        // simplistic pruning: we prune node (and all following
        // nodes) if less than this number of terms go through it:
        private readonly int MinSuffixCount1;

        // better pruning: we prune node (and all following
        // nodes) if the prior node has less than this number of
        // terms go through it:
        private readonly int MinSuffixCount2;

        private readonly bool DoShareNonSingletonNodes;
        private readonly int ShareMaxTailLength;

        private readonly IntsRef LastInput = new IntsRef();

        // for packing
        private readonly bool DoPackFST;

        private readonly float AcceptableOverheadRatio;

        // NOTE: cutting this over to ArrayList instead loses ~6%
        // in build performance on 9.8M Wikipedia terms; so we
        // left this as an array:
        // current "frontier"
        private UnCompiledNode<T>[] Frontier;

        /// <summary>
        /// Expert: this is invoked by Builder whenever a suffix
        ///  is serialized.
        /// </summary>
        public abstract class FreezeTail<S>
        {
            public abstract void Freeze(UnCompiledNode<S>[] frontier, int prefixLenPlus1, IntsRef prevInput);
        }

        private readonly FreezeTail<T> FreezeTail_Renamed;

        /// <summary>
        /// Instantiates an FST/FSA builder without any pruning. A shortcut
        /// to {@link #Builder(FST.INPUT_TYPE, int, int, boolean,
        /// boolean, int, Outputs, FreezeTail, boolean, float,
        /// boolean, int)} with pruning options turned off.
        /// </summary>
        public Builder(FST<T>.INPUT_TYPE inputType, Outputs<T> outputs)
            : this(inputType, 0, 0, true, true, int.MaxValue, outputs, null, false, PackedInts.COMPACT, true, 15)
        {
        }

        /// <summary>
        /// Instantiates an FST/FSA builder with all the possible tuning and construction
        /// tweaks. Read parameter documentation carefully.
        /// </summary>
        /// <param name="inputType">
        ///    The input type (transition labels). Can be anything from <seealso cref="INPUT_TYPE"/>
        ///    enumeration. Shorter types will consume less memory. Strings (character sequences) are
        ///    represented as <seealso cref="INPUT_TYPE#BYTE4"/> (full unicode codepoints).
        /// </param>
        /// <param name="minSuffixCount1">
        ///    If pruning the input graph during construction, this threshold is used for telling
        ///    if a node is kept or pruned. If transition_count(node) &gt;= minSuffixCount1, the node
        ///    is kept.
        /// </param>
        /// <param name="minSuffixCount2">
        ///    (Note: only Mike McCandless knows what this one is really doing...)
        /// </param>
        /// <param name="doShareSuffix">
        ///    If <code>true</code>, the shared suffixes will be compacted into unique paths.
        ///    this requires an additional RAM-intensive hash map for lookups in memory. Setting this parameter to
        ///    <code>false</code> creates a single suffix path for all input sequences. this will result in a larger
        ///    FST, but requires substantially less memory and CPU during building.
        /// </param>
        /// <param name="doShareNonSingletonNodes">
        ///    Only used if doShareSuffix is true.  Set this to
        ///    true to ensure FST is fully minimal, at cost of more
        ///    CPU and more RAM during building.
        /// </param>
        /// <param name="shareMaxTailLength">
        ///    Only used if doShareSuffix is true.  Set this to
        ///    Integer.MAX_VALUE to ensure FST is fully minimal, at cost of more
        ///    CPU and more RAM during building.
        /// </param>
        /// <param name="outputs"> The output type for each input sequence. Applies only if building an FST. For
        ///    FSA, use <seealso cref="NoOutputs#getSingleton()"/> and <seealso cref="NoOutputs#getNoOutput()"/> as the
        ///    singleton output object.
        /// </param>
        /// <param name="doPackFST"> Pass true to create a packed FST.
        /// </param>
        /// <param name="acceptableOverheadRatio"> How to trade speed for space when building the FST. this option </param>
        ///    is only relevant when doPackFST is true. <seealso cref= PackedInts#getMutable(int, int, float)
        /// </seealso>
        /// <param name="allowArrayArcs"> Pass false to disable the array arc optimization
        ///    while building the FST; this will make the resulting
        ///    FST smaller but slower to traverse.
        /// </param>
        /// <param name="bytesPageBits"> How many bits wide to make each
        ///    byte[] block in the BytesStore; if you know the FST
        ///    will be large then make this larger.  For example 15
        ///    bits = 32768 byte pages. </param>
        public Builder(FST<T>.INPUT_TYPE inputType, int minSuffixCount1, int minSuffixCount2, bool doShareSuffix, bool doShareNonSingletonNodes, int shareMaxTailLength, Outputs<T> outputs, FreezeTail<T> freezeTail, bool doPackFST, float acceptableOverheadRatio, bool allowArrayArcs, int bytesPageBits)
        {
            this.MinSuffixCount1 = minSuffixCount1;
            this.MinSuffixCount2 = minSuffixCount2;
            this.FreezeTail_Renamed = freezeTail;
            this.DoShareNonSingletonNodes = doShareNonSingletonNodes;
            this.ShareMaxTailLength = shareMaxTailLength;
            this.DoPackFST = doPackFST;
            this.AcceptableOverheadRatio = acceptableOverheadRatio;
            Fst = new FST<T>(inputType, outputs, doPackFST, acceptableOverheadRatio, allowArrayArcs, bytesPageBits);
            if (doShareSuffix)
            {
                DedupHash = new NodeHash<T>(Fst, Fst.Bytes.GetReverseReader(false));
            }
            else
            {
                DedupHash = null;
            }
            NO_OUTPUT = outputs.NoOutput;

            UnCompiledNode<T>[] f = (UnCompiledNode<T>[])new UnCompiledNode<T>[10];
            Frontier = f;
            for (int idx = 0; idx < Frontier.Length; idx++)
            {
                Frontier[idx] = new UnCompiledNode<T>(this, idx);
            }
        }

        public virtual long TotStateCount
        {
            get
            {
                return Fst.nodeCount;
            }
        }

        public virtual long TermCount
        {
            get
            {
                return Frontier[0].InputCount;
            }
        }

        public virtual long MappedStateCount
        {
            get
            {
                return DedupHash == null ? 0 : Fst.nodeCount;
            }
        }

        private CompiledNode CompileNode(UnCompiledNode<T> nodeIn, int tailLength)
        {
            long node;
            if (DedupHash != null && (DoShareNonSingletonNodes || nodeIn.NumArcs <= 1) && tailLength <= ShareMaxTailLength)
            {
                if (nodeIn.NumArcs == 0)
                {
                    node = Fst.AddNode(nodeIn);
                }
                else
                {
                    node = DedupHash.Add(nodeIn);
                }
            }
            else
            {
                node = Fst.AddNode(nodeIn);
            }
            Debug.Assert(node != -2);

            nodeIn.Clear();

            CompiledNode fn = new CompiledNode();
            fn.Node = node;
            return fn;
        }

        private void DoFreezeTail(int prefixLenPlus1)
        {
            if (FreezeTail_Renamed != null)
            {
                // Custom plugin:
                FreezeTail_Renamed.Freeze(Frontier, prefixLenPlus1, LastInput);
            }
            else
            {
                //System.out.println("  compileTail " + prefixLenPlus1);
                int downTo = Math.Max(1, prefixLenPlus1);
                for (int idx = LastInput.Length; idx >= downTo; idx--)
                {
                    bool doPrune = false;
                    bool doCompile = false;

                    UnCompiledNode<T> node = Frontier[idx];
                    UnCompiledNode<T> parent = Frontier[idx - 1];

                    if (node.InputCount < MinSuffixCount1)
                    {
                        doPrune = true;
                        doCompile = true;
                    }
                    else if (idx > prefixLenPlus1)
                    {
                        // prune if parent's inputCount is less than suffixMinCount2
                        if (parent.InputCount < MinSuffixCount2 || (MinSuffixCount2 == 1 && parent.InputCount == 1 && idx > 1))
                        {
                            // my parent, about to be compiled, doesn't make the cut, so
                            // I'm definitely pruned

                            // if minSuffixCount2 is 1, we keep only up
                            // until the 'distinguished edge', ie we keep only the
                            // 'divergent' part of the FST. if my parent, about to be
                            // compiled, has inputCount 1 then we are already past the
                            // distinguished edge.  NOTE: this only works if
                            // the FST outputs are not "compressible" (simple
                            // ords ARE compressible).
                            doPrune = true;
                        }
                        else
                        {
                            // my parent, about to be compiled, does make the cut, so
                            // I'm definitely not pruned
                            doPrune = false;
                        }
                        doCompile = true;
                    }
                    else
                    {
                        // if pruning is disabled (count is 0) we can always
                        // compile current node
                        doCompile = MinSuffixCount2 == 0;
                    }

                    //System.out.println("    label=" + ((char) lastInput.ints[lastInput.offset+idx-1]) + " idx=" + idx + " inputCount=" + frontier[idx].inputCount + " doCompile=" + doCompile + " doPrune=" + doPrune);

                    if (node.InputCount < MinSuffixCount2 || (MinSuffixCount2 == 1 && node.InputCount == 1 && idx > 1))
                    {
                        // drop all arcs
                        for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
                        {
                            UnCompiledNode<T> target = (UnCompiledNode<T>)node.Arcs[arcIdx].Target;
                            target.Clear();
                        }
                        node.NumArcs = 0;
                    }

                    if (doPrune)
                    {
                        // this node doesn't make it -- deref it
                        node.Clear();
                        parent.DeleteLast(LastInput.Ints[LastInput.Offset + idx - 1], node);
                    }
                    else
                    {
                        if (MinSuffixCount2 != 0)
                        {
                            CompileAllTargets(node, LastInput.Length - idx);
                        }
                        T nextFinalOutput = node.Output;

                        // We "fake" the node as being final if it has no
                        // outgoing arcs; in theory we could leave it
                        // as non-final (the FST can represent this), but
                        // FSTEnum, Util, etc., have trouble w/ non-final
                        // dead-end states:
                        bool isFinal = node.IsFinal || node.NumArcs == 0;

                        if (doCompile)
                        {
                            // this node makes it and we now compile it.  first,
                            // compile any targets that were previously
                            // undecided:
                            parent.ReplaceLast(LastInput.Ints[LastInput.Offset + idx - 1], CompileNode(node, 1 + LastInput.Length - idx), nextFinalOutput, isFinal);
                        }
                        else
                        {
                            // replaceLast just to install
                            // nextFinalOutput/isFinal onto the arc
                            parent.ReplaceLast(LastInput.Ints[LastInput.Offset + idx - 1], node, nextFinalOutput, isFinal);
                            // this node will stay in play for now, since we are
                            // undecided on whether to prune it.  later, it
                            // will be either compiled or pruned, so we must
                            // allocate a new node:
                            Frontier[idx] = new UnCompiledNode<T>(this, idx);
                        }
                    }
                }
            }
        }

        // for debugging
        /*
        private String toString(BytesRef b) {
          try {
            return b.utf8ToString() + " " + b;
          } catch (Throwable t) {
            return b.toString();
          }
        }
        */

        /// <summary>
        /// It's OK to add the same input twice in a row with
        ///  different outputs, as long as outputs impls the merge
        ///  method. Note that input is fully consumed after this
        ///  method is returned (so caller is free to reuse), but
        ///  output is not.  So if your outputs are changeable (eg
        ///  <seealso cref="ByteSequenceOutputs"/> or {@link
        ///  IntSequenceOutputs}) then you cannot reuse across
        ///  calls.
        /// </summary>
        public virtual void Add(IntsRef input, T output)
        {
            /*
            if (DEBUG) {
              BytesRef b = new BytesRef(input.length);
              for(int x=0;x<input.length;x++) {
                b.bytes[x] = (byte) input.ints[x];
              }
              b.length = input.length;
              if (output == NO_OUTPUT) {
                System.out.println("\nFST ADD: input=" + toString(b) + " " + b);
              } else {
                System.out.println("\nFST ADD: input=" + toString(b) + " " + b + " output=" + fst.outputs.outputToString(output));
              }
            }
            */

            // De-dup NO_OUTPUT since it must be a singleton:
            if (output.Equals(NO_OUTPUT))
            {
                output = NO_OUTPUT;
            }

            Debug.Assert(LastInput.Length == 0 || input.CompareTo(LastInput) >= 0, "inputs are added out of order lastInput=" + LastInput + " vs input=" + input);
            Debug.Assert(ValidOutput(output));

            //System.out.println("\nadd: " + input);
            if (input.Length == 0)
            {
                // empty input: only allowed as first input.  we have
                // to special case this because the packed FST
                // format cannot represent the empty input since
                // 'finalness' is stored on the incoming arc, not on
                // the node
                Frontier[0].InputCount++;
                Frontier[0].IsFinal = true;
                Fst.EmptyOutput = output;
                return;
            }

            // compare shared prefix length
            int pos1 = 0;
            int pos2 = input.Offset;
            int pos1Stop = Math.Min(LastInput.Length, input.Length);
            while (true)
            {
                Frontier[pos1].InputCount++;
                //System.out.println("  incr " + pos1 + " ct=" + frontier[pos1].inputCount + " n=" + frontier[pos1]);
                if (pos1 >= pos1Stop || LastInput.Ints[pos1] != input.Ints[pos2])
                {
                    break;
                }
                pos1++;
                pos2++;
            }
            int prefixLenPlus1 = pos1 + 1;

            if (Frontier.Length < input.Length + 1)
            {
                UnCompiledNode<T>[] next = new UnCompiledNode<T>[ArrayUtil.Oversize(input.Length + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(Frontier, 0, next, 0, Frontier.Length);
                for (int idx = Frontier.Length; idx < next.Length; idx++)
                {
                    next[idx] = new UnCompiledNode<T>(this, idx);
                }
                Frontier = next;
            }

            // minimize/compile states from previous input's
            // orphan'd suffix
            DoFreezeTail(prefixLenPlus1);

            // init tail states for current input
            for (int idx = prefixLenPlus1; idx <= input.Length; idx++)
            {
                Frontier[idx - 1].AddArc(input.Ints[input.Offset + idx - 1], Frontier[idx]);
                Frontier[idx].InputCount++;
            }

            UnCompiledNode<T> lastNode = Frontier[input.Length];
            if (LastInput.Length != input.Length || prefixLenPlus1 != input.Length + 1)
            {
                lastNode.IsFinal = true;
                lastNode.Output = NO_OUTPUT;
            }

            // push conflicting outputs forward, only as far as
            // needed
            for (int idx = 1; idx < prefixLenPlus1; idx++)
            {
                UnCompiledNode<T> node = Frontier[idx];
                UnCompiledNode<T> parentNode = Frontier[idx - 1];

                T lastOutput = parentNode.GetLastOutput(input.Ints[input.Offset + idx - 1]);
                Debug.Assert(ValidOutput(lastOutput));

                T commonOutputPrefix;
                T wordSuffix;

                if ((object)lastOutput != (object)NO_OUTPUT)
                {
                    commonOutputPrefix = Fst.Outputs.Common(output, lastOutput);
                    Debug.Assert(ValidOutput(commonOutputPrefix));
                    wordSuffix = Fst.Outputs.Subtract(lastOutput, commonOutputPrefix);
                    Debug.Assert(ValidOutput(wordSuffix));
                    parentNode.SetLastOutput(input.Ints[input.Offset + idx - 1], commonOutputPrefix);
                    node.PrependOutput(wordSuffix);
                }
                else
                {
                    commonOutputPrefix = wordSuffix = NO_OUTPUT;
                }

                output = Fst.Outputs.Subtract(output, commonOutputPrefix);
                Debug.Assert(ValidOutput(output));
            }

            if (LastInput.Length == input.Length && prefixLenPlus1 == 1 + input.Length)
            {
                // same input more than 1 time in a row, mapping to
                // multiple outputs
                lastNode.Output = Fst.Outputs.Merge(lastNode.Output, output);
            }
            else
            {
                // this new arc is private to this new input; set its
                // arc output to the leftover output:
                Frontier[prefixLenPlus1 - 1].SetLastOutput(input.Ints[input.Offset + prefixLenPlus1 - 1], output);
            }

            // save last input
            LastInput.CopyInts(input);

            //System.out.println("  count[0]=" + frontier[0].inputCount);
        }

        private bool ValidOutput(T output)
        {
            return (object)output == (object)NO_OUTPUT || !output.Equals(NO_OUTPUT);
        }

        /// <summary>
        /// Returns final FST.  NOTE: this will return null if
        ///  nothing is accepted by the FST.
        /// </summary>
        public virtual FST<T> Finish()
        {
            UnCompiledNode<T> root = Frontier[0];

            // minimize nodes in the last word's suffix
            DoFreezeTail(0);
            if (root.InputCount < MinSuffixCount1 || root.InputCount < MinSuffixCount2 || root.NumArcs == 0)
            {
                if (Fst.emptyOutput == null)
                {
                    return null;
                }
                else if (MinSuffixCount1 > 0 || MinSuffixCount2 > 0)
                {
                    // empty string got pruned
                    return null;
                }
            }
            else
            {
                if (MinSuffixCount2 != 0)
                {
                    CompileAllTargets(root, LastInput.Length);
                }
            }
            //if (DEBUG) System.out.println("  builder.finish root.isFinal=" + root.isFinal + " root.Output=" + root.Output);
            Fst.Finish(CompileNode(root, LastInput.Length).Node);

            if (DoPackFST)
            {
                return Fst.Pack(3, Math.Max(10, (int)(Fst.NodeCount / 4)), AcceptableOverheadRatio);
            }
            else
            {
                return Fst;
            }
        }

        private void CompileAllTargets(UnCompiledNode<T> node, int tailLength)
        {
            for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
            {
                Arc<T> arc = node.Arcs[arcIdx];
                if (!arc.Target.Compiled)
                {
                    // not yet compiled
                    UnCompiledNode<T> n = (UnCompiledNode<T>)arc.Target;
                    if (n.NumArcs == 0)
                    {
                        //System.out.println("seg=" + segment + "        FORCE final arc=" + (char) arc.Label);
                        arc.IsFinal = n.IsFinal = true;
                    }
                    arc.Target = CompileNode(n, tailLength - 1);
                }
            }
        }

        /// <summary>
        /// Expert: holds a pending (seen but not yet serialized) arc. </summary>
        public class Arc<S>
        {
            public int Label; // really an "unsigned" byte
            public Node Target;
            public bool IsFinal;
            public S Output;
            public S NextFinalOutput;
        }

        // NOTE: not many instances of Node or CompiledNode are in
        // memory while the FST is being built; it's only the
        // current "frontier":

        public interface Node
        {
            bool Compiled { get; }
        }

        public virtual long FstSizeInBytes()
        {
            return Fst.SizeInBytes();
        }

        public sealed class CompiledNode : Node
        {
            public long Node;

            public bool Compiled
            {
                get
                {
                    return true;
                }
            }
        }

        /// <summary>
        /// Expert: holds a pending (seen but not yet serialized) Node. </summary>
        public sealed class UnCompiledNode<S> : Node
        {
            internal readonly Builder<S> Owner;
            public int NumArcs;
            public Arc<S>[] Arcs;

            // TODO: instead of recording isFinal/output on the
            // node, maybe we should use -1 arc to mean "end" (like
            // we do when reading the FST).  Would simplify much
            // code here...
            public S Output;

            public bool IsFinal;
            public long InputCount;

            /// <summary>
            /// this node's depth, starting from the automaton root. </summary>
            public readonly int Depth;

            /// <param name="depth">
            ///          The node's depth starting from the automaton root. Needed for
            ///          LUCENE-2934 (node expansion based on conditions other than the
            ///          fanout size). </param>
            public UnCompiledNode(Builder<S> owner, int depth)
            {
                this.Owner = owner;
                Arcs = (Arc<S>[])new Arc<S>[1];
                Arcs[0] = new Arc<S>();
                Output = owner.NO_OUTPUT;
                this.Depth = depth;
            }

            public bool Compiled
            {
                get
                {
                    return false;
                }
            }

            public void Clear()
            {
                NumArcs = 0;
                IsFinal = false;
                Output = Owner.NO_OUTPUT;
                InputCount = 0;

                // We don't clear the depth here because it never changes
                // for nodes on the frontier (even when reused).
            }

            public S GetLastOutput(int labelToMatch)
            {
                Debug.Assert(NumArcs > 0);
                Debug.Assert(Arcs[NumArcs - 1].Label == labelToMatch);
                return Arcs[NumArcs - 1].Output;
            }

            public void AddArc(int label, Node target)
            {
                Debug.Assert(label >= 0);
                if (NumArcs != 0)
                {
                    Debug.Assert(label > Arcs[NumArcs - 1].Label, "arc[-1].Label=" + Arcs[NumArcs - 1].Label + " new label=" + label + " numArcs=" + NumArcs);
                }
                if (NumArcs == Arcs.Length)
                {
                    Arc<S>[] newArcs = new Arc<S>[ArrayUtil.Oversize(NumArcs + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(Arcs, 0, newArcs, 0, Arcs.Length);
                    for (int arcIdx = NumArcs; arcIdx < newArcs.Length; arcIdx++)
                    {
                        newArcs[arcIdx] = new Arc<S>();
                    }
                    Arcs = newArcs;
                }
                Arc<S> arc = Arcs[NumArcs++];
                arc.Label = label;
                arc.Target = target;
                arc.Output = arc.NextFinalOutput = Owner.NO_OUTPUT;
                arc.IsFinal = false;
            }

            public void ReplaceLast(int labelToMatch, Node target, S nextFinalOutput, bool isFinal)
            {
                Debug.Assert(NumArcs > 0);
                Arc<S> arc = Arcs[NumArcs - 1];
                Debug.Assert(arc.Label == labelToMatch, "arc.Label=" + arc.Label + " vs " + labelToMatch);
                arc.Target = target;
                //assert target.Node != -2;
                arc.NextFinalOutput = nextFinalOutput;
                arc.IsFinal = isFinal;
            }

            public void DeleteLast(int label, Node target)
            {
                Debug.Assert(NumArcs > 0);
                Debug.Assert(label == Arcs[NumArcs - 1].Label);
                Debug.Assert(target == Arcs[NumArcs - 1].Target);
                NumArcs--;
            }

            public void SetLastOutput(int labelToMatch, S newOutput)
            {
                Debug.Assert(Owner.ValidOutput(newOutput));
                Debug.Assert(NumArcs > 0);
                Arc<S> arc = Arcs[NumArcs - 1];
                Debug.Assert(arc.Label == labelToMatch);
                arc.Output = newOutput;
            }

            // pushes an output prefix forward onto all arcs
            public void PrependOutput(S outputPrefix)
            {
                Debug.Assert(Owner.ValidOutput(outputPrefix));

                for (int arcIdx = 0; arcIdx < NumArcs; arcIdx++)
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