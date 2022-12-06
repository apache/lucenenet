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
    /// Attribute for <see cref="Token.GetPartOfSpeech()"/>.
    /// </summary>
    public class PartOfSpeechAttribute : Attribute, IPartOfSpeechAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private Token token;

        public virtual string GetPartOfSpeech()
        {
            return token?.GetPartOfSpeech();
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
            if (target is not IPartOfSpeechAttribute t)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IPartOfSpeechAttribute)}", nameof(target));
            t.SetToken(token);
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            // LUCENENET: Added guard clause
            if (reflector is null)
                throw new ArgumentNullException(nameof(reflector));

            string partOfSpeech = GetPartOfSpeech();
            string partOfSpeechEN = partOfSpeech is null ? null : ToStringUtil.GetPOSTranslation(partOfSpeech);
            reflector.Reflect<IPartOfSpeechAttribute>("partOfSpeech", partOfSpeech);
            reflector.Reflect<IPartOfSpeechAttribute>("partOfSpeech (en)", partOfSpeechEN);
        }
    }
}
