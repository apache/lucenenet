// LUCENENET TODO: Port issues - missing dependencies

//using Lucene.Net.Util;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Lucene.Net.Analysis.ICU.TokenAttributes
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// Implementation of <see cref="IScriptAttribute"/> that stores the script
//    /// as an integer.
//    /// <para/>
//    /// @lucene.experimental
//    /// </summary>
//    public class ScriptAttribute : Attribute, IScriptAttribute, System.ICloneable
//    {
//        private int code = UScript.COMMON;

//        /** Initializes this attribute with <code>UScript.COMMON</code> */
//        public ScriptAttribute() { }

//        public virtual int Code
//        {
//            get { return code; }
//            set { code = value; }
//        }

//        public virtual string GetName()
//        {
//            return UScript.GetName(code);
//        }

//        public virtual string GetShortName()
//        {
//            return UScript.GetShortName(code);
//        }

//        public override void Clear()
//        {
//            code = UScript.COMMON;
//        }

//        public override void CopyTo(IAttribute target)
//        {
//            ScriptAttribute t = (ScriptAttribute)target;
//            t.Code = code;
//        }

//        public override bool Equals(object other)
//        {
//            if (this == other)
//            {
//                return true;
//            }

//            if (other is ScriptAttribute)
//            {
//                return ((ScriptAttribute)other).code == code;
//            }

//            return false;
//        }

//        public override int GetHashCode()
//        {
//            return code;
//        }

//        public override void ReflectWith(IAttributeReflector reflector)
//        {
//            // when wordbreaking CJK, we use the 15924 code Japanese (Han+Hiragana+Katakana) to 
//            // mark runs of Chinese/Japanese. our use is correct (as for chinese Han is a subset), 
//            // but this is just to help prevent confusion.
//            string name = code == UScript.JAPANESE ? "Chinese/Japanese" : GetName();
//            reflector.Reflect<IScriptAttribute>("script", name);
//        }
//    }
//}
