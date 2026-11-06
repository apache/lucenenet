using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Support
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
    /// Tests for <see cref="CloneableExtensions"/>, a helper to cast the
    /// result of the Clone() methods to the most derived type, so that
    /// the caller doesn't have to cast it themselves.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestCloneableExtensions : LuceneTestCase
    {
        private class ClonableType : ICloneable
        {
            object ICloneable.Clone() => Clone();

            protected virtual object Clone()
            {
                return MemberwiseClone();
            }
        }

        private class DerivedClonableType : ClonableType
        {
        }

        [Test]
        public void TestCloningBase()
        {
            ClonableType original = new();

            ClonableType clone = original.Clone(); // Compile time check to ensure we don't need a cast

            Assert.AreNotSame(original, clone);
        }


        [Test]
        public void TestCloningDerived()
        {
            DerivedClonableType original = new();

            DerivedClonableType clone = original.Clone(); // Compile time check to ensure we don't need a cast

            Assert.AreNotSame(original, clone);

            ClonableType baseType = clone;

            ClonableType baseTypeClone = baseType.Clone();

            Assert.AreNotSame(baseType, baseTypeClone);
        }
    }
}
