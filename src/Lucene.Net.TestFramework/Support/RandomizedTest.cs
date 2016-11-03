using Lucene.Net.Support.Configuration;
using NUnit.Framework;


namespace Lucene.Net.TestFramework.Support
{
    public class RandomizedTest
    {
        public static bool SystemPropertyAsBoolean(string key, bool defaultValue)
        {
            return Configuration.GetProperty<bool>(key, defaultValue,
                (str) =>
                {
                    bool value;
                    return bool.TryParse(str, out value) ? value : defaultValue;
                });
        }

        public static int SystemPropertyAsInt(string key, int defaultValue)
        {
            return Configuration.GetProperty<int>(key, defaultValue,
              (str) =>
              {
                  int value;
                  return int.TryParse(str, out value) ? value : defaultValue;
              }
            );
        }

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