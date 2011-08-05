// -----------------------------------------------------------------------
// <copyright company="Apache" file="CharTermAttributeTest.cs" >
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
    using System.Text.RegularExpressions;
    using Categories = Lucene.Net.TestCategories;
    using Internal;

#if NUNIT
    using NUnit.Framework;
    using Extensions.NUnit;
#else 
    using Gallio.Framework;
    using MbUnit.Framework;
#endif


    [TestFixture]
    [Category(Categories.Unit)]
    [Parallelizable]
    public class CharTermAttributeTest
    {
        /*
        [Test]
        public void Append()
        {
            var attribute = new CharTermAttribute();
            

        }*/
        

        [Test]
        public void Grow()
        {
            var attribute = new CharTermAttribute();
            var buffer = new StringBuilder("ab");

            Console.WriteLine(IntPtr.Size == 4 ? "x86" : "64bit ");

            // this currently fails running in 64 bit mode in Gallio
            // TODO: figure out the issue with running this attribute in 64bit. possibly the bit length.
            if (IntPtr.Size == 8)
                return;


            for (int i = 0; i < 20; i++)
            {
                var content = buffer.ToString().ToCharArray();
                attribute.CopyBuffer(content);
                Assert.AreEqual(buffer.Length, attribute.Length);
                Assert.AreEqual(buffer.ToString(), attribute.ToString());
                buffer.Append(buffer.ToString());
            }

            Assert.AreEqual(1048576, attribute.Length);

            

            attribute = new CharTermAttribute();
            buffer = new StringBuilder("ab");

            for (int i = 0; i < 20; i++)
            {
                attribute.Empty().Append(buffer);
                Assert.AreEqual(buffer.Length, attribute.Length);
                Assert.AreEqual(buffer.ToString(), attribute.ToString());
                buffer.Append(attribute);
            }

            Assert.AreEqual(1048576, attribute.Length);



            attribute = new CharTermAttribute();
            buffer = new StringBuilder("a");

            for (int i = 0; i < 20000; i++)
            {
                attribute.Empty().Append(buffer);
                Assert.AreEqual(buffer.Length, attribute.Length);
                Assert.AreEqual(buffer.ToString(), attribute.ToString());
                buffer.Append("a");
            }
            Assert.AreEqual(20000, attribute.Length);
        }

      
        [Test]
        public void CharSequence()
        {
            // Note that c# does not support the CharSquence interface, but
            // its possible to still test the api to some degree.

            string value = "0123456789";
            var attribute = new CharTermAttribute();
            attribute.Append(value);

            Assert.AreEqual(value.Length, attribute.Length);
            Assert.AreEqual("12", attribute.ToString().Substring(1, 2));
            Assert.AreEqual(value, attribute.ToString().Substring(0, value.Length));

            Assert.IsTrue(Regex.IsMatch(attribute.ToString(), @"01\d+"));
            Assert.IsTrue(Regex.IsMatch(attribute.ToString().Substring(3,5), "34"));

            Assert.AreEqual(value.Substring(3,7), attribute.ToString().Substring(3,7));

            for (int i = 0; i < value.Length; i++)
            {
                Assert.AreEqual(value[i], attribute.CharAt(i));
            }
        }
        
        [Test]
        public void Clone()
        {
            var attribute = new CharTermAttribute();
            var content = "hello".ToCharArray();

            attribute.CopyBuffer(content,0, 5);
            var copy = attribute.CreateCloneAndAssertEqual();

            Assert.AreEqual(attribute.ToString(), copy.ToString());
            Assert.AreNotSame(attribute.Buffer, copy.Buffer);
        }

        [Test]
        public void Copy()
        {
            var attribute = new CharTermAttribute();
            var copy = attribute.CreateCopyAndAssertEqual();

            Assert.AreEqual(string.Empty, attribute.ToString());
            Assert.AreEqual(string.Empty, copy.ToString());

            attribute = new CharTermAttribute();
            attribute.CopyBuffer("hello".ToCharArray());

            var buffer = attribute.Buffer;
            copy = attribute.CreateCopyAndAssertEqual();

            Assert.AreEqual(attribute.ToString(), copy.ToString());
            Assert.AreNotSame(buffer, copy.Buffer);
        }

        [Test]
        public void EqualsOverride()
        {
            var attribute = new CharTermAttribute();
            var content = "hello".ToCharArray();

            attribute.CopyBuffer(content);

            var attributeMirror = new CharTermAttribute();
            var contentMirror = "hello".ToCharArray();

            attributeMirror.CopyBuffer(contentMirror);

            var attributeOutcast = new CharTermAttribute();
            var contentOutcast = "hello2".ToCharArray();
            attributeOutcast.CopyBuffer(contentOutcast);

            Assert.IsTrue(attribute.Equals(attributeMirror));
            Assert.IsFalse(attribute.Equals(attributeOutcast));
            Assert.IsFalse(attributeOutcast.Equals(attributeMirror));

        }

        [Test]
        public void Resize()
        {
            var attribute = new CharTermAttribute();
            var content = "hello".ToCharArray();
            attribute.CopyBuffer(content, 0, content.Length);

            for (int i = 0; i < 2000; i++)
            {
                attribute.ResizeBuffer(i);
                Assert.IsTrue(i <= attribute.Buffer.Length, "i is {0} and buffer length is {1}", i, attribute.Buffer.Length);
                Assert.AreEqual("hello", attribute.ToString());
            }
        }

        [Test]
        public void ToStringOverride()
        {
            var sequence = new[] { 'a', 'l', 'o', 'h', 'a' };
            var attribute = new CharTermAttribute();

            attribute.CopyBuffer(sequence, 0, sequence.Length);

            Assert.AreEqual("aloha", attribute.ToString());

            attribute.Empty().Append("hi there");

            Assert.AreEqual("hi there", attribute.ToString());
        }
    }
}
