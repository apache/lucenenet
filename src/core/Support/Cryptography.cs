using System.Security.Cryptography;

namespace Lucene.Net.Support
{
    public class Cryptography
    {
        static public bool FIPSCompliant = false;

        static public System.Security.Cryptography.HashAlgorithm GetHashAlgorithm()
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