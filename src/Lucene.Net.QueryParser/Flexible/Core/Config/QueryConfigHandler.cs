using Lucene.Net.QueryParsers.Flexible.Core.Util;
using System.Collections.Generic;

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
    /// This class can be used to hold any query configuration and no field
    /// configuration. For field configuration, it creates an empty
    /// <see cref="FieldConfig"/> object and delegate it to field config listeners, 
    /// these are responsible for setting up all the field configuration.
    /// <para>
    /// <see cref="QueryConfigHandler"/> should be extended by classes that intends to
    /// provide configuration to <see cref="Processors.IQueryNodeProcessor"/> objects.
    /// </para>
    /// <para>
    /// The class that extends <see cref="QueryConfigHandler"/> should also provide
    /// <see cref="FieldConfig"/> objects for each collection field.
    /// </para>
    /// </summary>
    /// <seealso cref="FieldConfig"/>
    /// <seealso cref="IFieldConfigListener"/>
    /// <seealso cref="QueryConfigHandler"/>
    public abstract class QueryConfigHandler : AbstractQueryConfig
    {
        private readonly LinkedList<IFieldConfigListener> listeners = new LinkedList<IFieldConfigListener>();

        /// <summary>
        /// Returns an implementation of
        /// <see cref="FieldConfig"/> for a specific field name. If the implemented
        /// <see cref="QueryConfigHandler"/> does not know a specific field name, it may
        /// return <c>null</c>, indicating there is no configuration for that
        /// field.
        /// </summary>
        /// <param name="fieldName">the field name</param>
        /// <returns>A <see cref="FieldConfig"/>object containing the field name
        /// configuration or <c>null</c>, if the implemented
        /// <see cref="QueryConfigHandler"/> has no configuration for that field</returns>
        public virtual FieldConfig GetFieldConfig(string fieldName)
        {
            FieldConfig fieldConfig = new FieldConfig(StringUtils.ToString(fieldName));

            foreach (IFieldConfigListener listener in this.listeners)
            {
                listener.BuildFieldConfig(fieldConfig);
            }

            return fieldConfig;

        }

        /// <summary>
        /// Adds a listener. The added listeners are called in the order they are
        /// added.
        /// </summary>
        /// <param name="listener">the listener to be added</param>
        public virtual void AddFieldConfigListener(IFieldConfigListener listener)
        {
            this.listeners.AddLast(listener);
        }
    }
}
