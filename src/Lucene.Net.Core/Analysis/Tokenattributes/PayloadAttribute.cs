using System;

namespace Lucene.Net.Analysis.Tokenattributes
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

    using Attribute = Lucene.Net.Util.Attribute;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Default implementation of <seealso cref="PayloadAttribute"/>. </summary>
    public class PayloadAttribute : Attribute, IPayloadAttribute
    {
        private BytesRef payload;

        /// <summary>
        /// Initialize this attribute with no payload.
        /// </summary>
        public PayloadAttribute()
        {
        }

        /// <summary>
        /// Initialize this attribute with the given payload.
        /// </summary>
        public PayloadAttribute(BytesRef payload)
        {
            this.payload = payload;
        }

        public BytesRef Payload
        {
            get
            {
                return this.payload;
            }
            set
            {
                this.payload = value;
            }
        }

        public override void Clear()
        {
            payload = null;
        }

        public override object Clone()
        {
            PayloadAttribute clone = (PayloadAttribute)base.Clone();
            if (payload != null)
            {
                clone.payload = (BytesRef)payload.Clone();
            }
            return clone;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other is PayloadAttribute)
            {
                PayloadAttribute o = (PayloadAttribute)other;
                if (o.payload == null || payload == null)
                {
                    return o.payload == null && payload == null;
                }

                return o.payload.Equals(payload);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (payload == null) ? 0 : payload.GetHashCode();
        }

        public override void CopyTo(Attribute target)
        {
            PayloadAttribute t = (PayloadAttribute)target;
            t.Payload = (payload == null) ? null : (BytesRef)payload.Clone();
        }
    }
}