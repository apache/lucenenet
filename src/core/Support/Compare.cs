namespace Lucene.Net.Support
{
    /// <summary>
    /// Summary description for TestSupportClass.
    /// </summary>
    public class Compare
    {
        /// <summary>
        /// Compares two Term arrays for equality.
        /// </summary>
        /// <param name="t1">First Term array to compare</param>
        /// <param name="t2">Second Term array to compare</param>
        /// <returns>true if the Terms are equal in both arrays, false otherwise</returns>
        public static bool CompareTermArrays(Index.Term[] t1, Index.Term[] t2)
        {
            if (t1.Length != t2.Length)
                return false;
            for (int i = 0; i < t1.Length; i++)
            {
                if (t1[i].CompareTo(t2[i]) == 0)
                {
                    return true;
                }
            }
            return false;
        }
    }
}