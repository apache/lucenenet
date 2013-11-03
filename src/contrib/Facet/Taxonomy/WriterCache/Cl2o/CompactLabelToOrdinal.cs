using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Taxonomy.WriterCache.Cl2o
{
    public class CompactLabelToOrdinal : LabelToOrdinal
    {
        public static readonly float DefaultLoadFactor = 0.15F;
        static readonly char TERMINATOR_CHAR = (char)0xffff;
        private static readonly int COLLISION = -5;
        private HashArray[] hashArrays;
        private CollisionMap collisionMap;
        private CharBlockArray labelRepository;
        private int capacity;
        private int threshold;
        private float loadFactor;

        public virtual int SizeOfMap
        {
            get
            {
                return this.collisionMap.Size;
            }
        }

        private CompactLabelToOrdinal()
        {
        }

        public CompactLabelToOrdinal(int initialCapacity, float loadFactor, int numHashArrays)
        {
            this.hashArrays = new HashArray[numHashArrays];
            this.capacity = DetermineCapacity((int)Math.Pow(2, numHashArrays), initialCapacity);
            Init();
            this.collisionMap = new CollisionMap(this.labelRepository);
            this.counter = 0;
            this.loadFactor = loadFactor;
            this.threshold = (int)(this.loadFactor * this.capacity);
        }

        internal static int DetermineCapacity(int minCapacity, int initialCapacity)
        {
            int capacity = minCapacity;
            while (capacity < initialCapacity)
            {
                capacity <<= 1;
            }

            return capacity;
        }

        private void Init()
        {
            labelRepository = new CharBlockArray();
            CategoryPathUtils.Serialize(CategoryPath.EMPTY, labelRepository);
            int c = this.capacity;
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                this.hashArrays[i] = new HashArray(c);
                c /= 2;
            }
        }

        public override void AddLabel(CategoryPath label, int ordinal)
        {
            if (collisionMap.Size > threshold)
            {
                Grow();
            }

            int hash = CompactLabelToOrdinal.StringHashCode(label);
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                if (AddLabel(this.hashArrays[i], label, hash, ordinal))
                {
                    return;
                }
            }

            int prevVal = collisionMap.AddLabel(label, hash, ordinal);
            if (prevVal != ordinal)
            {
                throw new ArgumentException(@"Label already exists: " + label.ToString('/') + @" prev ordinal " + prevVal);
            }
        }

        public override int GetOrdinal(CategoryPath label)
        {
            if (label == null)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            int hash = CompactLabelToOrdinal.StringHashCode(label);
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                int ord = GetOrdinal(this.hashArrays[i], label, hash);
                if (ord != COLLISION)
                {
                    return ord;
                }
            }

            return this.collisionMap.Get(label, hash);
        }

        private void Grow()
        {
            HashArray temp = this.hashArrays[this.hashArrays.Length - 1];
            for (int i = this.hashArrays.Length - 1; i > 0; i--)
            {
                this.hashArrays[i] = this.hashArrays[i - 1];
            }

            this.capacity *= 2;
            this.hashArrays[0] = new HashArray(this.capacity);
            for (int i = 1; i < this.hashArrays.Length; i++)
            {
                int[] sourceOffsetArray = this.hashArrays[i].offsets;
                int[] sourceCidsArray = this.hashArrays[i].cids;
                for (int k = 0; k < sourceOffsetArray.Length; k++)
                {
                    for (int j = 0; j < i && sourceOffsetArray[k] != 0; j++)
                    {
                        int[] targetOffsetArray = this.hashArrays[j].offsets;
                        int[] targetCidsArray = this.hashArrays[j].cids;
                        int newIndex = IndexFor(StringHashCode(this.labelRepository, sourceOffsetArray[k]), targetOffsetArray.Length);
                        if (targetOffsetArray[newIndex] == 0)
                        {
                            targetOffsetArray[newIndex] = sourceOffsetArray[k];
                            targetCidsArray[newIndex] = sourceCidsArray[k];
                            sourceOffsetArray[k] = 0;
                        }
                    }
                }
            }

            for (int i = 0; i < temp.offsets.Length; i++)
            {
                int offset = temp.offsets[i];
                if (offset > 0)
                {
                    int hash = StringHashCode(this.labelRepository, offset);
                    AddLabelOffset(hash, temp.cids[i], offset);
                }
            }

            CollisionMap oldCollisionMap = this.collisionMap;
            this.collisionMap = new CollisionMap(oldCollisionMap.Capacity, this.labelRepository);
            this.threshold = (int)(this.capacity * this.loadFactor);
            IEnumerator<CollisionMap.Entry> it = oldCollisionMap.GetEnumerator();
            while (it.MoveNext())
            {
                CollisionMap.Entry e = it.Current;
                AddLabelOffset(StringHashCode(this.labelRepository, e.offset), e.cid, e.offset);
            }
        }

        private bool AddLabel(HashArray a, CategoryPath label, int hash, int ordinal)
        {
            int index = CompactLabelToOrdinal.IndexFor(hash, a.offsets.Length);
            int offset = a.offsets[index];
            if (offset == 0)
            {
                a.offsets[index] = this.labelRepository.Length;
                CategoryPathUtils.Serialize(label, labelRepository);
                a.cids[index] = ordinal;
                return true;
            }

            return false;
        }

        private void AddLabelOffset(int hash, int cid, int knownOffset)
        {
            for (int i = 0; i < this.hashArrays.Length; i++)
            {
                if (AddLabelOffsetToHashArray(this.hashArrays[i], hash, cid, knownOffset))
                {
                    return;
                }
            }

            this.collisionMap.AddLabelOffset(hash, knownOffset, cid);
            if (this.collisionMap.Size > this.threshold)
            {
                Grow();
            }
        }

        private bool AddLabelOffsetToHashArray(HashArray a, int hash, int ordinal, int knownOffset)
        {
            int index = CompactLabelToOrdinal.IndexFor(hash, a.offsets.Length);
            int offset = a.offsets[index];
            if (offset == 0)
            {
                a.offsets[index] = knownOffset;
                a.cids[index] = ordinal;
                return true;
            }

            return false;
        }

        private int GetOrdinal(HashArray a, CategoryPath label, int hash)
        {
            if (label == null)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            int index = IndexFor(hash, a.offsets.Length);
            int offset = a.offsets[index];
            if (offset == 0)
            {
                return LabelToOrdinal.INVALID_ORDINAL;
            }

            if (CategoryPathUtils.EqualsToSerialized(label, labelRepository, offset))
            {
                return a.cids[index];
            }

            return COLLISION;
        }

        static int IndexFor(int h, int length)
        {
            return h & (length - 1);
        }

        static int StringHashCode(CategoryPath label)
        {
            int hash = label.GetHashCode();
            hash = hash ^ ((hash >> 20) ^ (hash >> 12));
            hash = hash ^ (hash >> 7) ^ (hash >> 4);
            return hash;
        }

        static int StringHashCode(CharBlockArray labelRepository, int offset)
        {
            int hash = CategoryPathUtils.HashCodeOfSerialized(labelRepository, offset);
            hash = hash ^ ((hash >> 20) ^ (hash >> 12));
            hash = hash ^ (hash >> 7) ^ (hash >> 4);
            return hash;
        }

        internal virtual int MemoryUsage
        {
            get
            {
                int memoryUsage = 0;
                if (this.hashArrays != null)
                {
                    foreach (HashArray ha in this.hashArrays)
                    {
                        memoryUsage += (ha.capacity * 2 * 4) + 4;
                    }
                }

                if (this.labelRepository != null)
                {
                    int blockSize = this.labelRepository.blockSize;
                    int actualBlockSize = (blockSize * 2) + 4;
                    memoryUsage += this.labelRepository.blocks.Count * actualBlockSize;
                    memoryUsage += 8;
                }

                if (this.collisionMap != null)
                {
                    memoryUsage += this.collisionMap.MemoryUsage;
                }

                return memoryUsage;
            }
        }

        static CompactLabelToOrdinal Open(FileInfo file, float loadFactor, int numHashArrays)
        {
            CompactLabelToOrdinal l2o = new CompactLabelToOrdinal();
            l2o.loadFactor = loadFactor;
            l2o.hashArrays = new HashArray[numHashArrays];
            BinaryReader dis = null;
            FileStream fstream = file.OpenRead();
            try
            {
                dis = new BinaryReader(fstream);
                l2o.counter = dis.ReadInt32();
                l2o.capacity = DetermineCapacity((int)Math.Pow(2, l2o.hashArrays.Length), l2o.counter);
                l2o.Init();
                l2o.labelRepository = CharBlockArray.Open(fstream);
                l2o.collisionMap = new CollisionMap(l2o.labelRepository);
                int cid = 0;
                int offset = 1;
                int lastStartOffset = offset;
                while (offset < l2o.labelRepository.Length)
                {
                    int length = (short)l2o.labelRepository.CharAt(offset++);
                    int hash = length;
                    if (length != 0)
                    {
                        for (int i = 0; i < length; i++)
                        {
                            int len = (short)l2o.labelRepository.CharAt(offset++);
                            hash = hash * 31 + l2o.labelRepository.SubSequence(offset, offset + len).GetHashCode();
                            offset += len;
                        }
                    }

                    hash = hash ^ ((hash >> 20) ^ (hash >> 12));
                    hash = hash ^ (hash >> 7) ^ (hash >> 4);
                    l2o.AddLabelOffset(hash, cid, lastStartOffset);
                    cid++;
                    lastStartOffset = offset;
                }
            }
            catch (TypeLoadException cnfe)
            {
                throw new IOException(@"Invalid file format. Cannot deserialize.");
            }
            finally
            {
                if (dis != null)
                {
                    dis.Dispose();
                    fstream.Dispose();
                }
            }

            l2o.threshold = (int)(l2o.loadFactor * l2o.capacity);
            return l2o;
        }

        internal virtual void Flush(FileInfo file)
        {
            var fos = file.OpenWrite();
            try
            {
                //BufferedOutputStream os = new BufferedOutputStream(fos);
                var dos = new BinaryWriter(fos);
                dos.Write(this.counter);
                this.labelRepository.Flush(fos);
                dos.Dispose();
            }
            finally
            {
                fos.Dispose();
            }
        }

        private sealed class HashArray
        {
            internal int[] offsets;
            internal int[] cids;
            internal int capacity;
            
            internal HashArray(int c)
            {
                this.capacity = c;
                this.offsets = new int[this.capacity];
                this.cids = new int[this.capacity];
            }
        }
    }
}
