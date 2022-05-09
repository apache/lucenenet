using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
#if !FEATURE_TYPE_GETMETHOD__BINDINGFLAGS_PARAMS
using System.Linq;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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
    /// <para>Before the replacement method can be made abstract, the old method must kept deprecated.
    /// If somebody still overrides the deprecated method in a non-sealed class,
    /// you must keep track, of this and maybe delegate to the old method in the subclass.
    /// The cost of reflection is minimized by the following usage of this class:</para>
    /// <para>Define <strong>static readonly</strong> fields in the base class (<c>BaseClass</c>),
    /// where the old and new method are declared:</para>
    /// <code>
    /// internal static readonly VirtualMethod newMethod =
    ///     new VirtualMethod(typeof(BaseClass), "newName", parameters...);
    /// internal static readonly VirtualMethod oldMethod =
    ///     new VirtualMethod(typeof(BaseClass), "oldName", parameters...);
    /// </code>
    /// <para>this enforces the singleton status of these objects, as the maintenance of the cache would be too costly else.
    /// If you try to create a second instance of for the same method/<c>baseClass</c> combination, an exception is thrown.</para>
    /// <para>To detect if e.g. the old method was overridden by a more far subclass on the inheritance path to the current
    /// instance's class, use a <strong>non-static</strong> field:</para>
    /// <code>
    ///  bool isDeprecatedMethodOverridden =
    ///      oldMethod.GetImplementationDistance(this.GetType()) > newMethod.GetImplementationDistance(this.GetType());
    ///
    ///  <em>// alternatively (more readable):</em>
    ///  bool isDeprecatedMethodOverridden =
    ///      VirtualMethod.CompareImplementationDistance(this.GetType(), oldMethod, newMethod) > 0
    /// </code>
    /// <para><seealso cref="GetImplementationDistance"/> returns the distance of the subclass that overrides this method.
    /// The one with the larger distance should be used preferable.
    /// this way also more complicated method rename scenarios can be handled
    /// (think of 2.9 <see cref="Analysis.TokenStream"/> deprecations).</para>
    ///
    /// @lucene.internal
    /// </summary>
    // LUCENENET NOTE: Pointless to make this class generic, since the generic type is never used (the Type class in .NET
    // is not generic).
    public sealed class VirtualMethod
    {
        private static readonly ISet<MethodInfo> singletonSet = new ConcurrentHashSet<MethodInfo>();

        private readonly Type baseClass;
        private readonly string method;
        private readonly Type[] parameters;
        // LUCENENET: Replaced IdentityHashMap with ConditionalWeakTable. A Type IS an identity, so there is
        // no need for the extra IdentityWeakReference.
        private readonly ConditionalWeakTable<Type, Int32Ref> cache = new ConditionalWeakTable<Type, Int32Ref>();

        // LUCENENET specific wrapper needed because ConditionalWeakTable requires a reference type.
        private class Int32Ref : IEquatable<Int32Ref>
        {
            private readonly int value;

            public Int32Ref(int value)
            {
                this.value = value;
            }

            public bool Equals(Int32Ref other)
            {
                if (other is null)
                    return false;
                return value.Equals(other);
            }

            public override bool Equals(object obj)
            {
                if (obj is Int32Ref other)
                    return Equals(other);
                if (obj is int otherInt)
                    return value.Equals(otherInt);
                return false;
            }

            public override int GetHashCode()
            {
                return value.GetHashCode();
            }

            public static implicit operator int(Int32Ref value) => value.value;

            public static implicit operator Int32Ref(int value) => new Int32Ref(value);
        }


        /// <summary>
        /// Creates a new instance for the given <paramref name="baseClass"/> and method declaration. </summary>
        /// <exception cref="InvalidOperationException"> if you create a second instance of the same
        /// <paramref name="baseClass"/> and method declaration combination. This enforces the singleton status. </exception>
        /// <exception cref="ArgumentException"> If <paramref name="baseClass"/> does not declare the given method. </exception>
        public VirtualMethod(Type baseClass, string method, params Type[] parameters)
        {
            this.baseClass = baseClass;
            this.method = method;
            this.parameters = parameters;
            try
            {
                MethodInfo mi = GetMethod(baseClass, method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, parameters);
                if (mi is null)
                {
                    throw new ArgumentException(baseClass.Name + " has no such method.");
                }
                else if (!singletonSet.Add(mi))
                {
                    throw UnsupportedOperationException.Create("VirtualMethod instances must be singletons and therefore " + "assigned to static final members in the same class, they use as baseClass ctor param.");
                }
            }
            catch (Exception nsme) when (nsme.IsNoSuchMethodException())
            {
                throw new ArgumentException(baseClass.Name + " has no such method: " + nsme.Message, nsme);
            }
        }

        /// <summary>
        /// Returns the distance from the <c>baseClass</c> in which this method is overridden/implemented
        /// in the inheritance path between <c>baseClass</c> and the given subclass <paramref name="subclazz"/>. </summary>
        /// <returns> 0 if and only if not overridden, else the distance to the base class. </returns>
        public int GetImplementationDistance(Type subclazz)
        {
            // LUCENENET: Replaced WeakIdentityMap with ConditionalWeakTable - This operation is simplified over Lucene.
            return cache.GetValue(subclazz, (key) => ReflectImplementationDistance(key));
        }

        /// <summary>
        /// Returns, if this method is overridden/implemented in the inheritance path between
        /// <c>baseClass</c> and the given subclass <paramref name="subclazz"/>.
        /// <para/>You can use this method to detect if a method that should normally be final was overridden
        /// by the given instance's class. </summary>
        /// <returns> <c>false</c> if and only if not overridden. </returns>
        public bool IsOverriddenAsOf(Type subclazz)
        {
            return GetImplementationDistance(subclazz) > 0;
        }

        private int ReflectImplementationDistance(Type subclazz)
        {
            if (!baseClass.IsAssignableFrom(subclazz))
            {
                throw new ArgumentException(subclazz.Name + " is not a subclass of " + baseClass.Name);
            }
            bool overridden = false;
            int distance = 0;
            for (Type clazz = subclazz; clazz != baseClass && clazz != null; clazz = clazz.BaseType)
            {
                // lookup method, if success mark as overridden
                if (!overridden)
                {
                    try
                    {
                        MethodInfo mi = GetMethod(clazz, method,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                            parameters);

                        // LUCENENET specific - .NET returns null when it cannot find a method, it doesn't throw an exception
                        if (mi != null)
                            overridden = true;
                    }
                    // LUCENENET specific - there is a minor chance this will happen in .NET. This is
                    // just to mimic the fact they were swallowing in Java when the method isn't found.
                    catch (AmbiguousMatchException)
                    {
                    }
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
        /// <returns> 
        /// <list type="bullet">
        ///     <item><description>&gt; 1, iff <paramref name="m1"/> is overridden/implemented in a subclass of the class overriding/declaring <paramref name="m2"/></description></item>
        ///     <item><description>&lt; 1, iff <paramref name="m2"/> is overridden in a subclass of the class overriding/declaring <paramref name="m1"/></description></item>
        ///     <item><description>0, iff both methods are overridden in the same class (or are not overridden at all)</description></item>
        /// </list>
        /// </returns>
        public static int CompareImplementationDistance(Type clazz, VirtualMethod m1, VirtualMethod m2)
        {
            return m1.GetImplementationDistance(clazz).CompareTo(m2.GetImplementationDistance(clazz));
        }

        private static MethodInfo GetMethod(Type clazz, string methodName, BindingFlags bindingFlags, Type[] methodParameters) // LUCENENET: CA1822: Mark members as static
        {
#if FEATURE_TYPE_GETMETHOD__BINDINGFLAGS_PARAMS
            return clazz.GetMethod(methodName, bindingFlags, null, methodParameters, null);
#else
            var methods = clazz.GetTypeInfo().GetMethods(bindingFlags).Where(x => {
                return x.Name.Equals(methodName, StringComparison.Ordinal)
                    && x.GetParameters().Select(y => y.ParameterType).SequenceEqual(methodParameters);
                }).ToArray();

            if (methods.Length == 0)
            {
                return default;
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
#endif
        }
    }
}