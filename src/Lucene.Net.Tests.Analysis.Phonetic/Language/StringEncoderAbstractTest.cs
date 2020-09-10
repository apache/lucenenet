using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Phonetic.Language
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

    public abstract class StringEncoderAbstractTest<T>
        where T : IStringEncoder
    {
        protected T stringEncoder;

        [SetUp]
        public void SetUp()
        {
            stringEncoder = this.CreateStringEncoder();
        }

        public virtual void CheckEncoding(string expected, string source)
        {
            Assert.AreEqual(expected, this.StringEncoder.Encode(source), "Source: " + source);
        }

        protected virtual void CheckEncodings(string[][] data)
        {
            foreach (string[]
                element in data)
            {
                this.CheckEncoding(element[1], element[0]);
            }
        }

        protected virtual void CheckEncodingVariations(string expected, string[] data)
        {
            foreach (string element in data)
            {
                this.CheckEncoding(expected, element);
            }
        }

        protected abstract T CreateStringEncoder();

        public virtual T StringEncoder => this.stringEncoder;

        [Test]
        public virtual void TestEncodeEmpty()
        {
            IStringEncoder encoder = this.StringEncoder;
            encoder.Encode("");
            encoder.Encode(" ");
            encoder.Encode("\t");
        }

        // LUCENENET specific - since strings are sealed in .NET, there
        // is no point in implementing IEncoder or running these tests.
        // Our version only accepts strings 
        [Test]
        public virtual void TestEncodeNull()
        {
            IStringEncoder encoder = this.StringEncoder;
            try
            {
                encoder.Encode(null);
            }
#pragma warning disable 168
            catch (/*Encoder*/Exception ee)
#pragma warning restore 168
            {
                // An exception should be thrown
            }
        }

        //[Test]
        //public virtual void TestEncodeWithInvalidObject()
        //{
        //    bool exceptionThrown = false;
        //    try
        //    {
        //        IStringEncoder encoder = this.StringEncoder;
        //        encoder.Encode(3.4f);
        //    }
        //    catch (Exception e)
        //    {
        //        exceptionThrown = true;
        //    }
        //    Assert.True(exceptionThrown, "An exception was not thrown when we tried to encode " + "a Float object");
        //}

        [Test]
        public virtual void TestLocaleIndependence()
        {
            IStringEncoder encoder = this.StringEncoder;

            string[]
            data = { "I", "i", };

            CultureInfo orig = CultureInfo.CurrentCulture;
            CultureInfo[] locales = { new CultureInfo("en"), new CultureInfo("tr"), CultureInfo.CurrentCulture };

            try
            {
                foreach (string element in data)
                {
                    string @ref = null;
                    for (int j = 0; j < locales.Length; j++)
                    {
                        //Locale.setDefault(locales[j]);
#if FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
                        CultureInfo.CurrentCulture = locales[j];
#else
                        Thread.CurrentThread.CurrentCulture = locales[j];
#endif
                        if (j <= 0)
                        {
                            @ref = encoder.Encode(element);
                        }
                        else
                        {
                            string cur = null;
                            try
                            {
                                cur = encoder.Encode(element);
                            }
                            catch (Exception e)
                            {
                                Assert.Fail(CultureInfo.CurrentCulture.ToString() + ": " + e.Message);
                            }
                            Assert.AreEqual(@ref, cur, CultureInfo.CurrentCulture.ToString() + ": ");
                        }
                    }
                }
            }
            finally
            {
                //Locale.setDefault(orig);
#if FEATURE_CULTUREINFO_CURRENTCULTURE_SETTER
                CultureInfo.CurrentCulture = orig;
#else
                Thread.CurrentThread.CurrentCulture = orig;
#endif
            }
        }
    }
}
