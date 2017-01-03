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
    public class TestApiConsistency : LuceneTestCase
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
    }
}
