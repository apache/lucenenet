using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Support.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using FlagsAttribute = Lucene.Net.Analysis.TokenAttributes.FlagsAttribute;
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
    /// An <see cref="AttributeSource"/> contains a list of different <see cref="Attribute"/>s,
    /// and methods to add and get them. There can only be a single instance
    /// of an attribute in the same <see cref="AttributeSource"/> instance. This is ensured
    /// by passing in the actual type of the <see cref="IAttribute"/> to
    /// the <see cref="AddAttribute{T}"/>, which then checks if an instance of
    /// that type is already present. If yes, it returns the instance, otherwise
    /// it creates a new instance and returns it.
    /// </summary>
    public class AttributeSource
    {
        /// <summary>
        /// An <see cref="AttributeFactory"/> creates instances of <see cref="Attribute"/>s.
        /// </summary>
        public abstract class AttributeFactory // LUCENENET TODO: API - de-nest
        {
            /// <summary>
            /// returns an <see cref="Attribute"/> for the supplied <see cref="IAttribute"/> interface.
            /// </summary>
            public abstract Attribute CreateAttributeInstance<T>() where T : IAttribute;

            /// <summary>
            /// This is the default factory that creates <see cref="Attribute"/>s using the
            /// <see cref="Type"/> of the supplied <see cref="IAttribute"/> interface by removing the <code>I</code> from the prefix.
            /// </summary>
            public static readonly AttributeFactory DEFAULT_ATTRIBUTE_FACTORY = new DefaultAttributeFactory();

            private sealed class DefaultAttributeFactory : AttributeFactory
            {
                // LUCENENET: Using ConditionalWeakTable instead of WeakIdentityMap. A Type IS an
                // identity for a class, so there is no need for an identity wrapper for it.
                private static readonly ConditionalWeakTable<Type, WeakReference<Type>> attClassImplMap =
                    new ConditionalWeakTable<Type, WeakReference<Type>>();
                private static readonly object attClassImplMapLock = new object();

                internal DefaultAttributeFactory()
                {
                }

                public override Attribute CreateAttributeInstance<S>()
                {
                    try
                    {
                        Type attributeType = GetClassForInterface<S>();

                        // LUCENENET: Optimize for creating instances of the most common attributes
                        // directly rather than using Activator.CreateInstance()
                        return CreateInstance(attributeType) ?? (Attribute)Activator.CreateInstance(attributeType);
                    }
                    catch (Exception e) when (e.IsInstantiationException() || e.IsIllegalAccessException())
                    {
                        throw new ArgumentException("Could not instantiate implementing class for " + typeof(S).FullName, e);
                    }
                }

                // LUCENENET: optimize known creation of built-in types
                private static Attribute CreateInstance(Type attributeType) // LUCENENET: CA1822: Mark members as static
                {
                    if (ReferenceEquals(typeof(CharTermAttribute), attributeType))
                        return new CharTermAttribute();
                    if (ReferenceEquals(typeof(FlagsAttribute), attributeType))
                        return new FlagsAttribute();
                    if (ReferenceEquals(typeof(OffsetAttribute), attributeType))
                        return new OffsetAttribute();
                    if (ReferenceEquals(typeof(PayloadAttribute), attributeType))
                        return new PayloadAttribute();
                    if (ReferenceEquals(typeof(PositionIncrementAttribute), attributeType))
                        return new PositionIncrementAttribute();
                    if (ReferenceEquals(typeof(PositionLengthAttribute), attributeType))
                        return new PositionLengthAttribute();
                    if (ReferenceEquals(typeof(TypeAttribute), attributeType))
                        return new TypeAttribute();

                    return null;
                }

                internal static Type GetClassForInterface<T>() where T : IAttribute
                {
                    var attClass = typeof(T);
                    Type clazz;

                    // LUCENENET: If the weakreference is dead, we need to explicitly update its key.
                    // We synchronize on attClassImplMapLock to make the operation atomic.
                    UninterruptableMonitor.Enter(attClassImplMapLock);
                    try
                    {
                        if (!attClassImplMap.TryGetValue(attClass, out var @ref) || !@ref.TryGetTarget(out clazz))
                        {
#if FEATURE_CONDITIONALWEAKTABLE_ADDORUPDATE
                            attClassImplMap.AddOrUpdate(attClass, CreateAttributeWeakReference(attClass, out clazz));
#else
                            attClassImplMap.Remove(attClass);
                            attClassImplMap.Add(attClass, CreateAttributeWeakReference(attClass, out clazz));
#endif
                        }
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(attClassImplMapLock);
                    }

                    return clazz;
                }

                // LUCENENET specific - factored this out so we can reuse
                private static WeakReference<Type> CreateAttributeWeakReference(Type attributeInterfaceType, out Type clazz)
                {
                    try
                    {
                        string name = ConvertAttributeInterfaceToClassName(attributeInterfaceType);
                        return new WeakReference<Type>(clazz = attributeInterfaceType.Assembly.GetType(name, true));
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Could not find implementing class for " + attributeInterfaceType.Name, e);
                    }
                }

                private static string ConvertAttributeInterfaceToClassName(Type attributeInterfaceType)
                {
                    int lastPlus = attributeInterfaceType.FullName.LastIndexOf('+');
                    if (lastPlus == -1)
                    {
#if FEATURE_STRING_CONCAT_READONLYSPAN
                        return string.Concat(
                            attributeInterfaceType.Namespace,
                            ".",
                            attributeInterfaceType.Name.AsSpan(1));
#else
                        return string.Concat(
                            attributeInterfaceType.Namespace,
                            ".",
                            attributeInterfaceType.Name.Substring(1));
#endif
                    }
                    else
                    {
#if FEATURE_STRING_CONCAT_READONLYSPAN
                        return string.Concat(
                            attributeInterfaceType.FullName.AsSpan(0, lastPlus + 1),
                            attributeInterfaceType.Name.AsSpan(1));
#else
                        return string.Concat(
                            attributeInterfaceType.FullName.Substring(0, lastPlus + 1),
                            attributeInterfaceType.Name.Substring(1));
#endif
                    }
                }
            }
        }

        /// <summary>
        /// This class holds the state of an <see cref="AttributeSource"/>. </summary>
        /// <seealso cref="CaptureState()"/>
        /// <seealso cref="RestoreState(State)"/>
        public sealed class State // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
        {
            internal Attribute attribute;
            internal State next;

            public object Clone()
            {
                State clone = new State();
                clone.attribute = (Attribute)attribute.Clone();

                if (next != null)
                {
                    clone.next = (State)next.Clone();
                }

                return clone;
            }
        }

        // These two maps must always be in sync!!!
        // So they are private, final and read-only from the outside (read-only iterators)
        private readonly IDictionary<Type, Util.Attribute> attributes;

        private readonly IDictionary<Type, Util.Attribute> attributeImpls;
        private readonly State[] currentState;

        private readonly AttributeFactory factory;

        /// <summary>
        /// An <see cref="AttributeSource"/> using the default attribute factory <see cref="AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY"/>.
        /// </summary>
        public AttributeSource()
            : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY)
        {
        }

        /// <summary>
        /// An <see cref="AttributeSource"/> that uses the same attributes as the supplied one.
        /// </summary>
        public AttributeSource(AttributeSource input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input), "input AttributeSource must not be null"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            }
            this.attributes = input.attributes;
            this.attributeImpls = input.attributeImpls;
            this.currentState = input.currentState;
            this.factory = input.factory;
        }

        /// <summary>
        /// An <see cref="AttributeSource"/> using the supplied <see cref="AttributeFactory"/> for creating new <see cref="IAttribute"/> instances.
        /// </summary>
        public AttributeSource(AttributeFactory factory)
        {
            this.attributes = new JCG.LinkedDictionary<Type, Util.Attribute>();
            this.attributeImpls = new JCG.LinkedDictionary<Type, Util.Attribute>();
            this.currentState = new State[1];
            this.factory = factory;
        }

        /// <summary>
        /// Returns the used <see cref="AttributeFactory"/>.
        /// </summary>
        public AttributeFactory GetAttributeFactory()
        {
            return this.factory;
        }

        /// <summary>
        /// Returns a new iterator that iterates the attribute classes
        /// in the same order they were added in.
        /// </summary>
        public IEnumerator<Type> GetAttributeClassesEnumerator()
        {
            return attributes.Keys.GetEnumerator();
        }

        /// <summary>
        /// Returns a new iterator that iterates all unique <see cref="IAttribute"/> implementations.
        /// This iterator may contain less entries than <see cref="GetAttributeClassesEnumerator()"/>,
        /// if one instance implements more than one <see cref="IAttribute"/> interface.
        /// </summary>
        public IEnumerator<Attribute> GetAttributeImplsEnumerator()
        {
            State initState = GetCurrentState();
            if (initState != null)
            {
                return new EnumeratorAnonymousClass(initState);
            }
            else
            {
                return Collections.EmptySet<Attribute>().GetEnumerator();
            }
        }

        private sealed class EnumeratorAnonymousClass : IEnumerator<Attribute>
        {
            public EnumeratorAnonymousClass(AttributeSource.State initState)
            {
                state = initState;
            }

            private Attribute current;
            private State state;

            //public virtual void Remove() // LUCENENET specific - not used
            //{
            //    throw UnsupportedOperationException.Create();
            //}

            public void Dispose()
            {
                // LUCENENET: Intentionally blank
            }

            public bool MoveNext()
            {
                if (state is null)
                {
                    return false;
                }

                Attribute att = state.attribute;
                state = state.next;
                current = att;
                return true;
            }

            public void Reset()
            {
                throw UnsupportedOperationException.Create();
            }

            public Attribute Current => current;

            object IEnumerator.Current => current;
        }

        /// <summary>
        /// A cache that stores all interfaces for known implementation classes for performance (slow reflection) </summary>
        // LUCENENET: Using ConditionalWeakTable instead of WeakIdentityMap. A Type IS an
        // identity for a class, so there is no need for an identity wrapper for it.
        private static readonly ConditionalWeakTable<Type, LinkedList<WeakReference<Type>>> knownImplClasses =
            new ConditionalWeakTable<Type, LinkedList<WeakReference<Type>>>();

        internal static LinkedList<WeakReference<Type>> GetAttributeInterfaces(Type clazz)
        {
            return knownImplClasses.GetValue(clazz, (key) =>
            {
                // we have the slight chance that another thread may do the same, but who cares?
                LinkedList<WeakReference<Type>> foundInterfaces = new LinkedList<WeakReference<Type>>();
                // find all interfaces that this attribute instance implements
                // and that extend the Attribute interface
                Type actClazz = clazz;
                do
                {
                    foreach (Type curInterface in actClazz.GetInterfaces())
                    {
                        if (curInterface != typeof(IAttribute) && typeof(IAttribute).IsAssignableFrom(curInterface))
                        {
                            foundInterfaces.AddLast(new WeakReference<Type>(curInterface));
                        }
                    }
                    actClazz = actClazz.BaseType;
                } while (actClazz != null);

                return foundInterfaces;
            });
        }

        /// <summary>
        /// <b>Expert:</b> Adds a custom <see cref="Attribute"/> instance with one or more <see cref="IAttribute"/> interfaces.
        /// <para><font color="red"><b>Please note:</b> It is not guaranteed, that <paramref name="att"/> is added to
        /// the <see cref="AttributeSource"/>, because the provided attributes may already exist.
        /// You should always retrieve the wanted attributes using <see cref="GetAttribute{T}"/> after adding
        /// with this method and cast to your <see cref="Type"/>.
        /// The recommended way to use custom implementations is using an <see cref="AttributeFactory"/>.
        /// </font></para>
        /// </summary>
        public void AddAttributeImpl(Attribute att)
        {
            Type clazz = att.GetType();
            if (attributeImpls.ContainsKey(clazz))
            {
                return;
            }

            LinkedList<WeakReference<Type>> foundInterfaces = GetAttributeInterfaces(clazz);

            // add all interfaces of this Attribute to the maps
            foreach (var curInterfaceRef in foundInterfaces)
            {
                curInterfaceRef.TryGetTarget(out Type curInterface);
                if (Debugging.AssertsEnabled) Debugging.Assert(curInterface != null, "We have a strong reference on the class holding the interfaces, so they should never get evicted");
                // Attribute is a superclass of this interface
                if (!attributes.ContainsKey(curInterface))
                {
                    // invalidate state to force recomputation in captureState()
                    this.currentState[0] = null;
                    attributes.Add(curInterface, att);
                    if (!attributeImpls.ContainsKey(clazz))
                    {
                        attributeImpls.Add(clazz, att);
                    }
                }
            }
        }

        /// <summary>
        /// The caller must pass in an interface type that extends <see cref="IAttribute"/>.
        /// This method first checks if an instance of the corresponding class is 
        /// already in this <see cref="AttributeSource"/> and returns it. Otherwise a
        /// new instance is created, added to this <see cref="AttributeSource"/> and returned. 
        /// </summary>
        public T AddAttribute<T>()
            where T : IAttribute
        {
            var attClass = typeof(T);
            // LUCENENET: Eliminated exception and used TryGetValue
            if (!attributes.TryGetValue(attClass, out var result))
            {
                if (!(attClass.IsInterface && typeof(IAttribute).IsAssignableFrom(attClass)))
                {
                    throw new ArgumentException("AddAttribute() only accepts an interface that extends IAttribute, but " + attClass.FullName + " does not fulfil this contract.");
                }

                result = this.factory.CreateAttributeInstance<T>();
                AddAttributeImpl(result);
            }

            return (T)(IAttribute)result;
        }

        /// <summary>
        /// Returns <c>true</c>, if this <see cref="AttributeSource"/> has any attributes </summary>
        public bool HasAttributes => this.attributes.Count > 0;

        /// <summary>
        /// The caller must pass in an interface type that extends <see cref="IAttribute"/>.
        /// Returns <c>true</c>, if this <see cref="AttributeSource"/> contains the corrsponding <see cref="Attribute"/>.
        /// </summary>
        public bool HasAttribute<T>() where T : IAttribute
        {
            var attClass = typeof(T);
            return this.attributes.ContainsKey(attClass);
        }

        /// <summary>
        /// The caller must pass in an interface type that extends <see cref="IAttribute"/>.
        /// Returns the instance of the corresponding <see cref="Attribute"/> contained in this <see cref="AttributeSource"/>
        /// </summary>
        /// <exception cref="ArgumentException"> if this <see cref="AttributeSource"/> does not contain the
        ///         <see cref="Attribute"/>. It is recommended to always use <see cref="AddAttribute{T}()"/> even in consumers
        ///         of <see cref="Analysis.TokenStream"/>s, because you cannot know if a specific <see cref="Analysis.TokenStream"/> really uses
        ///         a specific <see cref="Attribute"/>. <see cref="AddAttribute{T}()"/> will automatically make the attribute
        ///         available. If you want to only use the attribute, if it is available (to optimize
        ///         consuming), use <see cref="HasAttribute{T}()"/>. </exception>
        public virtual T GetAttribute<T>() where T : IAttribute
        {
            var attClass = typeof(T);
            if (!attributes.TryGetValue(attClass, out var result))
            {
                throw new ArgumentException($"this AttributeSource does not have the attribute '{attClass.Name}'.");
            }
            return (T)(IAttribute)result;
        }

        private State GetCurrentState()
        {
            State s = currentState[0];
            if (s != null || !HasAttributes)
            {
                return s;
            }
            var c = s = currentState[0] = new State();
            using var it = attributeImpls.Values.GetEnumerator();
            it.MoveNext();
            c.attribute = it.Current;
            while (it.MoveNext())
            {
                c.next = new State();
                c = c.next;
                c.attribute = it.Current;
            }
            return s;
        }

        /// <summary>
        /// Resets all <see cref="Attribute"/>s in this <see cref="AttributeSource"/> by calling
        /// <see cref="Attribute.Clear()"/> on each <see cref="IAttribute"/> implementation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearAttributes()
        {
            for (State state = GetCurrentState(); state != null; state = state.next)
            {
                state.attribute.Clear();
            }
        }

        /// <summary>
        /// Captures the state of all <see cref="Attribute"/>s. The return value can be passed to
        /// <see cref="RestoreState(State)"/> to restore the state of this or another <see cref="AttributeSource"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual State CaptureState()
        {
            State state = this.GetCurrentState();
            return (state is null) ? null : (State)state.Clone();
        }

        /// <summary>
        /// Restores this state by copying the values of all attribute implementations
        /// that this state contains into the attributes implementations of the targetStream.
        /// The targetStream must contain a corresponding instance for each argument
        /// contained in this state (e.g. it is not possible to restore the state of
        /// an <see cref="AttributeSource"/> containing a <see cref="Analysis.TokenAttributes.ICharTermAttribute"/> into a <see cref="AttributeSource"/> using
        /// a <see cref="Analysis.Token"/> instance as implementation).
        /// <para/>
        /// Note that this method does not affect attributes of the targetStream
        /// that are not contained in this state. In other words, if for example
        /// the targetStream contains an <see cref="Analysis.TokenAttributes.IOffsetAttribute"/>, but this state doesn't, then
        /// the value of the <see cref="Analysis.TokenAttributes.IOffsetAttribute"/> remains unchanged. It might be desirable to
        /// reset its value to the default, in which case the caller should first
        /// call <see cref="AttributeSource.ClearAttributes()"/> (<c>TokenStream.ClearAttributes()</c> on the targetStream.
        /// </summary>
        public void RestoreState(State state)
        {
            if (state is null)
            {
                return;
            }

            do
            {
                if (!attributeImpls.ContainsKey(state.attribute.GetType()))
                {
                    throw new ArgumentException("State contains Attribute of type " + state.attribute.GetType().Name + " that is not in in this AttributeSource");
                }
                state.attribute.CopyTo(attributeImpls[state.attribute.GetType()]);
                state = state.next;
            } while (state != null);
        }

        public override int GetHashCode()
        {
            int code = 0;
            for (State state = GetCurrentState(); state != null; state = state.next)
            {
                code = code * 31 + state.attribute.GetHashCode();
            }
            return code;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is AttributeSource other)
            {
                if (HasAttributes)
                {
                    if (!other.HasAttributes)
                    {
                        return false;
                    }

                    if (this.attributeImpls.Count != other.attributeImpls.Count)
                    {
                        return false;
                    }

                    // it is only equal if all attribute impls are the same in the same order
                    State thisState = this.GetCurrentState();
                    State otherState = other.GetCurrentState();
                    while (thisState != null && otherState != null)
                    {
                        if (otherState.attribute.GetType() != thisState.attribute.GetType() || !otherState.attribute.Equals(thisState.attribute))
                        {
                            return false;
                        }
                        thisState = thisState.next;
                        otherState = otherState.next;
                    }
                    return true;
                }
                else
                {
                    return !other.HasAttributes;
                }
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// This method returns the current attribute values as a string in the following format
        /// by calling the <see cref="ReflectWith(IAttributeReflector)"/> method:
        ///
        /// <list type="bullet">
        ///     <item><term>if <paramref name="prependAttClass"/>=true:</term> <description> <c>"AttributeClass.Key=value,AttributeClass.Key=value"</c> </description></item>
        ///     <item><term>if <paramref name="prependAttClass"/>=false:</term> <description> <c>"key=value,key=value"</c> </description></item>
        /// </list>
        /// </summary>
        /// <seealso cref="ReflectWith(IAttributeReflector)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ReflectAsString(bool prependAttClass)
        {
            StringBuilder buffer = new StringBuilder();
            ReflectWith(new AttributeReflectorAnonymousClass(prependAttClass, buffer));
            return buffer.ToString();
        }

        private sealed class AttributeReflectorAnonymousClass : IAttributeReflector
        {
            private readonly bool prependAttClass;
            private readonly StringBuilder buffer;

            public AttributeReflectorAnonymousClass(bool prependAttClass, StringBuilder buffer)
            {
                this.prependAttClass = prependAttClass;
                this.buffer = buffer;
            }

            public void Reflect<T>(string key, object value)
                where T : IAttribute
            {
                Reflect(typeof(T), key, value);
            }

            public void Reflect(Type attClass, string key, object value)
            {
                if (buffer.Length > 0)
                {
                    buffer.Append(',');
                }
                if (prependAttClass)
                {
                    buffer.Append(attClass.Name).Append('#');
                }
                buffer.Append(key).Append('=');
                if (value is null)
                    buffer.Append("null");
                else
                    buffer.Append(value);
            }
        }

        /// <summary>
        /// This method is for introspection of attributes, it should simply
        /// add the key/values this <see cref="AttributeSource"/> holds to the given <see cref="IAttributeReflector"/>.
        ///
        /// <para>This method iterates over all <see cref="IAttribute"/> implementations and calls the
        /// corresponding <see cref="Attribute.ReflectWith(IAttributeReflector)"/> method.</para>
        /// </summary>
        /// <seealso cref="Attribute.ReflectWith(IAttributeReflector)"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReflectWith(IAttributeReflector reflector)
        {
            for (State state = GetCurrentState(); state != null; state = state.next)
            {
                state.attribute.ReflectWith(reflector);
            }
        }

        /// <summary>
        /// Performs a clone of all <see cref="Attribute"/> instances returned in a new
        /// <see cref="AttributeSource"/> instance. This method can be used to e.g. create another <see cref="Analysis.TokenStream"/>
        /// with exactly the same attributes (using <see cref="AttributeSource(AttributeSource)"/>).
        /// You can also use it as a (non-performant) replacement for <see cref="CaptureState()"/>, if you need to look
        /// into / modify the captured state.
        /// </summary>
        public AttributeSource CloneAttributes()
        {
            AttributeSource clone = new AttributeSource(this.factory);

            if (HasAttributes)
            {
                // first clone the impls
                for (State state = GetCurrentState(); state != null; state = state.next)
                {
                    //clone.AttributeImpls[state.attribute.GetType()] = state.attribute.Clone();
                    var impl = (Attribute)state.attribute.Clone();

                    if (!clone.attributeImpls.ContainsKey(impl.GetType()))
                    {
                        clone.attributeImpls.Add(impl.GetType(), impl);
                    }
                }

                // now the interfaces
                foreach (var entry in this.attributes)
                {
                    clone.attributes.Add(entry.Key, clone.attributeImpls[entry.Value.GetType()]);
                }
            }

            return clone;
        }

        /// <summary>
        /// Copies the contents of this <see cref="AttributeSource"/> to the given target <see cref="AttributeSource"/>.
        /// The given instance has to provide all <see cref="IAttribute"/>s this instance contains.
        /// The actual attribute implementations must be identical in both <see cref="AttributeSource"/> instances;
        /// ideally both <see cref="AttributeSource"/> instances should use the same <see cref="AttributeFactory"/>.
        /// You can use this method as a replacement for <see cref="RestoreState(State)"/>, if you use
        /// <see cref="CloneAttributes()"/> instead of <see cref="CaptureState()"/>.
        /// </summary>
        public void CopyTo(AttributeSource target)
        {
            for (State state = GetCurrentState(); state != null; state = state.next)
            {
                Attribute targetImpl = target.attributeImpls[state.attribute.GetType()];
                if (targetImpl is null)
                {
                    throw new ArgumentException("this AttributeSource contains Attribute of type " + state.attribute.GetType().Name + " that is not in the target");
                }
                state.attribute.CopyTo(targetImpl);
            }
        }

        /// <summary>
        /// Returns a string consisting of the class's simple name, the hex representation of the identity hash code,
        /// and the current reflection of all attributes. </summary>
        /// <seealso cref="ReflectAsString(bool)"/>
        public override string ToString()
        {
            return this.GetType().Name + '@' + RuntimeHelpers.GetHashCode(this).ToString("x") + " " + ReflectAsString(false);
        }
    }
}