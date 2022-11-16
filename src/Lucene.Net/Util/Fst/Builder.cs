using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;

    // TODO: could we somehow stream an FST to disk while we
    // build it?

    /// <summary>
    /// Builds a minimal FST (maps an <see cref="Int32sRef"/> term to an arbitrary
    /// output) from pre-sorted terms with outputs.  The FST
    /// becomes an FSA if you use NoOutputs.  The FST is written
    /// on-the-fly into a compact serialized format byte array, which can
    /// be saved to / loaded from a Directory or used directly
    /// for traversal.  The FST is always finite (no cycles).
    ///
    /// <para/>NOTE: The algorithm is described at
    /// http://citeseerx.ist.psu.edu/viewdoc/summary?doi=10.1.1.24.3698
    ///
    /// <para/>The parameterized type <typeparam name="T"/> is the output type.  See the
    /// subclasses of <see cref="Outputs{T}"/>.
    ///
    /// <para/>FSTs larger than 2.1GB are now possible (as of Lucene
    /// 4.2).  FSTs containing more than 2.1B nodes are also now
    /// possible, however they cannot be packed.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class Builder<T> : Builder
        where T : class // LUCENENET specific - added class constraint, since we compare reference equality
    {
        private readonly NodeHash<T> dedupHash;
        private readonly FST<T> fst;
        private readonly T NO_OUTPUT;

        // private static final boolean DEBUG = true;

        // simplistic pruning: we prune node (and all following
        // nodes) if less than this number of terms go through it:
        private readonly int minSuffixCount1;

        // better pruning: we prune node (and all following
        // nodes) if the prior node has less than this number of
        // terms go through it:
        private readonly int minSuffixCount2;

        private readonly bool doShareNonSingletonNodes;
        private readonly int shareMaxTailLength;

        private readonly Int32sRef lastInput = new Int32sRef();

        // for packing
        private readonly bool doPackFST;

        private readonly float acceptableOverheadRatio;

        // NOTE: cutting this over to ArrayList instead loses ~6%
        // in build performance on 9.8M Wikipedia terms; so we
        // left this as an array:
        // current "frontier"
        private UnCompiledNode<T>[] frontier;

        // LUCENENET specific - FreezeTail class moved to non-generic type Builder

        private readonly FreezeTail<T> freezeTail;

        // LUCENENET specific - allow external access through property
        internal FST<T> Fst => fst;

        internal T NoOutput => NO_OUTPUT;


        /// <summary>
        /// Instantiates an FST/FSA builder without any pruning. A shortcut
        /// to <see cref="Builder{T}.Builder(FST.INPUT_TYPE, int, int, bool, bool, int, Outputs{T}, FreezeTail{T}, bool, float, bool, int)"/>
        /// with pruning options turned off.
        /// </summary>
        public Builder(FST.INPUT_TYPE inputType, Outputs<T> outputs)
            : this(inputType, 0, 0, true, true, int.MaxValue, outputs, null, false, PackedInt32s.COMPACT, true, 15)
        {
        }

        /// <summary>
        /// Instantiates an FST/FSA builder with all the possible tuning and construction
        /// tweaks. Read parameter documentation carefully.
        /// </summary>
        /// <param name="inputType">
        ///    The input type (transition labels). Can be anything from <see cref="Lucene.Net.Util.Fst.FST.INPUT_TYPE"/>
        ///    enumeration. Shorter types will consume less memory. Strings (character sequences) are
        ///    represented as <see cref="Lucene.Net.Util.Fst.FST.INPUT_TYPE.BYTE4"/> (full unicode codepoints).
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
        ///    If <c>true</c>, the shared suffixes will be compacted into unique paths.
        ///    this requires an additional RAM-intensive hash map for lookups in memory. Setting this parameter to
        ///    <c>false</c> creates a single suffix path for all input sequences. this will result in a larger
        ///    FST, but requires substantially less memory and CPU during building.
        /// </param>
        /// <param name="doShareNonSingletonNodes">
        ///    Only used if <paramref name="doShareSuffix"/> is <c>true</c>.  Set this to
        ///    true to ensure FST is fully minimal, at cost of more
        ///    CPU and more RAM during building.
        /// </param>
        /// <param name="shareMaxTailLength">
        ///    Only used if <paramref name="doShareSuffix"/> is <c>true</c>.  Set this to
        ///    <see cref="int.MaxValue"/> to ensure FST is fully minimal, at cost of more
        ///    CPU and more RAM during building.
        /// </param>
        /// <param name="outputs"> The output type for each input sequence. Applies only if building an FST. For
        ///    FSA, use <see cref="NoOutputs.Singleton"/> and <see cref="NoOutputs.NoOutput"/> as the
        ///    singleton output object.
        /// </param>
        /// <param name="doPackFST"> Pass <c>true</c> to create a packed FST.
        /// </param>
        /// <param name="acceptableOverheadRatio"> How to trade speed for space when building the FST. this option 
        ///    is only relevant when doPackFST is true. <see cref="PackedInt32s.GetMutable(int, int, float)"/>
        /// </param>
        /// <param name="allowArrayArcs"> Pass false to disable the array arc optimization
        ///    while building the FST; this will make the resulting
        ///    FST smaller but slower to traverse.
        /// </param>
        /// <param name="bytesPageBits"> How many bits wide to make each
        ///    <see cref="T:byte[]"/> block in the <see cref="BytesStore"/>; if you know the FST
        ///    will be large then make this larger.  For example 15
        ///    bits = 32768 byte pages. </param>
        public Builder(FST.INPUT_TYPE inputType, int minSuffixCount1, int minSuffixCount2, bool doShareSuffix, 
            bool doShareNonSingletonNodes, int shareMaxTailLength, Outputs<T> outputs, 
            FreezeTail<T> freezeTail, bool doPackFST, float acceptableOverheadRatio, bool allowArrayArcs, 
            int bytesPageBits)
        {
            this.minSuffixCount1 = minSuffixCount1;
            this.minSuffixCount2 = minSuffixCount2;
            this.freezeTail = freezeTail;
            this.doShareNonSingletonNodes = doShareNonSingletonNodes;
            this.shareMaxTailLength = shareMaxTailLength;
            this.doPackFST = doPackFST;
            this.acceptableOverheadRatio = acceptableOverheadRatio;
            fst = new FST<T>(inputType, outputs, doPackFST, acceptableOverheadRatio, allowArrayArcs, bytesPageBits);
            if (doShareSuffix)
            {
                dedupHash = new NodeHash<T>(fst, fst.bytes.GetReverseReader(false));
            }
            else
            {
                dedupHash = null;
            }
            NO_OUTPUT = outputs.NoOutput;

            UnCompiledNode<T>[] f = (UnCompiledNode<T>[])new UnCompiledNode<T>[10];
            frontier = f;
            for (int idx = 0; idx < frontier.Length; idx++)
            {
                frontier[idx] = new UnCompiledNode<T>(this, idx);
            }
        }

        public virtual long TotStateCount => fst.nodeCount;

        public virtual long TermCount => frontier[0].InputCount;

        public virtual long MappedStateCount => dedupHash is null ? 0 : fst.nodeCount;

        private CompiledNode CompileNode(UnCompiledNode<T> nodeIn, int tailLength)
        {
            long node;
            if (dedupHash != null && (doShareNonSingletonNodes || nodeIn.NumArcs <= 1) && tailLength <= shareMaxTailLength)
            {
                if (nodeIn.NumArcs == 0)
                {
                    node = fst.AddNode(nodeIn);
                }
                else
                {
                    node = dedupHash.Add(nodeIn);
                }
            }
            else
            {
                node = fst.AddNode(nodeIn);
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(node != -2);

            nodeIn.Clear();

            return new CompiledNode { Node = node };
        }

        private void DoFreezeTail(int prefixLenPlus1)
        {
            if (freezeTail != null)
            {
                // Custom plugin:
                freezeTail.Freeze(frontier, prefixLenPlus1, lastInput);
            }
            else
            {
                //System.out.println("  compileTail " + prefixLenPlus1);
                int downTo = Math.Max(1, prefixLenPlus1);
                for (int idx = lastInput.Length; idx >= downTo; idx--)
                {
                    bool doPrune = false;
                    bool doCompile /*= false*/; // LUCENENET: Removed unnecessary assignment

                    UnCompiledNode<T> node = frontier[idx];
                    UnCompiledNode<T> parent = frontier[idx - 1];

                    if (node.InputCount < minSuffixCount1)
                    {
                        doPrune = true;
                        doCompile = true;
                    }
                    else if (idx > prefixLenPlus1)
                    {
                        // prune if parent's inputCount is less than suffixMinCount2
                        if (parent.InputCount < minSuffixCount2 || (minSuffixCount2 == 1 && parent.InputCount == 1 && idx > 1))
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
                        doCompile = minSuffixCount2 == 0;
                    }

                    //System.out.println("    label=" + ((char) lastInput.ints[lastInput.offset+idx-1]) + " idx=" + idx + " inputCount=" + frontier[idx].inputCount + " doCompile=" + doCompile + " doPrune=" + doPrune);

                    if (node.InputCount < minSuffixCount2 || (minSuffixCount2 == 1 && node.InputCount == 1 && idx > 1))
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
                        parent.DeleteLast(lastInput.Int32s[lastInput.Offset + idx - 1], node);
                    }
                    else
                    {
                        if (minSuffixCount2 != 0)
                        {
                            CompileAllTargets(node, lastInput.Length - idx);
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
                            parent.ReplaceLast(lastInput.Int32s[lastInput.Offset + idx - 1], CompileNode(node, 1 + lastInput.Length - idx), nextFinalOutput, isFinal);
                        }
                        else
                        {
                            // replaceLast just to install
                            // nextFinalOutput/isFinal onto the arc
                            parent.ReplaceLast(lastInput.Int32s[lastInput.Offset + idx - 1], node, nextFinalOutput, isFinal);
                            // this node will stay in play for now, since we are
                            // undecided on whether to prune it.  later, it
                            // will be either compiled or pruned, so we must
                            // allocate a new node:
                            frontier[idx] = new UnCompiledNode<T>(this, idx);
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
        /// different outputs, as long as outputs impls the merge
        /// method. Note that input is fully consumed after this
        /// method is returned (so caller is free to reuse), but
        /// output is not.  So if your outputs are changeable (eg
        /// <see cref="ByteSequenceOutputs"/> or 
        /// <see cref="Int32SequenceOutputs"/>) then you cannot reuse across
        /// calls.
        /// </summary>
        public virtual void Add(Int32sRef input, T output)
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

            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(lastInput.Length == 0 || input.CompareTo(lastInput) >= 0, "inputs are added out of order lastInput={0} vs input={1}", lastInput, input);
                Debugging.Assert(ValidOutput(output));
            }

            //System.out.println("\nadd: " + input);
            if (input.Length == 0)
            {
                // empty input: only allowed as first input.  we have
                // to special case this because the packed FST
                // format cannot represent the empty input since
                // 'finalness' is stored on the incoming arc, not on
                // the node
                frontier[0].InputCount++;
                frontier[0].IsFinal = true;
                fst.EmptyOutput = output;
                return;
            }

            // compare shared prefix length
            int pos1 = 0;
            int pos2 = input.Offset;
            int pos1Stop = Math.Min(lastInput.Length, input.Length);
            while (true)
            {
                frontier[pos1].InputCount++;
                //System.out.println("  incr " + pos1 + " ct=" + frontier[pos1].inputCount + " n=" + frontier[pos1]);
                if (pos1 >= pos1Stop || lastInput.Int32s[pos1] != input.Int32s[pos2])
                {
                    break;
                }
                pos1++;
                pos2++;
            }
            int prefixLenPlus1 = pos1 + 1;

            if (frontier.Length < input.Length + 1)
            {
                UnCompiledNode<T>[] next = new UnCompiledNode<T>[ArrayUtil.Oversize(input.Length + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Arrays.Copy(frontier, 0, next, 0, frontier.Length);
                for (int idx = frontier.Length; idx < next.Length; idx++)
                {
                    next[idx] = new UnCompiledNode<T>(this, idx);
                }
                frontier = next;
            }

            // minimize/compile states from previous input's
            // orphan'd suffix
            DoFreezeTail(prefixLenPlus1);

            // init tail states for current input
            for (int idx = prefixLenPlus1; idx <= input.Length; idx++)
            {
                frontier[idx - 1].AddArc(input.Int32s[input.Offset + idx - 1], frontier[idx]);
                frontier[idx].InputCount++;
            }

            UnCompiledNode<T> lastNode = frontier[input.Length];
            if (lastInput.Length != input.Length || prefixLenPlus1 != input.Length + 1)
            {
                lastNode.IsFinal = true;
                lastNode.Output = NO_OUTPUT;
            }

            // push conflicting outputs forward, only as far as
            // needed
            for (int idx = 1; idx < prefixLenPlus1; idx++)
            {
                UnCompiledNode<T> node = frontier[idx];
                UnCompiledNode<T> parentNode = frontier[idx - 1];

                T lastOutput = parentNode.GetLastOutput(input.Int32s[input.Offset + idx - 1]);
                if (Debugging.AssertsEnabled) Debugging.Assert(ValidOutput(lastOutput));

                T commonOutputPrefix;
                T wordSuffix;

                if (lastOutput != NO_OUTPUT)
                {
                    commonOutputPrefix = fst.Outputs.Common(output, lastOutput);
                    if (Debugging.AssertsEnabled) Debugging.Assert(ValidOutput(commonOutputPrefix));
                    wordSuffix = fst.Outputs.Subtract(lastOutput, commonOutputPrefix);
                    if (Debugging.AssertsEnabled) Debugging.Assert(ValidOutput(wordSuffix));
                    parentNode.SetLastOutput(input.Int32s[input.Offset + idx - 1], commonOutputPrefix);
                    node.PrependOutput(wordSuffix);
                }
                else
                {
                    commonOutputPrefix = /*wordSuffix =*/ NO_OUTPUT; // LUCENENET: Removed unnecessary assignment
                }

                output = fst.Outputs.Subtract(output, commonOutputPrefix);
                if (Debugging.AssertsEnabled) Debugging.Assert(ValidOutput(output));
            }

            if (lastInput.Length == input.Length && prefixLenPlus1 == 1 + input.Length)
            {
                // same input more than 1 time in a row, mapping to
                // multiple outputs
                lastNode.Output = fst.Outputs.Merge(lastNode.Output, output);
            }
            else
            {
                // this new arc is private to this new input; set its
                // arc output to the leftover output:
                frontier[prefixLenPlus1 - 1].SetLastOutput(input.Int32s[input.Offset + prefixLenPlus1 - 1], output);
            }

            // save last input
            lastInput.CopyInt32s(input);

            //System.out.println("  count[0]=" + frontier[0].inputCount);
        }

        internal bool ValidOutput(T output) // Only called from assert
        {
            return output == NO_OUTPUT || !output.Equals(NO_OUTPUT);
        }

        /// <summary>
        /// Returns final FST.  NOTE: this will return null if
        /// nothing is accepted by the FST.
        /// </summary>
        public virtual FST<T> Finish()
        {
            UnCompiledNode<T> root = frontier[0];

            // minimize nodes in the last word's suffix
            DoFreezeTail(0);
            if (root.InputCount < minSuffixCount1 || root.InputCount < minSuffixCount2 || root.NumArcs == 0)
            {
                if (fst.emptyOutput is null)
                {
                    return null;
                }
                else if (minSuffixCount1 > 0 || minSuffixCount2 > 0)
                {
                    // empty string got pruned
                    return null;
                }
            }
            else
            {
                if (minSuffixCount2 != 0)
                {
                    CompileAllTargets(root, lastInput.Length);
                }
            }
            //if (DEBUG) System.out.println("  builder.finish root.isFinal=" + root.isFinal + " root.Output=" + root.Output);
            fst.Finish(CompileNode(root, lastInput.Length).Node);

            if (doPackFST)
            {
                return fst.Pack(3, Math.Max(10, (int)(fst.NodeCount / 4)), acceptableOverheadRatio);
            }
            else
            {
                return fst;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CompileAllTargets(UnCompiledNode<T> node, int tailLength)
        {
            for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
            {
                Arc<T> arc = node.Arcs[arcIdx];
                if (!arc.Target.IsCompiled)
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

        // LUCENENET specific: moved Arc<S> to Builder type

        // NOTE: not many instances of Node or CompiledNode are in
        // memory while the FST is being built; it's only the
        // current "frontier":

        // LUCENENET specific: moved INode to Builder type

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual long GetFstSizeInBytes()
        {
            return fst.GetSizeInBytes();
        }

        // LUCENENET specific: Moved CompiledNode and UncompiledNode to Builder class
    }

    /// <summary>
    /// LUCENENET specific type used to access nested types of <see cref="Builder{T}"/>
    /// without referring to its generic closing type.
    /// </summary>
    public abstract class Builder
    {
        private protected Builder() { } // Disallow external creation

        /// <summary>
        /// Expert: this is invoked by Builder whenever a suffix
        ///  is serialized.
        /// </summary>
        public abstract class FreezeTail<S> where S : class // LUCNENET specific - added class constraint because we compare reference equality
        {
            public abstract void Freeze(UnCompiledNode<S>[] frontier, int prefixLenPlus1, Int32sRef prevInput);
        }

        public interface INode // LUCENENET NOTE: made public rather than internal because it is returned in public types
        {
            bool IsCompiled { get; }
        }

        /// <summary>
        /// Expert: holds a pending (seen but not yet serialized) arc. </summary>
        public class Arc<S> where S : class // LUCENENET specific - added class constraint because we compare reference equality
        {
            public int Label { get; set; } // really an "unsigned" byte
            public INode Target { get; set; }
            public bool IsFinal { get; set; }
            public S Output { get; set; }
            public S NextFinalOutput { get; set; }
        }

        public sealed class CompiledNode : INode
        {
            public long Node { get; set; }

            public bool IsCompiled => true;
        }

        /// <summary>
        /// Expert: holds a pending (seen but not yet serialized) Node. </summary>
        public sealed class UnCompiledNode<S> : INode where S : class // LUCENENET specific - added class constraint, since we compare reference equality
        {
            internal Builder<S> Owner { get; private set; }
            public int NumArcs { get; set; }

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public Arc<S>[] Arcs { get; set; }

            // TODO: instead of recording isFinal/output on the
            // node, maybe we should use -1 arc to mean "end" (like
            // we do when reading the FST).  Would simplify much
            // code here...
            public S Output { get; set; }

            public bool IsFinal { get; set; }
            public long InputCount { get; set; }

            /// <summary>
            /// this node's depth, starting from the automaton root. </summary>
            public int Depth { get; private set; }

            /// <param name="depth">
            ///          The node's depth starting from the automaton root. Needed for
            ///          LUCENE-2934 (node expansion based on conditions other than the
            ///          fanout size). </param>
            public UnCompiledNode(Builder<S> owner, int depth)
            {
                this.Owner = owner;
                Arcs = (Arc<S>[])new Arc<S>[1];
                Arcs[0] = new Arc<S>();
                Output = owner.NoOutput;
                this.Depth = depth;
            }

            public bool IsCompiled => false;

            public void Clear()
            {
                NumArcs = 0;
                IsFinal = false;
                Output = Owner.NoOutput;
                InputCount = 0;

                // We don't clear the depth here because it never changes
                // for nodes on the frontier (even when reused).
            }

            public S GetLastOutput(int labelToMatch)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(NumArcs > 0);
                    Debugging.Assert(Arcs[NumArcs - 1].Label == labelToMatch);
                }
                return Arcs[NumArcs - 1].Output;
            }

            public void AddArc(int label, INode target)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(label >= 0);
                if (NumArcs != 0)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(label > Arcs[NumArcs - 1].Label, "arc[-1].Label={0} new label={1} numArcs={2}", Arcs[NumArcs - 1].Label, label, NumArcs);
                }
                if (NumArcs == Arcs.Length)
                {
                    Arc<S>[] newArcs = new Arc<S>[ArrayUtil.Oversize(NumArcs + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Arrays.Copy(Arcs, 0, newArcs, 0, Arcs.Length);
                    for (int arcIdx = NumArcs; arcIdx < newArcs.Length; arcIdx++)
                    {
                        newArcs[arcIdx] = new Arc<S>();
                    }
                    Arcs = newArcs;
                }
                Arc<S> arc = Arcs[NumArcs++];
                arc.Label = label;
                arc.Target = target;
                arc.Output = arc.NextFinalOutput = Owner.NoOutput;
                arc.IsFinal = false;
            }

            public void ReplaceLast(int labelToMatch, INode target, S nextFinalOutput, bool isFinal)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(NumArcs > 0);
                Arc<S> arc = Arcs[NumArcs - 1];
                if (Debugging.AssertsEnabled) Debugging.Assert(arc.Label == labelToMatch,"arc.Label={0} vs {1}", arc.Label, labelToMatch);
                arc.Target = target;
                //assert target.Node != -2;
                arc.NextFinalOutput = nextFinalOutput;
                arc.IsFinal = isFinal;
            }

            public void DeleteLast(int label, INode target)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(NumArcs > 0);
                    Debugging.Assert(label == Arcs[NumArcs - 1].Label);
                    Debugging.Assert(target == Arcs[NumArcs - 1].Target);
                }
                NumArcs--;
            }

            public void SetLastOutput(int labelToMatch, S newOutput)
            {
                if (Debugging.AssertsEnabled)
                {
                    Debugging.Assert(Owner.ValidOutput(newOutput));
                    Debugging.Assert(NumArcs > 0);
                }
                Arc<S> arc = Arcs[NumArcs - 1];
                if (Debugging.AssertsEnabled) Debugging.Assert(arc.Label == labelToMatch);
                arc.Output = newOutput;
            }

            // pushes an output prefix forward onto all arcs
            public void PrependOutput(S outputPrefix)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(Owner.ValidOutput(outputPrefix));

                for (int arcIdx = 0; arcIdx < NumArcs; arcIdx++)
                {
                    Arcs[arcIdx].Output = Owner.Fst.Outputs.Add(outputPrefix, Arcs[arcIdx].Output);
                    if (Debugging.AssertsEnabled) Debugging.Assert(Owner.ValidOutput(Arcs[arcIdx].Output));
                }

                if (IsFinal)
                {
                    Output = Owner.Fst.Outputs.Add(outputPrefix, Output);
                    if (Debugging.AssertsEnabled) Debugging.Assert(Owner.ValidOutput(Output));
                }
            }
        }
    }
}