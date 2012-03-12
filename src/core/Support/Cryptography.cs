using System.Security.Cryptography;

namespace Lucene.Net.Support
{
    public static class Cryptography
    {
        public static bool FIPSCompliant = false;

        public static HashAlgorithm HashAlgorithm
        {
            get
            {
                if (FIPSCompliant)
                {
                    //LUCENENET-175
                    //No Assumptions should be made on the HashAlgorithm. It may change in time.
                    //SHA256 SHA384 SHA512 etc.
                    return SHA1.Create();
                }
                return MD5.Create();
            }
        }
    }
}