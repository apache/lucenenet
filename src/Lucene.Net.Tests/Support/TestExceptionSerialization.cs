/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestExceptionSerialization
    {
        public IEnumerable<object> ExceptionTestData 
        {
            get
            {
                return typeof(NoSuchDirectoryException).Assembly.GetTypes().Where(t => typeof(Exception).IsAssignableFrom(t)).Cast<object>();
            }
        }

        [Test]
        public void NoSuchDirectoryExceptionCanBeDeserialized()
        {
            var exception = new NoSuchDirectoryException("Message text");
            Assert.That(TypeCanSerialize(exception));
        }

        [Test]
        public void AllExceptionsInLuceneNamespaceCanSerialize([ValueSource("ExceptionTestData")]Type luceneException)
        {
            var instance = TryInstantiate(luceneException);
            Assert.That(TypeCanSerialize(instance), string.Format("Unable to serialize {0}", luceneException.FullName));
        }

        private static bool TypeCanSerialize<T>(T exception)
        {
            T clone;

            try
            {
                var binaryFormatter = new BinaryFormatter();
                using (var serializationStream = new MemoryStream())
                {
                    binaryFormatter.Serialize(serializationStream, exception);
                    serializationStream.Seek(0, SeekOrigin.Begin);
                    clone = (T)binaryFormatter.Deserialize(serializationStream);
                }
            }
            catch (SerializationException)
            {
                return false;
            }

            return true;
        }

        private static object TryInstantiate(Type type)
        {
            object instance = null;
            try
            {
                instance = Activator.CreateInstance(type, new object[] { "A message" });
            }
            catch (MissingMethodException)
            {
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (MissingMethodException)
                {
                    Assert.Fail("Can't instantiate type {0}, it's missing the necessary constructors.", type.FullName);
                }
            }
            return instance;
        }
    }
}