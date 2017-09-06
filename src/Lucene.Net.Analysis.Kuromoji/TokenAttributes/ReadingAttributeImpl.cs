using Lucene.Net.Analysis.Ja.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Ja.TokenAttributes
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
    public class ReadingAttribute : Attribute, IReadingAttribute, System.ICloneable
    {
        private Token token;

        public virtual string GetReading()
        {
            return token == null ? null : token.GetReading();
        }

        public virtual string GetPronunciation()
        {
            return token == null ? null : token.GetPronunciation();
        }

        public virtual void SetToken(Token token)
        {
            this.token = token;
        }

        public override void Clear()
        {
            token = null;
        }

        public override void CopyTo(IAttribute target)
        {
            ReadingAttribute t = (ReadingAttribute)target;
            t.SetToken(token);
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            string reading = GetReading();
            string readingEN = reading == null ? null : ToStringUtil.GetRomanization(reading);
            string pronunciation = GetPronunciation();
            string pronunciationEN = pronunciation == null ? null : ToStringUtil.GetRomanization(pronunciation);
            reflector.Reflect<IReadingAttribute>("reading", reading);
            reflector.Reflect<IReadingAttribute>("reading (en)", readingEN);
            reflector.Reflect<IReadingAttribute>("pronunciation", pronunciation);
            reflector.Reflect<IReadingAttribute>("pronunciation (en)", pronunciationEN);
        }
    }
}
