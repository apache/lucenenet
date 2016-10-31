using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Util
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
    /// A utility for keeping backwards compatibility on previously abstract methods
    /// (or similar replacements).
    /// <p>Before the replacement method can be made abstract, the old method must kept deprecated.
    /// If somebody still overrides the deprecated method in a non-final class,
    /// you must keep track, of this and maybe delegate to the old method in the subclass.
    /// The cost of reflection is minimized by the following usage of this class:</p>
    /// <p>Define <strong>static final</strong> fields in the base class ({@code BaseClass}),
    /// where the old and new method are declared:</p>
    /// <pre class="prettyprint">
    ///  static final VirtualMethod&lt;BaseClass&gt; newMethod =
    ///   new VirtualMethod&lt;BaseClass&gt;(BaseClass.class, "newName", parameters...);
    ///  static final VirtualMethod&lt;BaseClass&gt; oldMethod =
    ///   new VirtualMethod&lt;BaseClass&gt;(BaseClass.class, "oldName", parameters...);
    /// </pre>
    /// <p>this enforces the singleton status of these objects, as the maintenance of the cache would be too costly else.
    /// If you try to create a second instance of for the same method/{@code baseClass} combination, an exception is thrown.</p>
    /// <p>To detect if e.g. the old method was overridden by a more far subclass on the inheritance path to the current
    /// instance's class, use a <strong>non-static</strong> field:</p>
    /// <pre class="prettyprint">
    ///  final boolean isDeprecatedMethodOverridden =
    ///   oldMethod.getImplementationDistance(this.getClass()) > newMethod.getImplementationDistance(this.getClass());
    ///
    ///  <em>// alternatively (more readable):</em>
    ///  final boolean isDeprecatedMethodOverridden =
    ///   VirtualMethod.compareImplementationDistance(this.getClass(), oldMethod, newMethod) > 0
    /// </pre>
    /// <p><seealso cref="GetImplementationDistance"/> returns the distance of the subclass that overrides this method.
    /// The one with the larger distance should be used preferable.
    /// this way also more complicated method rename scenarios can be handled
    /// (think of 2.9 {@code TokenStream} deprecations).</p>
    ///
    /// @lucene.internal
    /// </summary>
    // LUCENENET NOTE: Pointless to make this class generic, since the generic type is never used (the Type class in .NET
    // is not generic).
    public sealed class VirtualMethod
    {
        private static readonly ISet<MethodInfo> SingletonSet = new ConcurrentHashSet<MethodInfo>(new HashSet<MethodInfo>());

        private readonly Type BaseClass;
        private readonly string Method;
        private readonly Type[] Parameters;
        private readonly WeakIdentityMap<Type, int> Cache = WeakIdentityMap<Type, int>.NewConcurrentHashMap(false);

        /// <summary>
        /// Creates a new instance for the given {@code baseClass} and method declaration. </summary>
        /// <exception cref="InvalidOperationException"> if you create a second instance of the same
        ///  {@code baseClass} and method declaration combination. this enforces the singleton status. </exception>
        /// <exception cref="ArgumentException"> if {@code baseClass} does not declare the given method. </exception>
        public VirtualMethod(Type baseClass, string method, params Type[] parameters)
        {
            this.BaseClass = baseClass;
            this.Method = method;
            this.Parameters = parameters;
            try
            {
                MethodInfo mi = GetMethod(baseClass, method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, parameters);
                if (mi == null)
                {
                    throw new System.ArgumentException(baseClass.Name + " has no such method.");
                }
                else if (!SingletonSet.Add(mi))
                {
                    throw new System.NotSupportedException("VirtualMethod instances must be singletons and therefore " + "assigned to static final members in the same class, they use as baseClass ctor param.");
                }
            }
            catch (NotSupportedException nsme)
            {
                throw new System.ArgumentException(baseClass.Name + " has no such method: " + nsme.Message);
            }
        }

        /// <summary>
        /// Returns the distance from the {@code baseClass} in which this method is overridden/implemented
        /// in the inheritance path between {@code baseClass} and the given subclass {@code subclazz}. </summary>
        /// <returns> 0 iff not overridden, else the distance to the base class </returns>
        public int GetImplementationDistance(Type subclazz)
        {
            int distance = Cache.Get(subclazz);
            if (distance == default(int))
            {
                // we have the slight chance that another thread may do the same, but who cares?
                Cache.Put(subclazz, distance = Convert.ToInt32(ReflectImplementationDistance(subclazz)));
            }
            return (int)distance;
        }

        /// <summary>
        /// Returns, if this method is overridden/implemented in the inheritance path between
        /// {@code baseClass} and the given subclass {@code subclazz}.
        /// <p>You can use this method to detect if a method that should normally be final was overridden
        /// by the given instance's class. </summary>
        /// <returns> {@code false} iff not overridden </returns>
        public bool IsOverriddenAsOf(Type subclazz)
        {
            return GetImplementationDistance(subclazz) > 0;
        }

        private int ReflectImplementationDistance(Type subclazz)
        {
            if (!BaseClass.GetTypeInfo().IsAssignableFrom(subclazz))
            {
                throw new System.ArgumentException(subclazz.Name + " is not a subclass of " + BaseClass.Name);
            }
            bool overridden = false;
            int distance = 0;
            for (Type clazz = subclazz; clazz != BaseClass && clazz != null; clazz = clazz.GetTypeInfo().BaseType)
            {
                // lookup method, if success mark as overridden
                if (!overridden)
                {
                    MethodInfo mi =  GetMethod(clazz, Method,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                        Parameters);

                    if (mi != null)
                        overridden = true;
                }

                // increment distance if overridden
                if (overridden)
                {
                    distance++;
                }
            }
            return distance;
        }

        /// <summary>
        /// Utility method that compares the implementation/override distance of two methods. </summary>
        /// <returns> <ul>
        ///  <li>&gt; 1, iff {@code m1} is overridden/implemented in a subclass of the class overriding/declaring {@code m2}
        ///  <li>&lt; 1, iff {@code m2} is overridden in a subclass of the class overriding/declaring {@code m1}
        ///  <li>0, iff both methods are overridden in the same class (or are not overridden at all)
        /// </ul> </returns>
        public static int CompareImplementationDistance(Type clazz, VirtualMethod m1, VirtualMethod m2)
        {
            return Convert.ToInt32(m1.GetImplementationDistance(clazz)).CompareTo(m2.GetImplementationDistance(clazz));
        }

        private MethodInfo GetMethod(Type clazz, string methodName, BindingFlags bindingFlags, Type[] methodParameters)
        {
#if NETSTANDARD
            var methods = clazz.GetTypeInfo().GetMethods(bindingFlags).Where(x => {
                return x.Name.Equals(methodName)
                    && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(methodParameters);
                }).ToArray();

            if (methods.Length == 0)
            {
                return default(MethodInfo);
            }
            else if (methods.Length == 1)
            {
                return methods[0];
            }
            else
            {
                var formatted = string.Format("Found more than one match for type {0}, methodName {1}, bindingFlags {2}, parameters {3}", clazz, methodName, bindingFlags, methodParameters);
                throw new AmbiguousMatchException(formatted);
            }
#else
            return clazz.GetMethod(methodName, bindingFlags, null, methodParameters, null);
#endif
        }
    }
}