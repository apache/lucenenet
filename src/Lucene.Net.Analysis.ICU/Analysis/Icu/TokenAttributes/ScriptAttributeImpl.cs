using ICU4N.Globalization;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Icu.TokenAttributes
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
    /// Implementation of <see cref="IScriptAttribute"/> that stores the script
    /// as an integer.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class ScriptAttribute : Attribute, IScriptAttribute // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        private int code = UScript.Common;

        /// <summary>Initializes this attribute with <see cref="UScript.Common"/>.</summary>
        public ScriptAttribute() { }

        public virtual int Code
        {
            get => code;
            set => code = value;
        }

        public virtual string GetName()
        {
            return UScript.GetName(code);
        }

        [ExceptionToNetNumericConvention]
        public virtual string GetShortName()
        {
            return UScript.GetShortName(code);
        }

        public override void Clear()
        {
            code = UScript.Common;
        }

        public override void CopyTo(IAttribute target) // LUCENENET specific - intentionally expanding target to use IAttribute rather than Attribute
        {
            // LUCENENET: Added guard clauses
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            if (target is not IScriptAttribute t)
                throw new ArgumentException($"Argument type {target.GetType().FullName} must implement {nameof(IScriptAttribute)}", nameof(target));
            t.Code = code;
        }

        public override bool Equals(object other)
        {
            if (this == other)
            {
                return true;
            }

            if (other is ScriptAttribute scriptAttribute)
            {
                return scriptAttribute.code == code;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return code;
        }

        public override void ReflectWith(IAttributeReflector reflector)
        {
            // LUCENENET: Added guard clause
            if (reflector is null)
                throw new ArgumentNullException(nameof(reflector));

            // when wordbreaking CJK, we use the 15924 code Japanese (Han+Hiragana+Katakana) to 
            // mark runs of Chinese/Japanese. our use is correct (as for chinese Han is a subset), 
            // but this is just to help prevent confusion.
            string name = code == UScript.Japanese ? "Chinese/Japanese" : GetName();
            reflector.Reflect<IScriptAttribute>("script", name);
        }
    }
}
