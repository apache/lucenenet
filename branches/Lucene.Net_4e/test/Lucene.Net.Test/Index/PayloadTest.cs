// -----------------------------------------------------------------------
// <copyright company="Apache" file="PayloadTest.cs" >
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



namespace Lucene.Net.Index
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
    public class PayloadTest
    {
        private readonly byte[] data = Encoding.UTF8.GetBytes("Lucene.Net n da hizouse");

        [Test]
        public void ByteAt()
        {
            byte c = this.data[2];
            byte dot = this.data[6];

            var payload = new Payload(this.data);
            Assert.AreEqual(c, payload.ByteAt(2));

            payload.SetData(this.data, 5);
            Assert.AreEqual(dot, payload.ByteAt(1));
        }

        [Test]
        public void Clone()
        {
            var payload = new Payload(this.data);
            var clone = (Payload)payload.Clone();

            Assert.AreEqual(this.data, clone.Data);
        }

        [Test]
        public void CopyTo_ThrowsArgumentNullException()
        {
            var payload = new Payload();
            Assert.Throws<ArgumentNullException>(() => {
                payload.CopyTo(null);
            });
        }

        [Test]
        public void CopyTo_ThrowsIndexOutOfRangeException()
        {
            var payload = new Payload(new byte[20]);

            Assert.Throws<IndexOutOfRangeException>(() => {
                payload.CopyTo(new byte[4]);
            });
        }

        [Test]
        public void CopyTo()
        {
            var payload = new Payload(this.data);
            var target = new byte[payload.Length];
            payload.CopyTo(target);

            Assert.AreEqual(this.data, target);
        }

        [Test]
        public void EqualsOverride()
        {
            var payload = new Payload(this.data);
            var equalPayload = new Payload(this.data);
            var notEqualPayload = new Payload(Encoding.UTF8.GetBytes("stuff"));
            var wrongType = this.data;

            Assert.IsTrue(payload.Equals(equalPayload));
            Assert.IsFalse(payload.Equals(notEqualPayload));
            Assert.IsFalse(payload.Equals(wrongType));
        }

        [Test]
        public void GetHashCodeOverride()
        {
            var payload = new Payload(this.data, 1, 10);
            var expected = payload.Data.CreateHashCode(payload.Offset, payload.Length + payload.Offset);
            Assert.AreEqual(expected, payload.GetHashCode());
        }

        [Test]
        public void SetData_ThrowsArgumentExceptionWhenOffsetIsLessThan0()
        {
            var payload = new Payload();
            Assert.Throws<ArgumentException>(() => {
                payload.SetData(this.data, -2);
            });
        }

        [Test]
        public void SetData_ThrowsArgumentExceptionForLength()
        {
            var payload = new Payload();
            Assert.Throws<ArgumentException>(() => {
                payload.SetData(this.data, 20, 10);
            });
        }
    }
}
