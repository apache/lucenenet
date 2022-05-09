using Lucene.Net.Index;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Documents
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
    /// A <see cref="StoredFieldVisitor"/> that creates a 
    /// <see cref="Document"/> containing all stored fields, or only specific
    /// requested fields provided to <see cref="DocumentStoredFieldVisitor.DocumentStoredFieldVisitor(ISet{string})"/>.
    /// <para/>
    /// This is used by <see cref="IndexReader.Document(int)"/> to load a
    /// document.
    ///
    /// @lucene.experimental
    /// </summary>
    public class DocumentStoredFieldVisitor : StoredFieldVisitor
    {
        private readonly Document doc = new Document();
        private readonly ISet<string> fieldsToAdd;

        /// <summary>
        /// Load only fields named in the provided <see cref="ISet{String}"/>. </summary>
        /// <param name="fieldsToAdd"> Set of fields to load, or <c>null</c> (all fields). </param>
        public DocumentStoredFieldVisitor(ISet<string> fieldsToAdd)
        {
            this.fieldsToAdd = fieldsToAdd;
        }

        /// <summary>
        /// Load only fields named in the provided fields. </summary>
        public DocumentStoredFieldVisitor(params string[] fields)
        {
            fieldsToAdd = new JCG.HashSet<string>();
            foreach (string field in fields)
            {
                fieldsToAdd.Add(field);
            }
        }

        /// <summary>
        /// Load all stored fields. </summary>
        public DocumentStoredFieldVisitor()
        {
            this.fieldsToAdd = null;
        }

        public override void BinaryField(FieldInfo fieldInfo, byte[] value)
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void StringField(FieldInfo fieldInfo, string value)
        {
            FieldType ft = new FieldType(TextField.TYPE_STORED);
            ft.StoreTermVectors = fieldInfo.HasVectors;
            ft.IsIndexed = fieldInfo.IsIndexed;
            ft.OmitNorms = fieldInfo.OmitsNorms;
            ft.IndexOptions = fieldInfo.IndexOptions;
            doc.Add(new Field(fieldInfo.Name, value, ft));
        }

        public override void Int32Field(FieldInfo fieldInfo, int value) // LUCENENET specific: renamed from IntField to follow .NET conventions
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void Int64Field(FieldInfo fieldInfo, long value) // LUCENENET specific: renamed from LongField to follow  .NET conventions
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void SingleField(FieldInfo fieldInfo, float value) // LUCENENET specific: renamed from FloatField to follow  .NET conventions
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override void DoubleField(FieldInfo fieldInfo, double value)
        {
            doc.Add(new StoredField(fieldInfo.Name, value));
        }

        public override Status NeedsField(FieldInfo fieldInfo)
        {
            return fieldsToAdd is null || fieldsToAdd.Contains(fieldInfo.Name) ? Status.YES : Status.NO;
        }

        /// <summary>
        /// Retrieve the visited document. </summary>
        /// <returns> Document populated with stored fields. Note that only
        ///         the stored information in the field instances is valid,
        ///         data such as boosts, indexing options, term vector options,
        ///         etc is not set. </returns>
        public virtual Document Document => doc;
    }
}