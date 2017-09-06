using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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
        public abstract class AttributeFactory
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
                internal static readonly WeakIdentityMap<Type, WeakReference> attClassImplMap = 
                    WeakIdentityMap<Type, WeakReference>.NewConcurrentHashMap(false);

                internal DefaultAttributeFactory()
                {
                }

                public override Attribute CreateAttributeInstance<S>()
                {
                    try
                    {
                        return (Attribute)System.Activator.CreateInstance(GetClassForInterface<S>());
                    }
                    catch (Exception e)
                    {
                        throw new System.ArgumentException("Could not instantiate implementing class for " + typeof(S).FullName, e);
                    }
                }

                internal static Type GetClassForInterface<T>() where T : IAttribute
                {
                    var attClass = typeof(T);
                    WeakReference @ref = attClassImplMap.Get(attClass);
                    Type clazz = (@ref == null) ? null : (Type)@ref.Target;
                    if (clazz == null)
                    {
                        // we have the slight chance that another thread may do the same, but who cares?
                        try
                        {
                            string name = attClass.FullName.Replace(attClass.Name, attClass.Name.Substring(1)) + ", " + attClass.GetTypeInfo().Assembly.FullName;
                            attClassImplMap.Put(attClass, new WeakReference(clazz = Type.GetType(name, true)));
                        }
                        catch (Exception e)
                        {
                            throw new System.ArgumentException("Could not find implementing class for " + attClass.Name, e);
                        }
                    }
                    return clazz;
                }
            }
        }

        /// <summary>
        /// This class holds the state of an <see cref="AttributeSource"/>. </summary>
        /// <seealso cref="CaptureState()"/>
        /// <seealso cref="RestoreState(State)"/>
        public sealed class State : ICloneable
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
        private readonly GeneralKeyedCollection<Type, AttributeItem> attributes;

        private readonly GeneralKeyedCollection<Type, AttributeItem> attributeImpls;
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
            if (input == null)
            {
                throw new System.ArgumentException("input AttributeSource must not be null");
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
            this.attributes = new GeneralKeyedCollection<Type, AttributeItem>(att => att.Key);
            this.attributeImpls = new GeneralKeyedCollection<Type, AttributeItem>(att => att.Key);
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
                return new IteratorAnonymousInnerClassHelper(this, initState);
            }
            else
            {
                return (new HashSet<Attribute>()).GetEnumerator();
            }
        }

        private class IteratorAnonymousInnerClassHelper : IEnumerator<Attribute>
        {
            private readonly AttributeSource outerInstance;

            private AttributeSource.State initState;
            private Attribute current;

            public IteratorAnonymousInnerClassHelper(AttributeSource outerInstance, AttributeSource.State initState)
            {
                this.outerInstance = outerInstance;
                this.initState = initState;
                state = initState;
            }

            private State state;

            public virtual void Remove()
            {
                throw new System.NotSupportedException();
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (state == null)
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
                throw new NotSupportedException();
            }

            public Attribute Current
            {
                get { return current; }
                set { current = value; }
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }

        /// <summary>
        /// A cache that stores all interfaces for known implementation classes for performance (slow reflection) </summary>
        private static readonly WeakIdentityMap<Type, LinkedList<WeakReference>> knownImplClasses =
            WeakIdentityMap<Type, LinkedList<WeakReference>>.NewConcurrentHashMap(false);

        internal static LinkedList<WeakReference> GetAttributeInterfaces(Type clazz)
        {
            LinkedList<WeakReference> foundInterfaces = knownImplClasses.Get(clazz);
            lock (knownImplClasses)
            {
                if (foundInterfaces == null)
                {
                    // we have the slight chance that another thread may do the same, but who cares?
                    foundInterfaces = new LinkedList<WeakReference>();
                    // find all interfaces that this attribute instance implements
                    // and that extend the Attribute interface
                    Type actClazz = clazz;
                    do
                    {
                        foreach (Type curInterface in actClazz.GetInterfaces())
                        {
                            if (curInterface != typeof(IAttribute) && (typeof(IAttribute)).IsAssignableFrom(curInterface))
                            {
                                foundInterfaces.AddLast(new WeakReference(curInterface));
                            }
                        }
                        actClazz = actClazz.GetTypeInfo().BaseType;
                    } while (actClazz != null);
                    knownImplClasses.Put(clazz, foundInterfaces);
                }
            }
            return foundInterfaces;
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

            LinkedList<WeakReference> foundInterfaces = GetAttributeInterfaces(clazz);

            // add all interfaces of this Attribute to the maps
            foreach (WeakReference curInterfaceRef in foundInterfaces)
            {
                Type curInterface = (Type)curInterfaceRef.Target;
                Debug.Assert(curInterface != null, "We have a strong reference on the class holding the interfaces, so they should never get evicted");
                // Attribute is a superclass of this interface
                if (!attributes.ContainsKey(curInterface))
                {
                    // invalidate state to force recomputation in captureState()
                    this.currentState[0] = null;
                    attributes.Add(new AttributeItem(curInterface, att));
                    if (!attributeImpls.ContainsKey(clazz))
                    {
                        attributeImpls.Add(new AttributeItem(clazz, att));
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
            if (!attributes.ContainsKey(attClass))
            {
                if (!(attClass.GetTypeInfo().IsInterface && typeof(IAttribute).IsAssignableFrom(attClass)))
                {
                    throw new ArgumentException("AddAttribute() only accepts an interface that extends IAttribute, but " + attClass.FullName + " does not fulfil this contract.");
                }

                AddAttributeImpl(this.factory.CreateAttributeInstance<T>());
            }

            T returnAttr;
            try
            {
                returnAttr = (T)(IAttribute)attributes[attClass].Value;
            }
#pragma warning disable 168
            catch (KeyNotFoundException knf)
#pragma warning restore 168
            {
                return default(T);
            }
            return returnAttr;
        }

        /// <summary>
        /// Returns <c>true</c>, if this <see cref="AttributeSource"/> has any attributes </summary>
        public bool HasAttributes
        {
            get { return this.attributes.Count > 0; }
        }

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
            if (!attributes.ContainsKey(attClass))
            {
                throw new System.ArgumentException("this AttributeSource does not have the attribute '" + attClass.Name + "'.");
            }
            return (T)(IAttribute)this.attributes[attClass].Value;
        }

        private State GetCurrentState()
        {
            State s = currentState[0];
            if (s != null || !HasAttributes)
            {
                return s;
            }
            var c = s = currentState[0] = new State();
            var it = attributeImpls.Values().GetEnumerator();
            it.MoveNext();
            c.attribute = it.Current.Value;
            while (it.MoveNext())
            {
                c.next = new State();
                c = c.next;
                c.attribute = it.Current.Value;
            }
            return s;
        }

        /// <summary>
        /// Resets all <see cref="Attribute"/>s in this <see cref="AttributeSource"/> by calling
        /// <see cref="Attribute.Clear()"/> on each <see cref="IAttribute"/> implementation.
        /// </summary>
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
        public virtual State CaptureState()
        {
            State state = this.GetCurrentState();
            return (state == null) ? null : (State)state.Clone();
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
            if (state == null)
            {
                return;
            }

            do
            {
                if (!attributeImpls.ContainsKey(state.attribute.GetType()))
                {
                    throw new System.ArgumentException("State contains Attribute of type " + state.attribute.GetType().Name + " that is not in in this AttributeSource");
                }
                state.attribute.CopyTo(attributeImpls[state.attribute.GetType()].Value);
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

            if (obj is AttributeSource)
            {
                AttributeSource other = (AttributeSource)obj;

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
        public string ReflectAsString(bool prependAttClass)
        {
            StringBuilder buffer = new StringBuilder();
            ReflectWith(new AttributeReflectorAnonymousInnerClassHelper(this, prependAttClass, buffer));
            return buffer.ToString();
        }

        private class AttributeReflectorAnonymousInnerClassHelper : IAttributeReflector
        {
            private readonly AttributeSource outerInstance;

            private bool prependAttClass;
            private StringBuilder buffer;

            public AttributeReflectorAnonymousInnerClassHelper(AttributeSource outerInstance, bool prependAttClass, StringBuilder buffer)
            {
                this.outerInstance = outerInstance;
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
                buffer.Append(key).Append('=').Append(object.ReferenceEquals(value, null) ? "null" : value);
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
                        clone.attributeImpls.Add(new AttributeItem(impl.GetType(), impl));
                    }
                }

                // now the interfaces
                foreach (var entry in this.attributes)
                {
                    clone.attributes.Add(new AttributeItem(entry.Key, clone.attributeImpls[entry.Value.GetType()].Value));
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
                Attribute targetImpl = target.attributeImpls[state.attribute.GetType()].Value;
                if (targetImpl == null)
                {
                    throw new System.ArgumentException("this AttributeSource contains Attribute of type " + state.attribute.GetType().Name + " that is not in the target");
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