// LUCENENET TODO: Port issues - missing Transliterator dependency from icu.net

//using Lucene.Net.Analysis.Util;
//using System;
//using System.Collections.Generic;

//namespace Lucene.Net.Analysis.ICU
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

//    public class ICUTransformFilterFactory : TokenFilterFactory, IMultiTermAwareComponent
//    {
//        private readonly Transliterator transliterator;

//        // TODO: add support for custom rules
//        /// <summary>Creates a new ICUTransformFilterFactory</summary>
//        public ICUTransformFilterFactory(IDictionary<string, string> args)
//            : base(args)
//        {
//            string id = Require(args, "id");
//            string direction = Get(args, "direction", new string[] { "forward", "reverse" }, "forward", false);
//            int dir = "forward".Equals(direction) ? Transliterator.FORWARD : Transliterator.REVERSE;
//            transliterator = Transliterator.getInstance(id, dir);
//            if (args.Count != 0)
//            {
//                throw new ArgumentException("Unknown parameters: " + args);
//            }
//        }

//        public override TokenStream Create(TokenStream input)
//        {
//            return new ICUTransformFilter(input, transliterator);
//        }

//        public virtual AbstractAnalysisFactory GetMultiTermComponent()
//        {
//            return this;
//        }
//    }
//}
