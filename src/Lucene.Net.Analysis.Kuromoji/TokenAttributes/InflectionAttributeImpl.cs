using Lucene.Net.Analysis.Ja.Util;
using Lucene.Net.Util;
using System;
using Attribute = Lucene.Net.Util.Attribute;

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
    public class InflectionAttribute : Attribute, IInflectionAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private Token token;

        public virtual string GetInflectionType()
        {
            return token?.GetInflectionType();
        }

        public virtual string GetInflectionForm()
        {
            return token?.GetInflectionForm();
        }

        public virtual void SetToken(Token token)
        {
            this.token = token;
        }

        public override void Clear()
        {
            token = null;
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IInflectionAttribute t)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IInflectionAttribute)}", nameof(target));
            t.SetToken(token);
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            // LUCENENET: Added guard clause
            if (reflector is null)
                throw new ArgumentNullException(nameof(reflector));

            string type = GetInflectionType();
            string typeEN = type is null ? null : ToStringUtil.GetInflectionTypeTranslation(type);
            reflector.Reflect<IInflectionAttribute>("inflectionType", type);
            reflector.Reflect<IInflectionAttribute>("inflectionType (en)", typeEN);
            string form = GetInflectionForm();
            string formEN = form is null ? null : ToStringUtil.GetInflectedFormTranslation(form);
            reflector.Reflect<IInflectionAttribute>("inflectionForm", form);
            reflector.Reflect<IInflectionAttribute>("inflectionForm (en)", formEN);
        }
    }
}
