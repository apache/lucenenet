using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestParameter
    {
        internal class MockParameter : Parameter
        {
            public MockParameter(string name)
                : base(name)
            { }
        }

        [Test]
        public void TestEquals()
        {
            var first = new MockParameter("FIRST");
            var other = new MockParameter("OTHER");

            // Make sure it's equal against itself
            Assert.AreEqual(first, first);
            // Not equal if it has a different name
            Assert.AreNotEqual(first, other);
            
            // Test == operator
            Assert.IsTrue(first == first);
            Assert.IsFalse(first == other);

            // Test != operator
            Assert.IsFalse(first != first);
            Assert.IsTrue(first != other);
        }

        
        [Test]
        public void TestLuceneNet472()
        {
            var thing = new MockParameter("THING");
            var otherThing = new MockParameter("OTHERTHING");

            // LUCENENET-472 - NRE on ==/!= parameter
            Assert.IsTrue(thing != null);
            Assert.IsFalse(thing == null);
            Assert.IsTrue(otherThing != null);
        }
    }
}
