using System;
using NUnit.Framework;

namespace Lucene.Net.Util
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

    [TestFixture]
    public class TestNamedSPILoader : LuceneTestCase
    {
        [Test]
        public void Lookup_ExistingService_ReturnServiceByName()
        {
            // Arrange
            var loader = CreateNamedSPILoaderForCodecClass();

            // Act
            var codec = loader.Lookup("CustomCodec1");

            // Assert
            Assert.IsInstanceOf<CustomCodec1>(codec);
            Assert.AreEqual("CustomCodec1", codec.Name);
        }

        [Test]
        public void Lookup_NonexistingService_ThrowsException()
        {
            // Arrange
            var loader = CreateNamedSPILoaderForCodecClass();

            // Act
            var actualException = Assert.Throws<ArgumentException>(() => loader.Lookup("NonexistingCodecName"));

            // Assert
            const string expectedMessage = "An SPI class of type CustomCodec with name 'NonexistingCodecName' does not exist. "
                + "You need to reference the corresponding assembly that contains the class. "
                + "The current NamedSPILoader supports the following names: CustomCodec1, CustomCodec2";
            Assert.AreEqual(expectedMessage, actualException.Message);
        }

        [Test]
        public void AvailableServices_LoaderWithServices_ReturnsListOfNamesOfRegisteredServices()
        {
            // Arrange
            var expectedServices = new [] { "CustomCodec1", "CustomCodec2" };
            var loader = CreateNamedSPILoaderForCodecClass();

            // Act
            var actualServices = loader.AvailableServices();

            // Assert
            Assert.IsNotNull(actualServices);
            CollectionAssert.IsNotEmpty(actualServices);
            CollectionAssert.AreEqual(expectedServices, actualServices);
        }

        private static NamedSPILoader<CustomCodec> CreateNamedSPILoaderForCodecClass()
        {
            return new NamedSPILoader<CustomCodec>(typeof(CustomCodec));
        }

        private abstract class CustomCodec : NamedSPILoader<CustomCodec>.NamedSPI
        {
            public string Name
            {
                get
                {
                    return this.GetType().Name;
                }
            }
        }

        private class CustomCodec1 : CustomCodec
        {
        }

        private class CustomCodec2 : CustomCodec
        {
        }
    }
}