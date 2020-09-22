namespace Lucene.Net.Analysis.TokenAttributes
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
    using IAttribute = Lucene.Net.Util.IAttribute;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Default implementation of <see cref="IPayloadAttribute"/>. </summary>
    public class PayloadAttribute : Attribute, IPayloadAttribute
#if FEATURE_CLONEABLE
        , System.ICloneable
#endif
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

        public virtual BytesRef Payload
        {
            get => this.payload;
            set => this.payload = value;
        }

        public override void Clear()
        {
            payload = null;
        }

        public override object Clone()
        {
            PayloadAttribute clone = (PayloadAttribute)base.Clone();
            if (payload is object)
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

            if (other is PayloadAttribute o)
            {
                if (o.payload is null || payload is null)
                {
                    return o.payload is null && payload is null;
                }

                return o.payload.Equals(payload);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (payload is null) ? 0 : payload.GetHashCode();
        }

        public override void CopyTo(IAttribute target)
        {
            PayloadAttribute t = (PayloadAttribute)target;
            t.Payload = (payload is null) ? null : (BytesRef)payload.Clone();
        }
    }
}