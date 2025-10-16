using Lucene.Net.Analysis.Ko.Dict;
using Lucene.Net.Util;
using System.Text;

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
    /// Attribute for <see cref="Token.GetPartOfSpeech()"/>.
    /// </summary>
    public class PartOfSpeechAttribute : Attribute, IPartOfSpeechAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private Token token;

        public virtual POS.Type GetPOSType() {
            return token.GetPOSType();
        }

        public virtual POS.Tag GetLeftPOS() {
            return token?.GetLeftPOS();
        }

        public virtual POS.Tag GetRightPOS() {
            return token?.GetRightPOS();
        }

        public virtual IDictionary.Morpheme[] GetMorphemes() {
            return token?.GetMorphemes();
        }

        public virtual void SetToken(Token token) {
            this.token = token;
        }

        public override void Clear() {
            token = null;
        }

        public override void ReflectWith(IAttributeReflector reflector) {
            string posName = GetPOSType() == null ? null : GetPOSType().ToString();
            string rightPOS = GetRightPOS() == null ? null : GetRightPOS().Name + "(" + GetRightPOS().Description + ")";
            string leftPOS = GetLeftPOS() == null ? null : GetLeftPOS().Name + "(" + GetLeftPOS().Description + ")";
            reflector.Reflect<PartOfSpeechAttribute>("posType", posName);
            reflector.Reflect<PartOfSpeechAttribute>("leftPOS", leftPOS);
            reflector.Reflect<PartOfSpeechAttribute>("rightPOS", rightPOS);
            reflector.Reflect<PartOfSpeechAttribute>("morphemes", DisplayMorphemes(GetMorphemes()));
        }

        private string DisplayMorphemes(IDictionary.Morpheme[] morphemes) {
            if (morphemes == null) {
                return null;
            }
            StringBuilder builder = new StringBuilder();
            foreach (var morpheme in morphemes)
            {
                builder.Append(morpheme.surfaceForm).Append('/').Append(morpheme.posTag.Name).Append('(').Append(morpheme.posTag.Description).Append(')');
            }
            return builder.ToString();
        }

        public override void CopyTo(IAttribute target) {
            PartOfSpeechAttribute t = (PartOfSpeechAttribute) target;
            t.SetToken(token);
        }
    }
}
