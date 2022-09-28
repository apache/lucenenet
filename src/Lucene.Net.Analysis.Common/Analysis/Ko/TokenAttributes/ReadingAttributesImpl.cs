using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Ko.TokenAttributes
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
    /// Attribute for Kuromoji reading data
    /// </summary>
    public class ReadingAttribute : Attribute, IReadingAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private Token token;
        public virtual string GetReading() {
            return token == null ? null : token.GetReading();
        }

        public string GetPronunciation() => throw new System.NotImplementedException();

        public virtual void SetToken(Token token) {
            this.token = token;
        }

        public override void Clear()
        {
            token = null;
        }

        public override void CopyTo(IAttribute attribute) {
            ReadingAttribute t = (ReadingAttribute) attribute;
            t.SetToken(token);
        }

        public override void ReflectWith(IAttributeReflector reflector) {
            reflector.Reflect<IReadingAttribute>("reading", GetReading());
        }
    }
}
