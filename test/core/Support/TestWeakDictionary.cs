using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestWeakDictionary
    {
        [Test]
        public void TestBasicOps()
        {
            WeakDictionary<string, string> wd = new WeakDictionary<string, string>();
            List<string> list = new List<string>();
            for (int i = 0; i < 100; i++)
            {
                list.Add(i.ToString());
            }

            foreach (string s in list)
            {
                wd.Add("k" + s, "v" + s);
            }
            foreach(string key in list)
            {
                Assert.AreEqual(wd["k"+key],"v"+key);
            }
            foreach (string key in list)
            {
                wd.Remove("k"+key);
            }
            Assert.AreEqual(0,wd.Count);


            foreach (string s in list)
            {
                wd.Add("k" + s, "v" + s);
            }
            foreach (string key in wd.Keys)
            {
                Assert.True(list.Contains(key.Substring(1)));
            }

            foreach(KeyValuePair<string,string> kv in wd)
            {
                Assert.True(list.Contains(kv.Key.Substring(1)));
                Assert.AreEqual(kv.Key.Substring(1), kv.Value.Substring(1));
            }
        }
    
        
    }
}
