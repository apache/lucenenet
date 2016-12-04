using System;

namespace Lucene.Net.QueryParsers.Flexible.Core.Config
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
    /// This class represents a field configuration.
    /// </summary>
    public class FieldConfig : AbstractQueryConfig
    {
        private string fieldName;

        /**
         * Constructs a {@link FieldConfig}
         * 
         * @param fieldName the field name, it cannot be null
         * @throws IllegalArgumentException if the field name is null
         */
        public FieldConfig(string fieldName)
        {
            if (fieldName == null)
            {
                throw new ArgumentException("field name should not be null!");
            }

            this.fieldName = fieldName;
        }

        /**
         * Returns the field name this configuration represents.
         * 
         * @return the field name
         */
        public virtual string Field
        {
            get { return this.fieldName; }
        }

        public override string ToString()
        {
            return "<fieldconfig name=\"" + this.fieldName + "\" configurations=\""
                + base.ToString() + "\"/>";
        }
    }
}
