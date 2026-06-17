using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Tests.TestFramework
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

    /// <summary>
    /// Verifies that <see cref="Lucene.Net.Util.LuceneTestFrameworkInitializer"/> wires the NUnit
    /// exception types down into <c>Lucene.ExceptionExtensions</c> during initialization. The
    /// exception-classification logic in <c>Lucene.Net</c> (e.g. <c>IsException()</c>,
    /// <c>IsAssertionError()</c>) depends on these mappings to treat NUnit's exceptions correctly in
    /// catch blocks, so if a future NUnit upgrade renamed or moved one of these types the mapping
    /// would silently become <c>null</c>. These tests guard the wiring directly.
    /// <para/>
    /// The initializer has already run for this test assembly by the time these tests execute (via
    /// the <see cref="Startup"/> <c>SetUpFixture</c>).
    /// </summary>
    [TestFixture, LuceneNetSpecific]
    public class TestLuceneTestFrameworkInitializer
    {
        [Test]
        public void NUnitExceptionTypesAreMappedAfterInitialization()
        {
            Assert.AreEqual(typeof(NUnit.Framework.ResultStateException),
                Lucene.ExceptionExtensions.NUnitResultStateExceptionType);
            Assert.AreEqual(typeof(NUnit.Framework.AssertionException),
                Lucene.ExceptionExtensions.NUnitAssertionExceptionType);
            Assert.AreEqual(typeof(NUnit.Framework.MultipleAssertException),
                Lucene.ExceptionExtensions.NUnitMultipleAssertExceptionType);
            Assert.AreEqual(typeof(NUnit.Framework.InconclusiveException),
                Lucene.ExceptionExtensions.NUnitInconclusiveExceptionType);
            Assert.AreEqual(typeof(NUnit.Framework.SuccessException),
                Lucene.ExceptionExtensions.NUnitSuccessExceptionType);
        }

        [Test]
        public void NUnitInvalidPlatformExceptionTypeIsMappedAfterInitialization()
        {
            // This is an internal NUnit type loaded by name; if NUnit moves or renames it, the
            // mapping becomes null and we should know.
            Type expected = Type.GetType("NUnit.Framework.Internal.InvalidPlatformException, NUnit.Framework");
            Assert.IsNotNull(expected, "Expected to resolve NUnit's InvalidPlatformException type by name.");
            Assert.AreEqual(expected, Lucene.ExceptionExtensions.NUnitInvalidPlatformException);
        }
    }
}
