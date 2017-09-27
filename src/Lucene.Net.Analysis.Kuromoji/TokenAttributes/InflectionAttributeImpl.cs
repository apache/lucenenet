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
    /// Attribute for Kuromoji inflection data.
    /// </summary>
    public class InflectionAttribute : Attribute, IInflectionAttribute
#if FEATURE_CLONEABLE
        , System.ICloneable
#endif
    {
        private Token token;

        public virtual string GetInflectionType()
        {
            return token == null ? null : token.GetInflectionType();
        }

        public virtual string GetInflectionForm()
        {
            return token == null ? null : token.GetInflectionForm();
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
            InflectionAttribute t = (InflectionAttribute)target;
            t.SetToken(token);
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            string type = GetInflectionType();
            string typeEN = type == null ? null : ToStringUtil.GetInflectionTypeTranslation(type);
            reflector.Reflect<IInflectionAttribute>("inflectionType", type);
            reflector.Reflect<IInflectionAttribute>("inflectionType (en)", typeEN);
            string form = GetInflectionForm();
            string formEN = form == null ? null : ToStringUtil.GetInflectedFormTranslation(form);
            reflector.Reflect<IInflectionAttribute>("inflectionForm", form);
            reflector.Reflect<IInflectionAttribute>("inflectionForm (en)", formEN);
        }
    }
}
