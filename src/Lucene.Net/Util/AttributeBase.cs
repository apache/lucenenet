// -----------------------------------------------------------------------
// <copyright file="AttributeBase.cs" company="Apache">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Support;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///         <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/util/AttributeImpl.java">
    ///             lucene/src/java/org/apache/lucene/util/AttributeImpl.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Util/AttributeBase.cs">
    ///              src/Lucene.Net/Util/AttributeBase.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/AttributeBaseTest.cs">
    ///             test/Lucene.Net.Test/Util/AttributeBaseTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    public abstract class AttributeBase : IAttribute, ICloneable
    {
        /// <summary>
        /// Clears the instance.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="attributeBase">The attribute base.</param>
        public abstract void CopyTo(AttributeBase attributeBase);


        /// <summary>
        /// Reflects as string.
        /// </summary>
        /// <param name="prependAttributeType">if set to <c>true</c> [prepend attribute type].</param>
        /// <returns>a <see cref="string"/></returns>
        public string ReflectAsString(bool prependAttributeType = false)
        {
            var buffer = new StringBuilder();

            this.Reflect((type, name, value) => 
            {
                if (buffer.Length > 0)
                    buffer.Append(",");

                if (prependAttributeType)
                    buffer.Append(type.Name).Append("#");

                buffer
                    .Append(name)
                    .Append("=")
                    .Append(value == null ? "null" : value.ToString());
            });

            return buffer.ToString();
        }



        /// <summary>
        /// Reflects the specified reflect action.
        /// </summary>
        /// <param name="reflectAction">The reflect action.</param>
        public virtual void Reflect(Action<Type, string, object> reflectAction)
        {
            Type type = this.GetType();
            LinkedList<WeakReference<Type>> foundInterfaces = AttributeSource.GetAttributeInterfaces(type);

            if (foundInterfaces.Count == 0)
                return;

            if (foundInterfaces.Count > 1)
                throw new NotSupportedException(
                    string.Format(
                        "{0} implements more than one attribute interface. " +
                        "The default ReflectWith(IAttributeReflector) implementation can not handle this.", 
                        type.FullName));

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
             
            foreach (var prop in properties)
            {
                reflectAction(type, prop.Name, prop.GetValue(this, null));
            }

            foreach (var field in fields)
            {
                reflectAction(type, field.Name, field.GetValue(this));
            }
        }



        /// <summary>
        /// Creates a clone of the object, generally shallow.
        /// </summary>
        /// <returns>an the clone of the current instance.</returns>
        public virtual object Clone()
        {
            var obj = this.MemberwiseClone();
            return obj;
        }
    }
}
