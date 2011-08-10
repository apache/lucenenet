// -----------------------------------------------------------------------
// <copyright company="Apache" file="PositionIncrementAttributeTest.cs" >
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
    public class PositionIncrementAttributeTest
    {

        [Test]
        public void SettingPositionIncrement_ThrowsArgumentOutOfRangeException()
        {
            var attribute = new PositionIncrementAttribute();

            Assert.Throws<ArgumentOutOfRangeException>(() => {
                attribute.PositionIncrement = -1;
            });

            Assert.DoesNotThrow(() => {
                attribute.PositionIncrement = 1;
            });
        }

        [Test]
        public void ClearOverride()
        {
            var attribute = new PositionIncrementAttribute(23);
            Assert.AreEqual(23, attribute.PositionIncrement);

            attribute.Clear();

            Assert.AreEqual(1, attribute.PositionIncrement);
        }

        [Test]
        public void CopyToOverride()
        {
            var attribute = new PositionIncrementAttribute(35);
            var target = new PositionIncrementLookAlike();
            Assert.AreEqual(1, target.PositionIncrement);

            attribute.CopyTo(target);
            Assert.AreEqual(35, target.PositionIncrement);
        }

        [Test]
        public void EqualsOverride()
        {
            var attribute = new PositionIncrementAttribute(12);
            var equalAttribute = new PositionIncrementAttribute(12);
            var notEqualAttribute = new PositionIncrementAttribute();
            const string wrongType = "test";

            Assert.IsTrue(attribute.Equals(equalAttribute));
            Assert.IsFalse(attribute.Equals(notEqualAttribute));
            Assert.IsFalse(attribute.Equals(wrongType));
        }

        [Test]
        public void GetHashCodeOverride()
        {
            var attribute = new PositionIncrementAttribute();
            Assert.AreEqual(1, attribute.GetHashCode());

            attribute.PositionIncrement = 23;
            Assert.AreEqual(23, attribute.GetHashCode());
        }

        #region helper

        internal class PositionIncrementLookAlike : AttributeBase, IPositionIncrementAttribute
        {
            public PositionIncrementLookAlike()
            {
                this.PositionIncrement = 1;
            }

            public override void Clear()
            {
                throw new NotImplementedException();
            }

            public override void CopyTo(AttributeBase attributeBase)
            {
                throw new NotImplementedException();
            }

            public int PositionIncrement { get; set; }
        }

        #endregion
    }
}
