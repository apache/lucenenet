using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;
using System.Threading;

namespace Lucene.Net.Core.Util
{
    /// <summary>
    /// This test was added for .NET compatibility
    /// </summary>
    public class TestToStringUtils : LuceneTestCase
    {
        CultureInfo originalCulture;
        public override void SetUp()
        {
            base.SetUp();
            originalCulture = Thread.CurrentThread.CurrentCulture;
        }

        public override void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            base.TearDown();
        }

        /// <summary>
        /// Check to ensure that the Boost function is properly converted in every possible culture.
        /// </summary>
        [Test]
        public void TestBoost()
        {
            float boostNormal = 1f;
            float boostFractional = 2.5f;
            float boostNonFractional = 5f;
            float boostLong = 1.111111111f;

            foreach (CultureInfo culture in CultureInfo.GetCultures(CultureTypes.SpecificCultures | CultureTypes.NeutralCultures))
            {
                Thread.CurrentThread.CurrentCulture = culture;

                assertEquals("", ToStringUtils.Boost(boostNormal));
                assertEquals("^2.5", ToStringUtils.Boost(boostFractional));
                assertEquals("^5.0", ToStringUtils.Boost(boostNonFractional));
                assertEquals("^1.111111", ToStringUtils.Boost(boostLong));
            }
        }
    }
}
