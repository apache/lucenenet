using NUnit.Framework;


namespace Lucene.Net.TestFramework.Support
{
    public class RandomizedTest
    {
        public static void AssumeTrue(string msg, bool value)
        {
            Assume.That(value, msg);
        }

        public static void AssumeFalse(string msg, bool value)
        {
            Assume.That(!value, msg);
        }
    }
}