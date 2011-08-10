// -----------------------------------------------------------------------
// <copyright company="Apache" file="TypeAttributeTest.cs" >
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
    public class TypeAttributeTest
    {
        [Test]
        public void ClearOverrride()
        {
            var attribute = new TypeAttribute("special");
            Assert.AreEqual("special", attribute.Type);

            attribute.Clear();
            Assert.AreEqual(TypeAttribute.DefaultType, attribute.Type);
        }

        [Test]
        public void CloneOverride()
        {
            var attribute = new TypeAttribute("special");
            var clone = (TypeAttribute)attribute.Clone();

            Assert.AreEqual("special", clone.Type);
        }


        [Test]
        public void CopyToOverride()
        {
            var attribute = new TypeAttribute() {Type = "special"};
            var target = new TypeAttribute();
            var targetB = new TypeAttributeLookAlike();

            Assert.AreEqual(TypeAttribute.DefaultType, target.Type);
            Assert.AreEqual(TypeAttribute.DefaultType, targetB.Type);

            attribute.CopyTo(target);
            Assert.AreEqual("special", target.Type);

            attribute.CopyTo(targetB);
            Assert.AreEqual("special", targetB.Type);
        }

        [Test]
        public void EqualsOverride()
        {
            var attribute = new TypeAttribute("special");
            var equalAttribute = new TypeAttribute("special");
            var notEqualAttribute = new TypeAttribute();
            const int wrongType = 25;

            Assert.IsTrue(attribute.Equals(equalAttribute));
            Assert.IsFalse(attribute.Equals(notEqualAttribute));
            Assert.IsFalse(attribute.Equals(wrongType));
        }

        [Test]
        public void GetHashCodeOverride()
        {
            var attribute = new TypeAttribute();
            Assert.AreEqual(TypeAttribute.DefaultType.GetHashCode(), attribute.GetHashCode());
        }

        #region Helpers

        internal class TypeAttributeLookAlike: AttributeBase, ITypeAttribute
        {
            public TypeAttributeLookAlike()
            {
                this.Type = TypeAttribute.DefaultType;
            }

            public string Type { get; set; }

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
