namespace Lucene.Net.Index
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
    /// Expert: Provides a low-level means of accessing the stored field
    /// values in an index.  See <see cref="IndexReader.Document(int, StoredFieldVisitor)"/>.
    ///
    /// <para/><b>NOTE</b>: a <see cref="StoredFieldVisitor"/> implementation
    /// should not try to load or visit other stored documents in
    /// the same reader because the implementation of stored
    /// fields for most codecs is not reeentrant and you will see
    /// strange exceptions as a result.
    ///
    /// <para/>See <see cref="Documents.DocumentStoredFieldVisitor"/>, which is a
    /// <see cref="StoredFieldVisitor"/> that builds the
    /// <see cref="Documents.Document"/> containing all stored fields.  This is
    /// used by <see cref="IndexReader.Document(int)"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class StoredFieldVisitor
    {
        /// <summary>
        /// Sole constructor. (For invocation by subclass
        /// constructors, typically implicit.)
        /// </summary>
        protected StoredFieldVisitor()
        {
        }

        /// <summary>
        /// Process a binary field. </summary>
        /// <param name="value"> newly allocated byte array with the binary contents.  </param>
        public virtual void BinaryField(FieldInfo fieldInfo, byte[] value)
        {
        }

        /// <summary>
        /// Process a <see cref="string"/> field </summary>
        public virtual void StringField(FieldInfo fieldInfo, string value)
        {
        }

        /// <summary>
        /// Process a <see cref="int"/> numeric field. </summary>
        public virtual void Int32Field(FieldInfo fieldInfo, int value) // LUCENENET specific: Renamed from IntField to Int32Field per .NET conventions
        {
        }

        /// <summary>
        /// Process a <see cref="long"/> numeric field. </summary>
        public virtual void Int64Field(FieldInfo fieldInfo, long value) // LUCENENET specific: Renamed from LongField to Int64Field per .NET conventions
        {
        }

        /// <summary>
        /// Process a <see cref="float"/> numeric field. </summary>
        public virtual void SingleField(FieldInfo fieldInfo, float value) // LUCENENET specific: Renamed from FloatField to SingleField per .NET conventions
        {
        }

        /// <summary>
        /// Process a <see cref="double"/> numeric field. </summary>
        public virtual void DoubleField(FieldInfo fieldInfo, double value)
        {
        }

        /// <summary>
        /// Hook before processing a field.
        /// Before a field is processed, this method is invoked so that
        /// subclasses can return a <see cref="Status"/> representing whether
        /// they need that particular field or not, or to stop processing
        /// entirely.
        /// </summary>
        public abstract Status NeedsField(FieldInfo fieldInfo);

        /// <summary>
        /// Enumeration of possible return values for <see cref="NeedsField(FieldInfo)"/>.
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// YES: the field should be visited. </summary>
            YES,

            /// <summary>
            /// NO: don't visit this field, but continue processing fields for this document. </summary>
            NO,

            /// <summary>
            /// STOP: don't visit this field and stop processing any other fields for this document. </summary>
            STOP
        }
    }
}