// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;

namespace Lucene.Net.Analysis.Hi
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
    /// Light Stemmer for Hindi.
    /// <para>
    /// Implements the algorithm specified in:
    /// <c>A Lightweight Stemmer for Hindi</c>
    /// Ananthakrishnan Ramanathan and Durgesh D Rao.
    /// http://computing.open.ac.uk/Sites/EACLSouthAsia/Papers/p6-Ramanathan.pdf
    /// </para>
    /// </summary>
    public class HindiStemmer
    {
        public virtual int Stem(char[] buffer, int len)
        {
            // 5
            if ((len > 6) && (StemmerUtil.EndsWith(buffer, len, "ाएंगी") || StemmerUtil.EndsWith(buffer, len, "ाएंगे") || StemmerUtil.EndsWith(buffer, len, "ाऊंगी") || StemmerUtil.EndsWith(buffer, len, "ाऊंगा") || StemmerUtil.EndsWith(buffer, len, "ाइयाँ") || StemmerUtil.EndsWith(buffer, len, "ाइयों") || StemmerUtil.EndsWith(buffer, len, "ाइयां")))
            {
                return len - 5;
            }

            // 4
            if ((len > 5) && (StemmerUtil.EndsWith(buffer, len, "ाएगी") || StemmerUtil.EndsWith(buffer, len, "ाएगा") || StemmerUtil.EndsWith(buffer, len, "ाओगी") || StemmerUtil.EndsWith(buffer, len, "ाओगे") || StemmerUtil.EndsWith(buffer, len, "एंगी") || StemmerUtil.EndsWith(buffer, len, "ेंगी") || StemmerUtil.EndsWith(buffer, len, "एंगे") || StemmerUtil.EndsWith(buffer, len, "ेंगे") || StemmerUtil.EndsWith(buffer, len, "ूंगी") || StemmerUtil.EndsWith(buffer, len, "ूंगा") || StemmerUtil.EndsWith(buffer, len, "ातीं") || StemmerUtil.EndsWith(buffer, len, "नाओं") || StemmerUtil.EndsWith(buffer, len, "नाएं") || StemmerUtil.EndsWith(buffer, len, "ताओं") || StemmerUtil.EndsWith(buffer, len, "ताएं") || StemmerUtil.EndsWith(buffer, len, "ियाँ") || StemmerUtil.EndsWith(buffer, len, "ियों") || StemmerUtil.EndsWith(buffer, len, "ियां")))
            {
                return len - 4;
            }

            // 3
            if ((len > 4) && (StemmerUtil.EndsWith(buffer, len, "ाकर") || StemmerUtil.EndsWith(buffer, len, "ाइए") || StemmerUtil.EndsWith(buffer, len, "ाईं") || StemmerUtil.EndsWith(buffer, len, "ाया") || StemmerUtil.EndsWith(buffer, len, "ेगी") || StemmerUtil.EndsWith(buffer, len, "ेगा") || StemmerUtil.EndsWith(buffer, len, "ोगी") || StemmerUtil.EndsWith(buffer, len, "ोगे") || StemmerUtil.EndsWith(buffer, len, "ाने") || StemmerUtil.EndsWith(buffer, len, "ाना") || StemmerUtil.EndsWith(buffer, len, "ाते") || StemmerUtil.EndsWith(buffer, len, "ाती") || StemmerUtil.EndsWith(buffer, len, "ाता") || StemmerUtil.EndsWith(buffer, len, "तीं") || StemmerUtil.EndsWith(buffer, len, "ाओं") || StemmerUtil.EndsWith(buffer, len, "ाएं") || StemmerUtil.EndsWith(buffer, len, "ुओं") || StemmerUtil.EndsWith(buffer, len, "ुएं") || StemmerUtil.EndsWith(buffer, len, "ुआं")))
            {
                return len - 3;
            }

            // 2
            if ((len > 3) && (StemmerUtil.EndsWith(buffer, len, "कर") || StemmerUtil.EndsWith(buffer, len, "ाओ") || StemmerUtil.EndsWith(buffer, len, "िए") || StemmerUtil.EndsWith(buffer, len, "ाई") || StemmerUtil.EndsWith(buffer, len, "ाए") || StemmerUtil.EndsWith(buffer, len, "ने") || StemmerUtil.EndsWith(buffer, len, "नी") || StemmerUtil.EndsWith(buffer, len, "ना") || StemmerUtil.EndsWith(buffer, len, "ते") || StemmerUtil.EndsWith(buffer, len, "ीं") || StemmerUtil.EndsWith(buffer, len, "ती") || StemmerUtil.EndsWith(buffer, len, "ता") || StemmerUtil.EndsWith(buffer, len, "ाँ") || StemmerUtil.EndsWith(buffer, len, "ां") || StemmerUtil.EndsWith(buffer, len, "ों") || StemmerUtil.EndsWith(buffer, len, "ें")))
            {
                return len - 2;
            }

            // 1
            if ((len > 2) && (StemmerUtil.EndsWith(buffer, len, "ो") || StemmerUtil.EndsWith(buffer, len, "े") || StemmerUtil.EndsWith(buffer, len, "ू") || StemmerUtil.EndsWith(buffer, len, "ु") || StemmerUtil.EndsWith(buffer, len, "ी") || StemmerUtil.EndsWith(buffer, len, "ि") || StemmerUtil.EndsWith(buffer, len, "ा")))
            {
                return len - 1;
            }
            return len;
        }
    }
}