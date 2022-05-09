using Lucene.Net.Util;
using System;
using Double = J2N.Numerics.Double;
using Int32 = J2N.Numerics.Int32;
using Int64 = J2N.Numerics.Int64;
using Single = J2N.Numerics.Single;

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
    /// A field whose value is stored so that 
    /// <see cref="Search.IndexSearcher.Doc(int)"/> and <see cref="Index.IndexReader.Document(int)"/> will
    /// return the field and its value.
    /// </summary>
    public sealed class StoredField : Field
    {
        /// <summary>
        /// Type for a stored-only field.
        /// </summary>
        // LUCENENET: Avoid static constructors (see https://github.com/apache/lucenenet/pull/224#issuecomment-469284006)
        public static readonly FieldType TYPE = new FieldType
        {
            IsStored = true
        }.Freeze();

        /// <summary>
        /// Create a stored-only field with the given binary value.
        /// <para>NOTE: the provided <see cref="T:byte[]"/> is not copied so be sure
        /// not to change it until you're done with this field.</para>
        /// </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> byte array pointing to binary content (not copied) </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public StoredField(string name, byte[] value)
            : base(name, value, TYPE)
        {
        }

        /// <summary>
        /// Create a stored-only field with the given binary value.
        /// <para>NOTE: the provided <see cref="T:byte[]"/> is not copied so be sure
        /// not to change it until you're done with this field.</para>
        /// </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="byte"/> array pointing to binary content (not copied) </param>
        /// <param name="offset"> starting position of the byte array </param>
        /// <param name="length"> valid length of the byte array </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public StoredField(string name, byte[] value, int offset, int length)
            : base(name, value, offset, length, TYPE)
        {
        }

        /// <summary>
        /// Create a stored-only field with the given binary value.
        /// <para>NOTE: the provided <see cref="BytesRef"/> is not copied so be sure
        /// not to change it until you're done with this field.</para>
        /// </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="BytesRef"/> pointing to binary content (not copied) </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public StoredField(string name, BytesRef value)
            : base(name, value, TYPE)
        {
        }

        /// <summary>
        /// Create a stored-only field with the given <see cref="string"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="string"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> or <paramref name="value"/> is <c>null</c>. </exception>
        public StoredField(string name, string value)
            : base(name, value, TYPE)
        {
        }

        // TODO: not great but maybe not a big problem?
        /// <summary>
        /// Create a stored-only field with the given <see cref="int"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="int"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public StoredField(string name, int value)
            : base(name, TYPE)
        {
            FieldsData = Int32.GetInstance(value);
        }

        /// <summary>
        /// Create a stored-only field with the given <see cref="float"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="float"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public StoredField(string name, float value)
            : base(name, TYPE)
        {
            FieldsData = Single.GetInstance(value);
        }

        /// <summary>
        /// Create a stored-only field with the given <see cref="long"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="long"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public StoredField(string name, long value)
            : base(name, TYPE)
        {
            FieldsData = Int64.GetInstance(value);
        }

        /// <summary>
        /// Create a stored-only field with the given <see cref="double"/> value. </summary>
        /// <param name="name"> field name </param>
        /// <param name="value"> <see cref="double"/> value </param>
        /// <exception cref="ArgumentNullException"> if the field <paramref name="name"/> is <c>null</c>. </exception>
        public StoredField(string name, double value)
            : base(name, TYPE)
        {
            FieldsData = Double.GetInstance(value);
        }
    }
}