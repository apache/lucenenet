using System;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Documents
{
    // javadocs
    // javadocs
    
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
    /// A field whose value is stored so that {@link
    ///  IndexSearcher#doc} and <seealso cref="IndexReader#document"/> will
    ///  return the field and its value.
    /// </summary>
    public sealed class StoredField : Field
    {
        /// <summary>
        /// Type for a stored-only field.
        /// </summary>
        public static readonly FieldType TYPE;

        static StoredField()
        {
            TYPE = new FieldType {IsStored = true};
            TYPE.Freeze();
        }

        /// <summary>
        /// Create a stored-only field with the given binary value.
        /// <p>NOTE: the provided byte[] is not copied so be sure
        /// not to change it until you're done with this field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> byte array pointing to binary content (not copied) </param>
        /// <exception cref="ArgumentException"> if the field name is null. </exception>
        public StoredField(string name, byte[] value)
            : base(name, value, TYPE)
        {
        }

        /// <summary>
        /// Create a stored-only field with the given binary value.
        /// <p>NOTE: the provided byte[] is not copied so be sure
        /// not to change it until you're done with this field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> byte array pointing to binary content (not copied) </param>
        /// <param name="offset"> starting position of the byte array </param>
        /// <param name="length"> valid length of the byte array </param>
        /// <exception cref="ArgumentException"> if the field name is null. </exception>
        public StoredField(string name, byte[] value, int offset, int length)
            : base(name, value, offset, length, TYPE)
        {
        }

        /// <summary>
        /// Create a stored-only field with the given binary value.
        /// <p>NOTE: the provided BytesRef is not copied so be sure
        /// not to change it until you're done with this field. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> BytesRef pointing to binary content (not copied) </param>
        /// <exception cref="ArgumentException"> if the field name is null. </exception>
        public StoredField(string name, BytesRef value)
            : base(name, value, TYPE)
        {
        }

        /// <summary>
        /// Create a stored-only field with the given string value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> string value </param>
        /// <exception cref="ArgumentException"> if the field name or value is null. </exception>
        public StoredField(string name, string value)
            : base(name, value, TYPE)
        {
        }

        // TODO: not great but maybe not a big problem?
        /// <summary>
        /// Create a stored-only field with the given integer value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> integer value </param>
        /// <exception cref="ArgumentException"> if the field name is null. </exception>
        public StoredField(string name, int value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }

        /// <summary>
        /// Create a stored-only field with the given float value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> float value </param>
        /// <exception cref="ArgumentException"> if the field name is null. </exception>
        public StoredField(string name, float value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }

        /// <summary>
        /// Create a stored-only field with the given long value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> long value </param>
        /// <exception cref="ArgumentException"> if the field name is null. </exception>
        public StoredField(string name, long value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }

        /// <summary>
        /// Create a stored-only field with the given double value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> double value </param>
        /// <exception cref="ArgumentException"> if the field name is null. </exception>
        public StoredField(string name, double value)
            : base(name, TYPE)
        {
            fieldsData = value;
        }
    }
}