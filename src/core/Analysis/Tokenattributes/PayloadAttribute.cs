/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Lucene.Net.Util;
using System;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
    /// <summary> The payload of a Token. See also <see cref="Payload" />.</summary>
    [Serializable]
    public class PayloadAttribute : Attribute, IPayloadAttribute, ICloneable
    {
        private BytesRef payload;

        /// <summary> Initialize this attribute with no payload.</summary>
        public PayloadAttribute()
        {
        }

        /// <summary> Initialize this attribute with the given payload. </summary>
        public PayloadAttribute(BytesRef payload)
        {
            Payload = payload;
        }

        /// <summary> Returns this Token's payload.</summary>
        public virtual BytesRef Payload
        {
            get { return payload; }
            set { payload = value; }
        }

        public override void Clear()
        {
            payload = null;
        }

        public override object Clone()
        {
            var clone = (PayloadAttribute) base.Clone();
            if (Payload != null)
            {
                clone.Payload = (BytesRef)Payload.Clone();
            }
            return clone;
            // TODO: This code use to be as below.  Any reason why?  the if(payload!=null) was missing...
            //PayloadAttributeImpl impl = new PayloadAttributeImpl();
            //impl.payload = new Payload(this.payload.data, this.payload.offset, this.payload.length);
            //return impl;
        }

        public override bool Equals(object other)
        {
            if (other == this)
            {
                return true;
            }

            if (other is IPayloadAttribute)
            {
                PayloadAttribute o = (PayloadAttribute) other;
                if (o.Payload == null || Payload == null)
                {
                    return o.Payload == null && Payload == null;
                }

                return o.Payload.Equals(Payload);
            }

            return false;
        }

        public override int GetHashCode()
        {
            return (Payload == null) ? 0 : Payload.GetHashCode();
        }

        public override void CopyTo(Attribute target)
        {
            IPayloadAttribute t = (IPayloadAttribute) target;
            t.Payload = (Payload == null) ? null : (BytesRef) Payload.Clone();
        }
    }
}