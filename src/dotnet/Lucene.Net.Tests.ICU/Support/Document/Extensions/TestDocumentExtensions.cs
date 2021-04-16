using ICU4N.Text;
using Lucene.Net.Attributes;
using Lucene.Net.Collation;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Documents.Extensions
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

    public class TestDocumentExtensions : LuceneTestCase
    {
        [Test]
        [LuceneNetSpecific]
        public void TestAddICUCollationDocValuesField()
        {
            ICUCollationDocValuesField field = null;
            Collator collator = Collator.GetInstance(new CultureInfo("en"));
            AssertDocumentExtensionAddsToDocument(document => field = document.AddICUCollationDocValuesField("theName", collator));
            Assert.AreEqual("theName", field.Name);
            Assert.AreEqual(collator, field.collator); // Collator is cloned, so we don't expect them to be the same instance
        }

        private void AssertDocumentExtensionAddsToDocument<T>(Func<Document, T> extension) where T : IIndexableField
        {
            var document = new Document();
            var field = extension(document);
            Assert.IsNotNull(field);
            Assert.AreEqual(1, document.Fields.Count);
            Assert.AreSame(field, document.Fields[0]);

            document = null;
            Assert.Throws<ArgumentNullException>(() => extension(document));
        }
    }
}
