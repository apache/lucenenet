// LUCENENET TODO: Port issues - missing Normalizer2 dependency from icu.net

//using Icu;
//using Lucene.Net.Support;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

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

//    public sealed class ICUFoldingFilter : ICUNormalizer2Filter
//    {
//        private static readonly Normalizer2 normalizer;

//        /// <summary>
//        /// Create a new ICUFoldingFilter on the specified input
//        /// </summary>
//        public ICUFoldingFilter(TokenStream input)
//            : base(input, normalizer)
//        {
//        }

//        static ICUFoldingFilter()
//        {
//            normalizer = Normalizer2.GetInstance(
//                typeof(ICUFoldingFilter).Assembly.FindAndGetManifestResourceStream(typeof(ICUFoldingFilter), "utr30.nrm"),
//                "utr30", Normalizer2.Mode.COMPOSE);
//        }
//    }
//}
