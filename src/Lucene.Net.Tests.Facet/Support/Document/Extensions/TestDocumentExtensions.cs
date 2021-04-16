using Lucene.Net.Attributes;
using Lucene.Net.Facet;
using Lucene.Net.Facet.SortedSet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
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
        public void TestAddSortedSetDocValuesFacetField()
        {
            SortedSetDocValuesFacetField field = null;
            AssertDocumentExtensionAddsToDocument(document => field = document.AddSortedSetDocValuesFacetField("theDim", "theLabel"));
            Assert.AreEqual("theDim", field.Dim);
            Assert.AreEqual("theLabel", field.Label);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddAssociationFacetField()
        {
            AssociationFacetField field = null;
            BytesRef assoc = new BytesRef("theAssoc");
            string[] path = new[] { "thePath0", "thePath1", "thePath2" };
            AssertDocumentExtensionAddsToDocument(document => field = document.AddAssociationFacetField(assoc, "theDim", path));
            Assert.AreSame(assoc, field.Assoc);
            Assert.AreEqual("theDim", field.Dim);
            Assert.AreEqual(path, field.Path);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddInt32AssociationFacetField()
        {
            Int32AssociationFacetField field = null;
            int assoc = 1234;
            string[] path = new[] { "thePath0", "thePath1", "thePath2" };
            AssertDocumentExtensionAddsToDocument(document => field = document.AddInt32AssociationFacetField(assoc, "theDim", path));
            Assert.AreEqual(Int32AssociationFacetField.Int32ToBytesRef(assoc), field.Assoc);
            Assert.AreEqual("theDim", field.Dim);
            Assert.AreEqual(path, field.Path);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddSingleAssociationFacetField()
        {
            SingleAssociationFacetField field = null;
            float assoc = 1234.5678f;
            string[] path = new[] { "thePath0", "thePath1", "thePath2" };
            AssertDocumentExtensionAddsToDocument(document => field = document.AddSingleAssociationFacetField(assoc, "theDim", path));
            Assert.AreEqual(SingleAssociationFacetField.SingleToBytesRef(assoc), field.Assoc);
            Assert.AreEqual("theDim", field.Dim);
            Assert.AreEqual(path, field.Path);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestAddFacetField()
        {
            FacetField field = null;
            string[] path = new[] { "thePath0", "thePath1", "thePath2" };
            AssertDocumentExtensionAddsToDocument(document => field = document.AddFacetField("theDim", path));
            Assert.AreEqual("theDim", field.Dim);
            Assert.AreEqual(path, field.Path);
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
