using Lucene.Net.Store;
using Lucene.Net.Util.Fst;
using System;
using System.IO;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ko.Dict
{
    public sealed class TokenInfoDictionary : BinaryDictionary
    {
        public static readonly string FST_FILENAME_SUFFIX = "$fst.dat";

        private readonly TokenInfoFST fst;

        private TokenInfoDictionary()
        {
            FST<Int64> fst = null;
            using (Stream @is = GetResource(FST_FILENAME_SUFFIX))
            {
                fst = new FST<Int64>(new InputStreamDataInput(@is), PositiveInt32Outputs.Singleton);
            }
            // TODO: some way to configure?
            this.fst = new TokenInfoFST(fst, true);
        }

        public TokenInfoFST FST => fst;

        public static TokenInfoDictionary Instance => SingletonHolder.INSTANCE;

        private class SingletonHolder
        {
            internal static readonly TokenInfoDictionary INSTANCE = LoadInstance();
            private static TokenInfoDictionary LoadInstance() // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
            {
                try
                {
                    return new TokenInfoDictionary();
                }
                catch (Exception ioe) when (ioe.IsIOException())
                {
                    throw RuntimeException.Create("Cannot load TokenInfoDictionary.", ioe);
                }
            }
        }
    }
}