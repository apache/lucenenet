using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Codecs.Tests
{
    /// <summary>
    /// LUCENENET specific tests for ensuring API conventions are followed
    /// </summary>
    public class TestApiConsistency : ApiScanTestBase
    {
        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestProtectedFieldNames(Type typeFromTargetAssembly)
        {
            base.TestProtectedFieldNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestPrivateFieldNames(Type typeFromTargetAssembly)
        {
            base.TestPrivateFieldNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestPublicFields(Type typeFromTargetAssembly)
        {
            base.TestPublicFields(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestMethodParameterNames(Type typeFromTargetAssembly)
        {
            base.TestMethodParameterNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestInterfaceNames(Type typeFromTargetAssembly)
        {
            base.TestInterfaceNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestClassNames(Type typeFromTargetAssembly)
        {
            base.TestClassNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestForPropertiesWithNoGetter(Type typeFromTargetAssembly)
        {
            base.TestForPropertiesWithNoGetter(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestForPropertiesThatReturnArray(Type typeFromTargetAssembly)
        {
            base.TestForPropertiesThatReturnArray(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestForMethodsThatReturnWritableArray(Type typeFromTargetAssembly)
        {
            base.TestForMethodsThatReturnWritableArray(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestForPublicMembersContainingComparer(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersContainingComparer(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestForPublicMembersNamedSize(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersNamedSize(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestForPublicMembersContainingNonNetNumeric(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersContainingNonNetNumeric(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Codecs.BlockTerms.BlockTermsReader))]
        public override void TestForPublicMembersWithNullableEnum(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersWithNullableEnum(typeFromTargetAssembly);
        }
    }
}
