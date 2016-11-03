using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Text;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestStringBuilderExtensions
    {
        [Test, LuceneNetSpecific]
        public virtual void TestReverse()
        {
            var sb = new StringBuilder("foo 𝌆 bar𫀁mañana");

            sb.Reverse();

            Assert.AreEqual("anañam𫀁rab 𝌆 oof", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointBmp()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 97; // a

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo bara", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointUnicode()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 3594; // ช

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo barช", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointUTF16Surrogates()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 176129; // '\uD86C', '\uDC01' (𫀁)

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo bar𫀁", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointTooHigh()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = Character.MAX_CODE_POINT + 1;

            Assert.Throws<ArgumentException>(() => sb.AppendCodePoint(codePoint));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointTooLow()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = Character.MIN_CODE_POINT - 1;

            Assert.Throws<ArgumentException>(() => sb.AppendCodePoint(codePoint));
        }
    }
}
