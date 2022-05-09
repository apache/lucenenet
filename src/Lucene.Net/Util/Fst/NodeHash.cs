using J2N.Collections;
using Lucene.Net.Diagnostics;
using System.Runtime.CompilerServices;
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

    using PackedInt32s = Lucene.Net.Util.Packed.PackedInt32s;
    using PagedGrowableWriter = Lucene.Net.Util.Packed.PagedGrowableWriter;

    /// <summary>
    /// Used to dedup states (lookup already-frozen states)
    /// </summary>
    internal sealed class NodeHash<T>
        where T : class // LUCENENET specific - added class constraint, since we compare reference equality
    {
        private PagedGrowableWriter table;
        private long count;
        private long mask;
        private readonly FST<T> fst;
        private readonly FST.Arc<T> scratchArc = new FST.Arc<T>();
        private readonly FST.BytesReader input;

        // LUCENENET specific - optimize the Hash methods
        // by only calling StructuralEqualityComparer.GetHashCode() if the value is a reference type
        private readonly static bool tIsValueType = typeof(T).IsValueType;

        public NodeHash(FST<T> fst, FST.BytesReader input)
        {
            table = new PagedGrowableWriter(16, 1 << 30, 8, PackedInt32s.COMPACT);
            mask = 15;
            this.fst = fst;
            this.input = input;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool NodesEqual(Builder.UnCompiledNode<T> node, long address)
        {
            fst.ReadFirstRealTargetArc(address, scratchArc, input);
            if (scratchArc.BytesPerArc != 0 && node.NumArcs != scratchArc.NumArcs)
            {
                return false;
            }
            for (int arcUpto = 0; arcUpto < node.NumArcs; arcUpto++)
            {
                Builder.Arc<T> arc = node.Arcs[arcUpto];
                if (arc.IsFinal != scratchArc.IsFinal ||
                    arc.Label != scratchArc.Label ||
                    ((Builder.CompiledNode)arc.Target).Node != scratchArc.Target ||
                    !(tIsValueType ? JCG.EqualityComparer<T>.Default.Equals(arc.Output, scratchArc.Output) : StructuralEqualityComparer.Default.Equals(arc.Output, scratchArc.Output)) || 
                    !(tIsValueType ? JCG.EqualityComparer<T>.Default.Equals(arc.NextFinalOutput, scratchArc.NextFinalOutput) : StructuralEqualityComparer.Default.Equals(arc.NextFinalOutput, scratchArc.NextFinalOutput))
                    )
                {
                    return false;
                }

                if (scratchArc.IsLast)
                {
                    if (arcUpto == node.NumArcs - 1)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                fst.ReadNextRealArc(scratchArc, input);
            }

            return false;
        }

        /// <summary>
        /// hash code for an unfrozen node.  this must be identical
        /// to the frozen case (below)!!
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long Hash(Builder.UnCompiledNode<T> node) // LUCENENET: CA1822: Mark members as static
        {
            const int PRIME = 31;
            //System.out.println("hash unfrozen");
            long h = 0;
            // TODO: maybe if number of arcs is high we can safely subsample?
            for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
            {
                Builder.Arc<T> arc = node.Arcs[arcIdx];
                h = PRIME * h + arc.Label;
                long n = ((Builder.CompiledNode)arc.Target).Node;
                h = PRIME * h + (int)(n ^ (n >> 32));

                // LUCENENET specific - optimize the Hash methods
                // by only calling StructuralEqualityComparer.GetHashCode() if the value is a reference type
                h = PRIME * h + (tIsValueType ? JCG.EqualityComparer<T>.Default.GetHashCode(arc.Output) : StructuralEqualityComparer.Default.GetHashCode(arc.Output));
                h = PRIME * h + (tIsValueType ? JCG.EqualityComparer<T>.Default.GetHashCode(arc.NextFinalOutput) : StructuralEqualityComparer.Default.GetHashCode(arc.NextFinalOutput));
                if (arc.IsFinal)
                {
                    h += 17;
                }
            }
            //System.out.println("  ret " + (h&Integer.MAX_VALUE));
            return h & long.MaxValue;
        }

        /// <summary>
        /// hash code for a frozen node
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private long Hash(long node)
        {
            const int PRIME = 31;
            //System.out.println("hash frozen node=" + node);
            long h = 0;
            fst.ReadFirstRealTargetArc(node, scratchArc, input);
            while (true)
            {
                //System.out.println("  label=" + scratchArc.label + " target=" + scratchArc.target + " h=" + h + " output=" + fst.outputs.outputToString(scratchArc.output) + " next?=" + scratchArc.flag(4) + " final?=" + scratchArc.isFinal() + " pos=" + in.getPosition());
                h = PRIME * h + scratchArc.Label;
                h = PRIME * h + (int)(scratchArc.Target ^ (scratchArc.Target >> 32));

                // LUCENENET specific - optimize the Hash methods
                // by only calling StructuralEqualityComparer.Default.GetHashCode() if the value is a reference type
                h = PRIME * h + (tIsValueType ? JCG.EqualityComparer<T>.Default.GetHashCode(scratchArc.Output) : StructuralEqualityComparer.Default.GetHashCode(scratchArc.Output));
                h = PRIME * h + (tIsValueType ? JCG.EqualityComparer<T>.Default.GetHashCode(scratchArc.NextFinalOutput) : StructuralEqualityComparer.Default.GetHashCode(scratchArc.NextFinalOutput));
                if (scratchArc.IsFinal)
                {
                    h += 17;
                }
                if (scratchArc.IsLast)
                {
                    break;
                }
                fst.ReadNextRealArc(scratchArc, input);
            }
            //System.out.println("  ret " + (h&Integer.MAX_VALUE));
            return h & long.MaxValue;
        }

        public long Add(Builder.UnCompiledNode<T> nodeIn)
        {
            //System.out.println("hash: add count=" + count + " vs " + table.size() + " mask=" + mask);
            long h = Hash(nodeIn);
            long pos = h & mask;
            int c = 0;
            while (true)
            {
                long v = table.Get(pos);
                if (v == 0)
                {
                    // freeze & add
                    long node = fst.AddNode(nodeIn);
                    //System.out.println("  now freeze node=" + node);
                    if (Debugging.AssertsEnabled)
                    {
                        // LUCENENET specific - store hash value and reuse it, since it might be expensive to create
                        long hash = Hash(node);
                        Debugging.Assert(hash == h, "frozenHash={0} vs h={1}", hash, h);
                    }
                    count++;
                    table.Set(pos, node);
                    // Rehash at 2/3 occupancy:
                    if (count > 2 * table.Count / 3)
                    {
                        Rehash();
                    }
                    return node;
                }
                else if (NodesEqual(nodeIn, v))
                {
                    // same node is already here
                    return v;
                }

                // quadratic probe
                pos = (pos + (++c)) & mask;
            }
        }

        /// <summary>
        /// called only by rehash
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddNew(long address)
        {
            long pos = Hash(address) & mask;
            int c = 0;
            while (true)
            {
                if (table.Get(pos) == 0)
                {
                    table.Set(pos, address);
                    break;
                }

                // quadratic probe
                pos = (pos + (++c)) & mask;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Rehash()
        {
            PagedGrowableWriter oldTable = table;

            table = new PagedGrowableWriter(2 * oldTable.Count, 1 << 30, PackedInt32s.BitsRequired(count), PackedInt32s.COMPACT);
            mask = table.Count - 1;
            for (long idx = 0; idx < oldTable.Count; idx++)
            {
                long address = oldTable.Get(idx);
                if (address != 0)
                {
                    AddNew(address);
                }
            }
        }
    }
}