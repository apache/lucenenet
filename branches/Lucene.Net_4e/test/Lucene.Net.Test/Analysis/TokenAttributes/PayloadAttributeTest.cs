// -----------------------------------------------------------------------
// <copyright company="Apache" file="PayloadAttributeTest.cs" >
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

using Lucene.Net.Index;

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
    public class PayloadAttributeTest
    {

        [Test]
        public void ClearOverride()
        {
            var attribute = new PayloadAttribute(new Payload());
            Assert.IsNotNull(attribute.Payload);

            attribute.Clear();
            Assert.IsNull(attribute.Payload);
        }

        [Test]
        public void CloneOverride()
        {
            var attribute = new PayloadAttribute();
            var clone = (PayloadAttribute)attribute.Clone();
            
            Assert.AreEqual(attribute, clone);
            Assert.IsNull(clone.Payload);

            attribute.Payload = new Payload();
            clone = (PayloadAttribute)attribute.Clone();

          
            Assert.IsNotNull(clone.Payload);
            Assert.IsTrue(attribute.Payload.Equals(clone.Payload));
        }

        [Test]
        public void CopyToOverride()
        {
            var attribute = new PayloadAttribute(new Payload());
            var target = new PayloadAttributeLookAlike();
            Assert.IsNull(target.Payload);

            attribute.CopyTo(target);
            Assert.AreEqual(attribute.Payload, target.Payload);
        }

        [Test]
        public void EqualsOverride()
        {
            var attribute = new PayloadAttribute(new Payload());
            var equalAttribute = new PayloadAttribute(new Payload());
            var notEqualAttribute = new PayloadAttribute();
            const int wrongType = 55;

            Assert.IsTrue(attribute.Equals(equalAttribute));
            Assert.IsFalse(attribute.Equals(notEqualAttribute));
            Assert.IsFalse(attribute.Equals(wrongType));

            attribute.Clear();
            
            Assert.IsTrue(attribute.Equals(new PayloadAttribute()));
        }

        [Test]
        public void GetHashCodeOverride()
        {
            var attribute = new PayloadAttribute();
            Assert.AreEqual(0, attribute.GetHashCode());

            attribute.Payload = new Payload();

            Assert.AreEqual(attribute.Payload.GetHashCode(), attribute.GetHashCode());

        }

        #region helpers

        internal class PayloadAttributeLookAlike : AttributeBase, IPayloadAttribute
        {
            

            public Payload Payload { get; set; }

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
