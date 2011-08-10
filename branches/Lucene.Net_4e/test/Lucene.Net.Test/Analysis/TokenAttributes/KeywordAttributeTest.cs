// -----------------------------------------------------------------------
// <copyright company="Apache" file="KeywordAttributeTest.cs" >
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
    public class KeywordAttributeTest
    {

        [Test]
        public void Clear()
        {
            var attribute = new KeywordAttribute() {IsKeyword = true};

            Assert.IsTrue(attribute.IsKeyword);

            attribute.Clear();
            
            Assert.IsFalse(attribute.IsKeyword);
        }

        [Test]
        public void CopyTo()
        {
            var attribute = new KeywordAttribute() {IsKeyword = true};
            var target = new KeywordAttributeLookALike();
            var targetB = new KeywordAttribute();

            Assert.IsFalse(target.IsKeyword);
            Assert.IsFalse(targetB.IsKeyword);

            attribute.CopyTo(target);
            Assert.IsTrue(target.IsKeyword);

            attribute.CopyTo(targetB);
            Assert.IsTrue(targetB.IsKeyword);

        }

        [Test]
        public void GetHashCodeOverride()
        {
            var attribute = new KeywordAttribute();
            Assert.AreEqual(37, attribute.GetHashCode());

            attribute.IsKeyword = true;
            Assert.AreEqual(31, attribute.GetHashCode());
        }

        [Test]
        public void EqualsOverride()
        {
            var attributeX = new KeywordAttribute() {IsKeyword = true};
            var attributeY = new KeywordAttribute();

            Assert.IsFalse(attributeX.Equals(attributeY));

            attributeY.IsKeyword = true;

            Assert.IsTrue(attributeX.Equals(attributeY));

            object unknownType = attributeY;

            Assert.IsTrue(attributeX.Equals(unknownType));

            Assert.IsFalse(attributeX.Equals(4));
        }

        #region helpers

        internal class KeywordAttributeLookALike : AttributeBase, IKeywordAttribute
        {

            public bool IsKeyword { get; set; }

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
