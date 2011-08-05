// -----------------------------------------------------------------------
// <copyright file="AttributeBaseTest.cs" company="Apache">
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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Lucene.Net.Analysis.TokenAttributes;
    using FlagsAttribute = Lucene.Net.Analysis.TokenAttributes.FlagsAttribute;

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
    public class AttributeBaseTest
    {

        [Test]
        public void Reflect()
        {
            var attribute = new FlagsAttribute();
            var builder = new StringBuilder();

            attribute.Reflect((t, n, v) =>
            {
                builder
                        .Append(t)
                        .Append(".")
                        .Append(n)
                        .Append("=")
                        .Append(v);
            });

            Assert.AreEqual("Lucene.Net.Analysis.TokenAttributes.FlagsAttribute.Flags=0", builder.ToString());
        }

        [Test]
        public void Reflect_WithNotSupportedException()
        {
            var twoAttributesInstance = new TwoAttributes();
            var builder = new StringBuilder();

            Assert.Throws<NotSupportedException>(() =>
            {
                twoAttributesInstance.Reflect((t, n, v) =>
                {
                    builder
                            .Append(t)
                            .Append(".")
                            .Append(n)
                            .Append("=")
                            .Append(v);
                });
            });
        }

        [Test]
        public void ReflectAsString()
        {
            var attribute = new FlagsAttribute();
            var result =  attribute.ReflectAsString();
            
            Assert.AreEqual("Flags=0", result);

            result = attribute.ReflectAsString(true);
            Assert.AreEqual("FlagsAttribute#Flags=0", result);
        }


        #region Helpers

        public class TwoAttributes : AttributeBase, IFlagsAttribute, ISecondAttribute
        {

            public int Flags { get; set; }
                

            public override void Clear()
            {
                throw new NotImplementedException();
            }

            public override void CopyTo(AttributeBase attributeBase)
            {
                throw new NotImplementedException();
            }

           
        }

        public interface ISecondAttribute : IAttribute
        {

        }

        #endregion
    }
}
