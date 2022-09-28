using Lucene.Net.Diagnostics;
using Lucene.Net.Util.Fst;
using System.Diagnostics;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ko.Dict
{
    public class TokenInfoFST
    {
        private readonly FST<Int64> fst;

        private readonly int cacheCeiling;
        private readonly FST.Arc<Int64>[] rootCache;

        private readonly Int64 NO_OUTPUT;

        // LUCENENET specific - made field private
        // and added public property for reading it.
        public Int64 NoOutput => NO_OUTPUT;

        public TokenInfoFST(FST<Int64> fst, bool fasterButMoreRam)
        {
            this.fst = fst;
            this.cacheCeiling = fasterButMoreRam ? 0x9FFF : 0x30FF;
            NO_OUTPUT = fst.Outputs.NoOutput;
            rootCache = CacheRootArcs();
        }

        private FST.Arc<Int64>[] CacheRootArcs()
        {
            FST.Arc<Int64>[] rootCache = new FST.Arc<Int64>[1 + (cacheCeiling - 0x3040)];
            FST.Arc<Int64> firstArc = new FST.Arc<Int64>();
            fst.GetFirstArc(firstArc);
            FST.Arc<Int64> arc = new FST.Arc<Int64>();
            FST.BytesReader fstReader = fst.GetBytesReader();
            // TODO: jump to 3040, readNextRealArc to ceiling? (just be careful we don't add bugs)
            for (int i = 0; i < rootCache.Length; i++)
            {
                if (fst.FindTargetArc(0xAC00 + i, firstArc, arc, fstReader) != null)
                {
                    rootCache[i] = new FST.Arc<Int64>().CopyFrom(arc);
                }
            }
            return rootCache;
        }

        public FST.Arc<Int64> FindTargetArc(int ch, FST.Arc<Int64> follow, FST.Arc<Int64> arc, bool useCache, FST.BytesReader fstReader)
        {
            if (useCache && ch >= 0xAC00 && ch <= cacheCeiling)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(ch != FST.END_LABEL);
                FST.Arc<Int64> result = rootCache[ch - 0xAC00];
                if (result is null)
                {
                    return null;
                }
                else
                {
                    arc.CopyFrom(result);
                    return arc;
                }
            }
            else
            {
                return fst.FindTargetArc(ch, follow, arc, fstReader);
            }
        }
        public FST.Arc<Int64> GetFirstArc(FST.Arc<Int64> arc)
        {
            return fst.GetFirstArc(arc);
        }

        public FST.BytesReader GetBytesReader()
        {
            return fst.GetBytesReader();
        }

        /// <summary>
        /// for testing only
        /// <para/>
        /// @lucene.internal
        /// </summary>
        internal FST<Int64> InternalFST => fst;
    }
}