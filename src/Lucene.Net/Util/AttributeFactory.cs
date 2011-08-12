// -----------------------------------------------------------------------
// <copyright company="Apache" file="AttributeFactory.cs">
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
    using Support;


    /// <summary>
    /// A contract for factories that create instances of <see cref="AttributeBase" />
    /// that are map to interfaces that extend <see cref="IAttribute" />
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The factory is abstract so the behavior of how interfaces that derive from
    ///         <see cref="IAttribute"/> are mapped to concrete types that implement that 
    ///         interface. The factory also can change how the concrete types are created.
    ///     </para>
    ///     <para>
    ///         You could easily create your own factory that replaces the default 
    ///         behavior with Dependency Injection. 
    ///     </para>
    /// </remarks>
    /// <seealso cref="DefaultFactory"/>
    public abstract class AttributeFactory
    {
        /// <summary>
        /// Returns the default implementation for <see cref="AttributeFactory"/>, an instance of
        /// <see cref="DefaultAttributeFactory"/>. 
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The default factory uses a name convention to retrieve and create instances
        ///         of <see cref="AttributeBase" />. The convention is based on the name of 
        ///         the interface types that are subclasses of <see cref="IAttribute"/>.
        ///     </para>
        ///     <para>
        ///         The Java version appends "Impl" to each interface name. The .NET version
        ///         removes the &quot;I&quot; from the interface name by using <c>type.Name.Substring(0)</c>.
        ///     </para>
        /// </remarks>
        public static readonly AttributeFactory DefaultFactory = new DefaultAttributeFactory();

        /// <summary>
        /// Creates the <see cref="AttributeBase"/> instance based on the <typeparamref name="T"/> parameter
        /// that must derive from both <see cref="AttributeBase"/> and <see cref="IAttribute"/>.
        /// </summary>
        /// <typeparam name="T">The type of interface</typeparam>
        /// <returns>An instance of <see cref="AttributeBase"/>.</returns>
        public abstract AttributeBase CreateAttributeInstance<T>() where T : IAttribute;

        /// <summary>
        /// Creates the <see cref="AttributeBase"/> instance based on the interface <paramref name="type"/> 
        /// that derives from <see cref="IAttribute"/>.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        /// An instance of <see cref="AttributeBase"/>.
        /// </returns>
        public abstract AttributeBase CreateAttributeInstance(Type type);


        /// <summary>
        /// The default attribute factory implementation.
        /// </summary>
        private sealed class DefaultAttributeFactory : AttributeFactory
        {
            private static readonly WeakDictionary<Type, Type> map =
                new WeakDictionary<Type, Type>();


            public override AttributeBase CreateAttributeInstance<T>()
            {
                var implementationType = FetchClassForInterface(typeof(T));
                return CreateAttributeInstance(typeof(T));
            }

            public override AttributeBase CreateAttributeInstance(Type type)
            {
                var implementationType = FetchClassForInterface(type);
                return (AttributeBase)Activator.CreateInstance(implementationType);
            }


            private static Type FetchClassForInterface(Type type)
            {
                Type value;
                lock (map)
                {
                    if (!map.TryGetValue(type, out value))
                    {
                        var interfaceName = type.Name;
                        var typeName = interfaceName.Substring(1);

                        try
                        {
                            value = Type.GetType(string.Format("{0}.{1}", type.Namespace, typeName));
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException(
                                string.Format(
                                    "The implementation '{0}' could not be found for attribute interface '{1}'",
                                    typeName,
                                    type.FullName), 
                                    ex);
                        }

                        map.Add(type, value);
                    }
                }

                return value;
            }
        }
    }
}
