using Lucene.Net.QueryParsers.Flexible.Core.Config;

namespace Lucene.Net.QueryParsers.Flexible.Spans
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
    /// This query config handler only adds the <see cref="IUniqueFieldAttribute"/> to it.
    /// <para/>
    /// It does not return any configuration for a field in specific.
    /// </summary>
    public class SpansQueryConfigHandler : QueryConfigHandler
    {
        public readonly static ConfigurationKey<string> UNIQUE_FIELD = ConfigurationKey.NewInstance<string>();

        public SpansQueryConfigHandler()
        {
            // empty constructor
        }


        public override FieldConfig GetFieldConfig(string fieldName)
        {

            // there is no field configuration, always return null
            return null;

        }
    }
}
