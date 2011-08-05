// -----------------------------------------------------------------------
// <copyright file="AttributeSource.cs" company="Apache">
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
    using Support;


    /// <summary>
    ///     An <c>AttributeSource</c> contains a list of different <see cref="AttributeBase"/>s,
    ///     and methods to add and get them. There can only be a single instance
    ///     of an attribute in the same AttributeSource instance. This is ensured
    ///     by passing in the actual type of the Attribute  to 
    ///     the <see cref="AddAttribute(Type)" />, which then checks if an instance of
    ///     that type is already present. If yes, it returns the instance, otherwise
    ///     it creates a new instance and returns it.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///         <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/util/AttributeSource.java">
    ///             lucene/src/java/org/apache/lucene/util/AttributeSource.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Util/AttributeSource.cs">
    ///              src/Lucene.Net/Util/AttributeSource.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/AttributeSourceTest.cs">
    ///             test/Lucene.Net.Test/Util/AttributeSourceTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    public class AttributeSource
    {
        private static readonly object instanceLock = new object();

        private static readonly WeakDictionary<Type, LinkedList<WeakReference<Type>>> knownAttributeClasses =
            new WeakDictionary<Type, LinkedList<WeakReference<Type>>>();

        /// <summary>
        /// Gets the attribute interfaces.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     an instance of <see cref="LinkedList{T}"/> of <see cref="WeakReference{T}"/> of <see cref="Type"/>
        ///     that hold the known interfaces that inherit from <see cref="IAttribute"/>.
        /// </returns>
        public static LinkedList<WeakReference<Type>> GetAttributeInterfaces(Type type)
        {
            lock (instanceLock)
            {
                LinkedList<WeakReference<Type>> foundInterfaces = knownAttributeClasses.GetDefaultedValue(type);

                if (foundInterfaces == null)
                {
                    knownAttributeClasses[type] = foundInterfaces = new LinkedList<WeakReference<Type>>();

                    Type attributeInterface = typeof(IAttribute);
                    Type activeType = type;

                    do
                    {
                        foreach (Type currentInterface in activeType.GetInterfaces())
                        {
                            if (currentInterface != attributeInterface &&
                                attributeInterface.IsAssignableFrom(currentInterface))
                            {
                                foundInterfaces.AddLast(new WeakReference<Type>(currentInterface));
                            }
                        }
                          
                        activeType = activeType.BaseType;
                    } while (activeType != null);
                }

                return foundInterfaces;
            }
        }

        /// <summary>
        /// Adds an attribute.
        /// </summary>
        /// <param name="attributeType">The type of attribute that is to be added.</param>
        public void AddAttribute(Type attributeType)
        {
            throw new NotImplementedException();
        }
    }
}
