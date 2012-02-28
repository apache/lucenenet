using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Reverse;
using Lucene.Net.Analysis.Tokenattributes;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Reverse
{
    [TestFixture]
    public class TestReverseStringFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestFilter()
        {
            TokenStream stream = new WhitespaceTokenizer(
                new StringReader("Do have a nice day"));     // 1-4 length string
            ReverseStringFilter filter = new ReverseStringFilter(stream);
            TermAttribute text = filter.GetAttribute<TermAttribute>();
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("oD", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("evah", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("a", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("ecin", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("yad", text.Term());
            Assert.False(filter.IncrementToken());
        }

        [Test]
        public void TestFilterWithMark()
        {
            TokenStream stream = new WhitespaceTokenizer(new StringReader(
                "Do have a nice day")); // 1-4 length string
            ReverseStringFilter filter = new ReverseStringFilter(stream, '\u0001');
            TermAttribute text = filter.GetAttribute<TermAttribute>();
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001oD", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001evah", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001a", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001ecin", text.Term());
            Assert.True(filter.IncrementToken());
            Assert.AreEqual("\u0001yad", text.Term());
            Assert.False(filter.IncrementToken());
        }

        [Test]
        public void TestReverseString()
        {
            Assert.AreEqual("A", ReverseStringFilter.Reverse("A"));
            Assert.AreEqual("BA", ReverseStringFilter.Reverse("AB"));
            Assert.AreEqual("CBA", ReverseStringFilter.Reverse("ABC"));
        }

        [Test]
        public void TestReverseChar()
        {
            char[] buffer = { 'A', 'B', 'C', 'D', 'E', 'F' };
            ReverseStringFilter.Reverse(buffer, 2, 3);
            Assert.AreEqual("ABEDCF", new String(buffer));
        }
    }
}