// commons-codec version compatibility level: 1.9
using System;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
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
    /// Types of rule.
    /// <para/>
    /// since 1.6
    /// </summary>
    public enum RuleType
    {
        /// <summary>
        /// Approximate rules, which will lead to the largest number of phonetic interpretations.
        /// </summary>
        APPROX,

        /// <summary>
        /// Exact rules, which will lead to a minimum number of phonetic interpretations.
        /// </summary>
        EXACT,

        /// <summary>
        /// For internal use only. Please use <see cref="APPROX"/> or <see cref="EXACT"/>.
        /// </summary>
        RULES
    }

    public static class RuleTypeExtensions
    {
        /// <summary>
        /// Gets the rule name.
        /// </summary>
        /// <param name="ruleType">The <see cref="RuleType"/>.</param>
        /// <returns>The rule name.</returns>
        public static string GetName(this RuleType ruleType)
        {
            switch (ruleType)
            {
                case RuleType.APPROX:
                    return "approx";
                case RuleType.EXACT:
                    return "exact";
                case RuleType.RULES:
                    return "rules";
            }

            throw new ArgumentException("Invalid ruleType");
        }
    }
}
