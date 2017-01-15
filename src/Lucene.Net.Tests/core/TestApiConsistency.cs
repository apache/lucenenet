using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net
{
    /// <summary>
    /// LUCENENET specific tests for ensuring API conventions are followed
    /// </summary>
    public class TestApiConsistency : ApiScanTestBase
    {
        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Analyzer))]
        public override void TestProtectedFieldNames(Type typeFromTargetAssembly)
        {
            base.TestProtectedFieldNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Analyzer))]
        public override void TestPrivateFieldNames(Type typeFromTargetAssembly)
        {
            base.TestPrivateFieldNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Analyzer))]
        public override void TestPublicFields(Type typeFromTargetAssembly)
        {
            base.TestPublicFields(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Analyzer))]
        public override void TestMethodParameterNames(Type typeFromTargetAssembly)
        {
            base.TestMethodParameterNames(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Analyzer))]
        public override void TestForPropertiesWithNoGetter(Type typeFromTargetAssembly)
        {
            base.TestForPropertiesWithNoGetter(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Analyzer))]
        public override void TestForPropertiesThatReturnArray(Type typeFromTargetAssembly)
        {
            base.TestForPropertiesThatReturnArray(typeFromTargetAssembly);
        }

        [Test, LuceneNetSpecific]
        [TestCase(typeof(Lucene.Net.Analysis.Analyzer))]
        public override void TestForPublicMembersContainingComparator(Type typeFromTargetAssembly)
        {
            base.TestForPublicMembersContainingComparator(typeFromTargetAssembly);
        }
    }
}
