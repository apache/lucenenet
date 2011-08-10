// -----------------------------------------------------------------------
// <copyright company="Apache" file="OffsetAttributeTest.cs" >
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Analysis.TokenAttributes
{
#if NUNIT
    using NUnit.Framework;
    using Extensions.NUnit;
#else
    using Gallio.Framework;
    using MbUnit.Framework;
#endif
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Lucene.Net.Util;


    [TestFixture]
    [Category(TestCategories.Unit)]
    [Parallelizable(TestScope.Descendants)]
    public class OffsetAttributeTest
    {

        [Test]
        public void ClearOverride()
        {
            var attribute = new OffsetAttribute(3, 20);
            Assert.AreEqual(3, attribute.OffsetStart);
            Assert.AreEqual(20, attribute.OffsetEnd);

            attribute.Clear();
            
            Assert.AreEqual(0, attribute.OffsetStart);
            Assert.AreEqual(0, attribute.OffsetEnd);
        }

        [Test]
        public void CopyToOverride()
        {
            var attribute = new OffsetAttribute(3, 20);
            var target = new OffsetAttributeLookalike();

            Assert.AreEqual(0, target.OffsetStart);
            Assert.AreEqual(0, target.OffsetEnd);

            attribute.CopyTo(target);

            Assert.AreEqual(3, target.OffsetStart);
            Assert.AreEqual(20, target.OffsetEnd);
        }

        [Test]
        public void EqualsOverride()
        {
            var attribute = new OffsetAttribute(5, 30);
            var equalAttribute = new OffsetAttribute(5, 30);
            var notEqualAttribute = new OffsetAttribute();
            const int wrongType = 45;

            Assert.IsTrue(attribute.Equals(equalAttribute));
            Assert.IsFalse(attribute.Equals(notEqualAttribute));
            Assert.IsFalse(attribute.Equals(wrongType));
        }

        [Test]
        public void GetHashCodeOverride()
        {
            var attribute = new OffsetAttribute(5, 30);
            int code = 5;
            code = (code * 31) + 30;

            Assert.AreEqual(code, attribute.GetHashCode());
        }

        #region helpers

        internal  class OffsetAttributeLookalike : AttributeBase, IOffsetAttribute
        {

            public int OffsetStart { get; set; }

            public int OffsetEnd { get; set; }

            public void SetOffset(int start, int end)
            {
                this.OffsetStart = start;
                this.OffsetEnd = end;
            }

            public override void Clear()
            {
                throw new NotImplementedException();
            }

            public override void CopyTo(AttributeBase attributeBase)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
