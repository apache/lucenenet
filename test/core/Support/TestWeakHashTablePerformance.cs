using System.Collections;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestWeakHashTablePerformance
    {
        IDictionary dictionary;
        SmallObject[] keys;


        [SetUp]
        public void Setup()
        {
            dictionary = TestWeakHashTableBehavior.CreateDictionary();
        }

        private void Fill(IDictionary dictionary)
        {
            foreach (SmallObject key in keys)
                dictionary.Add(key, "value");
        }

        [TestFixtureSetUp]
        public void TestSetup()
        {
            keys = new SmallObject[100000];
            for (int i = 0; i < keys.Length; i++)
                keys[i] = new SmallObject(i);
        }

        [Test]
        public void Test_Performance_Add()
        {
            for (int i = 0; i < 10; i++)
            {
                dictionary.Clear();
                Fill(dictionary);
            }
        }

        [Test]
        public void Test_Performance_Remove()
        {
            for (int i = 0; i < 10; i++)
            {
                Fill(dictionary);
                foreach (SmallObject key in keys)
                    dictionary.Remove(key);
            }
        }

        [Test]
        public void Test_Performance_Replace()
        {
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                    dictionary[key] = "value2";
            }
        }

        [Test]
        public void Test_Performance_Access()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                {
                    object value = dictionary[key];
                }
            }
        }

        [Test]
        public void Test_Performance_Contains()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (SmallObject key in keys)
                {
                    dictionary.Contains(key);
                }
            }
        }

        [Test]
        public void Test_Performance_Keys()
        {
            Fill(dictionary);
            for (int i = 0; i < 100; i++)
            {
                ICollection keys = dictionary.Keys;
            }
        }

        [Test]
        public void Test_Performance_ForEach()
        {
            Fill(dictionary);
            for (int i = 0; i < 10; i++)
            {
                foreach (DictionaryEntry de in dictionary)
                {

                }
            }
        }

        [Test]
        public void Test_Performance_CopyTo()
        {
            Fill(dictionary);
            DictionaryEntry[] array = new DictionaryEntry[dictionary.Count];

            for (int i = 0; i < 10; i++)
            {
                dictionary.CopyTo(array, 0);
            }
        }
    }
}