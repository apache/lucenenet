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
    public class PayloadAttribute : Attribute, IPayloadAttribute, ICloneable
    {
        private BytesRef Payload_Renamed;

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
            this.Payload_Renamed = payload;
        }

        public BytesRef Payload
        {
            get
            {
                return this.Payload_Renamed;
            }
            set
            {
                this.Payload_Renamed = value;
            }
        }

        public override void Clear()
        {
            Payload_Renamed = null;
        }

        public override object Clone()
        {
            PayloadAttribute clone = (PayloadAttribute)base.Clone();
            if (Payload_Renamed != null)
            {
                clone.Payload_Renamed = (BytesRef)Payload_Renamed.Clone();
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
                if (o.Payload_Renamed == null || Payload_Renamed == null)
                {
                    return o.Payload_Renamed == null && Payload_Renamed == null;
                }

                return o.Payload_Renamed.Equals(Payload_Renamed);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (Payload_Renamed == null) ? 0 : Payload_Renamed.GetHashCode();
        }

        public override void CopyTo(Attribute target)
        {
            PayloadAttribute t = (PayloadAttribute)target;
            t.Payload = (Payload_Renamed == null) ? null : (BytesRef)Payload_Renamed.Clone();
        }
    }
}