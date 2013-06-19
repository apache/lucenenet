using System;
using Lucene.Net.Util.Packed;

namespace Lucene.Net.Util.Fst
{
    internal sealed class NodeHash<T>
    {

        private GrowableWriter table;
        private int count;
        private int mask;
        private readonly FST<T> fst;
        private readonly FST<T>.Arc<T> scratchArc = new FST<T>.Arc<T>();
        private readonly FST.BytesReader input;

        public NodeHash(FST<T> fst, FST.BytesReader input)
        {
            table = new GrowableWriter(8, 16, PackedInts.COMPACT);
            mask = 15;
            this.fst = fst;
            this.input = input;
        }

        private bool NodesEqual(Builder<T>.UnCompiledNode<T> Node, long address)
        {
            fst.ReadFirstRealTargetArc(address, scratchArc, input);
            if (scratchArc.BytesPerArc != 0 && Node.NumArcs != scratchArc.NumArcs)
            {
                return false;
            }
            for (int arcUpto = 0; arcUpto < Node.NumArcs; arcUpto++)
            {
                Builder<T>.Arc<T> arc = Node.Arcs[arcUpto];
                if (arc.Label != scratchArc.Label ||
                    !arc.Output.Equals(scratchArc.Output) ||
                    ((Builder<T>.CompiledNode)arc.Target).Node != scratchArc.Target ||
                    !arc.NextFinalOutput.Equals(scratchArc.NextFinalOutput) ||
                    arc.IsFinal != scratchArc.IsFinal())
                {
                    return false;
                }

                if (scratchArc.IsLast())
                {
                    if (arcUpto == Node.NumArcs - 1)
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

        private int Hash(Builder<T>.UnCompiledNode<T> Node)
        {
            var PRIME = 31;
            var h = 0;
            for (var arcIdx = 0; arcIdx < Node.NumArcs; arcIdx++)
            {
                var arc = Node.Arcs[arcIdx];
                h = PRIME * h + arc.Label;
                long n = ((Builder<T>.CompiledNode)arc.Target).Node;
                h = PRIME * h + (int)(n ^ (n >> 32));
                h = PRIME * h + arc.Output.GetHashCode();
                h = PRIME * h + arc.NextFinalOutput.GetHashCode();
                if (arc.IsFinal)
                {
                    h += 17;
                }
            }
            return h & int.MaxValue;
        }

        private int Hash(long Node)
        {
            var PRIME = 31;
            var h = 0;
            fst.ReadFirstRealTargetArc(Node, scratchArc, input);
            while (true)
            {
                h = PRIME * h + scratchArc.Label;
                h = PRIME * h + (int)(scratchArc.Target ^ (scratchArc.Target >> 32));
                h = PRIME * h + scratchArc.Output.GetHashCode();
                h = PRIME * h + scratchArc.NextFinalOutput.GetHashCode();
                if (scratchArc.IsFinal())
                {
                    h += 17;
                }
                if (scratchArc.IsLast())
                {
                    break;
                }
                fst.ReadNextRealArc(scratchArc, input);
            }
            return h & int.MaxValue;
        }

        public long Add(Builder<T>.UnCompiledNode<T> NodeIn)
        {
            var h = Hash(NodeIn);
            var pos = h & mask;
            var c = 0;
            while (true)
            {
                long v = table.Get(pos);
                if (v == 0)
                {
                    // freeze & add
                    long Node = fst.AddNode(NodeIn);
                    //System.out.println("  now freeze Node=" + Node);
                    // Debug.Assert(hash(Node) == h , "frozenHash=" + hash(Node) + " vs h=" + h);
                    count++;
                    table.Set(pos, Node);
                    if (table.Size() < 2 * count)
                    {
                        Rehash();
                    }
                    return Node;
                }
                else if (NodesEqual(NodeIn, v))
                {
                    // same Node is already here
                    return v;
                }

                // quadratic probe
                pos = (pos + (++c)) & mask;
            }
        }

        // called only by rehash
        private void AddNew(long address)
        {
            var pos = Hash(address) & mask;
            var c = 0;
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

        private void Rehash()
        {
            var oldTable = table;

            if (oldTable.Size() >= int.MaxValue / 2)
            {
                throw new InvalidOperationException("FST too large (> 2.1 GB)");
            }

            table = new GrowableWriter(oldTable.GetBitsPerValue(), 2 * oldTable.Size(), PackedInts.COMPACT);
            mask = table.Size() - 1;
            for (var idx = 0; idx < oldTable.Size(); idx++)
            {
                long address = oldTable.Get(idx);
                if (address != 0)
                {
                    AddNew(address);
                }
            }
        }

        public int Count()
        {
            return count;
        }
    }
}
