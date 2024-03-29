using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Tests for StringHelper.getVersionComparer
    /// </summary>
    [TestFixture]
    public class TestVersionComparer : LuceneTestCase
    {
        [Test]
        public virtual void TestVersions()
        {
            IComparer<string> comp = StringHelper.VersionComparer;
            Assert.IsTrue(comp.Compare("1", "2") < 0);
            Assert.IsTrue(comp.Compare("1", "1") == 0);
            Assert.IsTrue(comp.Compare("2", "1") > 0);

            Assert.IsTrue(comp.Compare("1.1", "1") > 0);
            Assert.IsTrue(comp.Compare("1", "1.1") < 0);
            Assert.IsTrue(comp.Compare("1.1", "1.1") == 0);

            Assert.IsTrue(comp.Compare("1.0", "1") == 0);
            Assert.IsTrue(comp.Compare("1", "1.0") == 0);
            Assert.IsTrue(comp.Compare("1.0.1", "1.0") > 0);
            Assert.IsTrue(comp.Compare("1.0", "1.0.1") < 0);

            Assert.IsTrue(comp.Compare("1.02.003", "1.2.3.0") == 0);
            Assert.IsTrue(comp.Compare("1.2.3.0", "1.02.003") == 0);

            Assert.IsTrue(comp.Compare("1.10", "1.9") > 0);
            Assert.IsTrue(comp.Compare("1.9", "1.10") < 0);

            Assert.IsTrue(comp.Compare("0", "1.0") < 0);
            Assert.IsTrue(comp.Compare("00", "1.0") < 0);
            Assert.IsTrue(comp.Compare("-1.0", "1.0") < 0);
            Assert.IsTrue(comp.Compare("3.0", Convert.ToString(int.MinValue, CultureInfo.InvariantCulture)) > 0);
        }
    }
}
