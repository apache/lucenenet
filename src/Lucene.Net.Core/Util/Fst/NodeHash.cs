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

    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedGrowableWriter = Lucene.Net.Util.Packed.PagedGrowableWriter;

    // Used to dedup states (lookup already-frozen states)
    internal sealed class NodeHash<T>
    {
        private PagedGrowableWriter Table;
        private long Count;
        private long Mask;
        private readonly FST<T> Fst;
        private readonly FST<T>.Arc<T> ScratchArc = new FST<T>.Arc<T>();
        private readonly FST.BytesReader @in;

        public NodeHash(FST<T> fst, FST.BytesReader @in)
        {
            Table = new PagedGrowableWriter(16, 1 << 30, 8, PackedInts.COMPACT);
            Mask = 15;
            this.Fst = fst;
            this.@in = @in;
        }

        private bool NodesEqual(Builder<T>.UnCompiledNode<T> node, long address)
        {
            Fst.ReadFirstRealTargetArc(address, ScratchArc, @in);
            if (ScratchArc.BytesPerArc != 0 && node.NumArcs != ScratchArc.NumArcs)
            {
                return false;
            }
            for (int arcUpto = 0; arcUpto < node.NumArcs; arcUpto++)
            {
                Builder<T>.Arc<T> arc = node.Arcs[arcUpto];
                if (arc.Label != ScratchArc.Label || !arc.Output.Equals(ScratchArc.Output) || ((Builder<T>.CompiledNode)arc.Target).Node != ScratchArc.Target || !arc.NextFinalOutput.Equals(ScratchArc.NextFinalOutput) || arc.IsFinal != ScratchArc.Final)
                {
                    return false;
                }

                if (ScratchArc.Last)
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
                Fst.ReadNextRealArc(ScratchArc, @in);
            }

            return false;
        }

        // hash code for an unfrozen node.  this must be identical
        // to the frozen case (below)!!
        private long Hash(Builder<T>.UnCompiledNode<T> node)
        {
            const int PRIME = 31;
            //System.out.println("hash unfrozen");
            long h = 0;
            // TODO: maybe if number of arcs is high we can safely subsample?
            for (int arcIdx = 0; arcIdx < node.NumArcs; arcIdx++)
            {
                Builder<T>.Arc<T> arc = node.Arcs[arcIdx];
                h = PRIME * h + arc.Label;
                long n = ((Builder<T>.CompiledNode)arc.Target).Node;
                h = PRIME * h + (int)((n ^ (n >> 32)) >> 32);
                var arcOutputHashCode = arc.Output.GetHashCode();
                h = PRIME * h + arcOutputHashCode;
                var arcFinalOutputHashCode = arc.NextFinalOutput.GetHashCode();
                h = PRIME * h + arcFinalOutputHashCode;
                if (arc.IsFinal)
                {
                    h += 17;
                }
            }
            //System.out.println("  ret " + (h&Integer.MAX_VALUE));
            return h & long.MaxValue;
        }

        // hash code for a frozen node
        private long Hash(long node)
        {
            const int PRIME = 31;
            //System.out.println("hash frozen node=" + node);
            long h = 0;
            Fst.ReadFirstRealTargetArc(node, ScratchArc, @in);
            while (true)
            {
                //System.out.println("  label=" + scratchArc.label + " target=" + scratchArc.target + " h=" + h + " output=" + fst.outputs.outputToString(scratchArc.output) + " next?=" + scratchArc.flag(4) + " final?=" + scratchArc.isFinal() + " pos=" + in.getPosition());
                h = PRIME * h + ScratchArc.Label;
                //Force truncation by shifting at the end
                h = PRIME * h + (int)((ScratchArc.Target ^ (ScratchArc.Target >> 32)) >> 32);
                var sractchArcHashCode = ScratchArc.Output.GetHashCode();
                h = PRIME * h + sractchArcHashCode;
                var scratchArcFinalOutputHashCode = ScratchArc.NextFinalOutput.GetHashCode();
                h = PRIME * h + scratchArcFinalOutputHashCode;
                if (ScratchArc.Final)
                {
                    h += 17;
                }
                if (ScratchArc.Last)
                {
                    break;
                }
                Fst.ReadNextRealArc(ScratchArc, @in);
            }
            //System.out.println("  ret " + (h&Integer.MAX_VALUE));
            return h & long.MaxValue;
        }

        public long Add(Builder<T>.UnCompiledNode<T> nodeIn)
        {
            //System.out.println("hash: add count=" + count + " vs " + table.size() + " mask=" + mask);
            long h = Hash(nodeIn);
            long pos = h & Mask;
            int c = 0;
            while (true)
            {
                long v = Table.Get(pos);
                if (v == 0)
                {
                    // freeze & add
                    long node = Fst.AddNode(nodeIn);
                    //System.out.println("  now freeze node=" + node);
                    long hashNode = Hash(node);
                    Debug.Assert(hashNode == h, "frozenHash=" + hashNode + " vs h=" + h);
                    Count++;
                    Table.Set(pos, node);
                    // Rehash at 2/3 occupancy:
                    if (Count > 2 * Table.Size() / 3)
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
                pos = (pos + (++c)) & Mask;
            }
        }

        // called only by rehash
        private void AddNew(long address)
        {
            long pos = Hash(address) & Mask;
            int c = 0;
            while (true)
            {
                if (Table.Get(pos) == 0)
                {
                    Table.Set(pos, address);
                    break;
                }

                // quadratic probe
                pos = (pos + (++c)) & Mask;
            }
        }

        private void Rehash()
        {
            PagedGrowableWriter oldTable = Table;

            Table = new PagedGrowableWriter(2 * oldTable.Size(), 1 << 30, PackedInts.BitsRequired(Count), PackedInts.COMPACT);
            Mask = Table.Size() - 1;
            for (long idx = 0; idx < oldTable.Size(); idx++)
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