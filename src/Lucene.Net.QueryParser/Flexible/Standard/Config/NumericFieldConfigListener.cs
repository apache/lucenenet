using Lucene.Net.QueryParsers.Flexible.Core.Config;
using System;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Config
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
    /// This listener is used to listen to <see cref="FieldConfig"/> requests in
    /// <see cref="QueryConfigHandler"/> and add <see cref="ConfigurationKeys.NUMERIC_CONFIG"/>
    /// based on the <see cref="ConfigurationKeys.NUMERIC_CONFIG_MAP"/> set in the
    /// <see cref="QueryConfigHandler"/>.
    /// </summary>
    /// <seealso cref="NumericConfig"/>
    /// <seealso cref="QueryConfigHandler"/>
    /// <seealso cref="ConfigurationKeys.NUMERIC_CONFIG"/>
    /// <seealso cref="ConfigurationKeys.NUMERIC_CONFIG_MAP"/>
    public class NumericFieldConfigListener : IFieldConfigListener
    {
        private readonly QueryConfigHandler config;

        /// <summary>
        /// Constructs a <see cref="NumericFieldConfigListener"/> object using the given <see cref="QueryConfigHandler"/>.
        /// </summary>
        /// <param name="config">the <see cref="QueryConfigHandler"/> it will listen too</param>
        public NumericFieldConfigListener(QueryConfigHandler config)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config), "config cannot be null!"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
        }

        public virtual void BuildFieldConfig(FieldConfig fieldConfig)
        {
            IDictionary<string, NumericConfig> numericConfigMap = config
                .Get(ConfigurationKeys.NUMERIC_CONFIG_MAP);

            if (numericConfigMap != null)
            {
                if (numericConfigMap.TryGetValue(fieldConfig.Field, out NumericConfig numericConfig) && numericConfig != null)
                {
                    fieldConfig.Set(ConfigurationKeys.NUMERIC_CONFIG, numericConfig);
                }
            }
        }
    }
}
