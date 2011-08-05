// -----------------------------------------------------------------------
// <copyright file="FlagsAttributeTest.cs" company="Apache">
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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Lucene.Net.Util;

#if NUNIT
    using NUnit.Framework;
    using Extensions.NUnit;
#else 
    using Gallio.Framework;
    using MbUnit.Framework;
#endif
  
    
    [TestFixture]
    [Category(TestCategories.Unit)]
    [Parallelizable]
    public class FlagsAttributeTest
    {
        [Test]
        public void Flags()
        {
            var attribute = new FlagsAttribute { Flags = 5 };
            Assert.AreEqual(5, attribute.Flags);
        }

        [Test]
        public void Clear()
        {
            var attribute = new FlagsAttribute { Flags = 5 };
            Assert.AreEqual(5, attribute.Flags);

            attribute.Clear();
            Assert.AreEqual(0, attribute.Flags);
        }

        [Test]
        public void Clone()
        {
            var attribute = new FlagsAttribute { Flags = 5 };
            Assert.AreEqual(5, attribute.Flags);

            var clone = attribute.Clone() as FlagsAttribute;
            Assert.IsNotNull(clone);
            Assert.AreEqual(5, clone.Flags);
        }

        [Test]
        public void CopyTo()
        {
            var attribute = new FlagsAttribute { Flags = 5 };
            Assert.AreEqual(5, attribute.Flags);

            var copy = new FlagsAttribute();
            attribute.CopyTo(copy);

            Assert.AreEqual(5, copy.Flags);
        }

        [Test]
        public void CopyTo_WithArgumentException()
        {
            var attribute = new FlagsAttribute { Flags = 5 };
            Assert.AreEqual(5, attribute.Flags);

            Assert.Throws<ArgumentException>(() => attribute.CopyTo(new NonFlagAttribute()));
        }

        [Test]
        public void EqualsOverride()
        {
            var attribute = new FlagsAttribute { Flags = 25 };
            Assert.AreEqual(25, attribute.Flags);

            var y = new FlagsAttribute { Flags = 25 };

            Assert.IsTrue(attribute.Equals(y));
        }

        [Test]
        public void GetHashCodeOverride()
        {
            var attribute = new FlagsAttribute { Flags = 25 };
            Assert.AreEqual(25, attribute.Flags);

            Assert.AreEqual(25, attribute.GetHashCode());
        }

        #region helpers

        private class NonFlagAttribute : AttributeBase
        {

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
