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
    using System.Diagnostics.CodeAnalysis;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.TokenAttributes;
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
    public partial class AttributeSource 
    {
        private static readonly object instanceLock = new object();
        private static readonly WeakDictionary<Type, LinkedList<WeakReference<Type>>> knownAttributeClasses =
            new WeakDictionary<Type, LinkedList<WeakReference<Type>>>();

        private readonly Dictionary<Type, AttributeBase> interfaceMap = new Dictionary<Type, AttributeBase>();
        private readonly Dictionary<Type, AttributeBase> attributeMap = new Dictionary<Type, AttributeBase>();
        
        private AttributeSourceState[] currentState;


        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeSource"/> class. 
        /// This constructor uses the <see cref="AttributeFactory.DefaultFactory"/>
        /// </summary>
        public AttributeSource()
            : this(AttributeFactory.DefaultFactory)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeSource"/> class. 
        /// This uses another attribute source to create a new one based on the internally
        /// store maps of <see cref="Type"/> to instances of <see cref="AttributeBase"/>
        /// </summary>
        /// <param name="source">The source.</param>
        public AttributeSource(AttributeSource source)
        {
            this.attributeMap = source.attributeMap;
            this.interfaceMap = source.interfaceMap;
            this.currentState = source.currentState;
            this.Factory = source.Factory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeSource"/> class. Creates
        /// a new attribute source with the specified factory.
        /// </summary>
        /// <param name="factory">The factory.</param>
        public AttributeSource(AttributeFactory factory)
        {
            this.attributeMap = new Dictionary<Type, AttributeBase>();
            this.interfaceMap = new Dictionary<Type, AttributeBase>();
            this.currentState = new AttributeSourceState[1];

            this.Factory = factory;
        }


        /// <summary>
        /// Gets the attribute factory.
        /// </summary>
        /// <value>The factory.</value>
        public AttributeFactory Factory { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has attributes.
        /// </summary>
        /// <value>
        ///    <c>true</c> if this instance has attributes; otherwise, <c>false</c>.
        /// </value>
        public bool HasAttributes
        {
            get { return this.attributeMap.Count > 0; }
        }

        

        /// <summary>
        /// Gets the attribute interfaces.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     an instance of <see cref="LinkedList{T}"/> of <see cref="WeakReference{T}"/> of <see cref="Type"/>
        ///     that hold the known interfaces that inherit from <see cref="IAttribute"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1006:DoNotNestGenericTypeMemberSignatures",
            Justification = "I considered WeakReference<T> is needed.")]
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
        /// Adds the attribute.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        public void AddAttribute(AttributeBase attribute)
        {
            if (attribute == null)
                throw new ArgumentNullException("attribute");

            Type type = attribute.GetType();
            if (this.attributeMap.ContainsKey(type))
                return;

            var foundInterfaces = GetAttributeInterfaces(type);
            foreach (var interfaceTypeRef in foundInterfaces)
            {
                var interfaceType = interfaceTypeRef.Target;

#if DEBUG
                System.Diagnostics.Debug.Assert(interfaceType != null, "We have a strong reference on the class holding the interfaces, so they should never get evicted");
#endif
                
                if (!this.interfaceMap.ContainsKey(interfaceType))
                {
                    this.currentState[0] = null;
                    this.attributeMap.Add(type, attribute);
                    this.interfaceMap.Add(interfaceType, attribute);
                }
            }
        }

        /// <summary>
        /// Adds an attribute.
        /// </summary>
        /// <param name="attributeType">The type of attribute that is to be added.</param>
        /// <exception cref="ArgumentNullException">
        ///         Thrown when <paramref name="attributeType"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///     Thrown when <paramref name="attributeType"/> is not a type of interface
        ///     that inherits from <see cref="IAttribute"/>.
        /// </exception>
        /// <returns>
        /// The instance of <see cref="AttributeBase"/> that was created.
        /// </returns>
        public AttributeBase AddAttribute(Type attributeType)
        {
            if (attributeType == null)
                throw new ArgumentNullException("attributeType");

            AttributeBase instance;

            if (this.attributeMap.TryGetValue(attributeType, out instance))
            {
                if (!attributeType.IsInterface)
                    throw new ArgumentException("The type must be an interface.");

                if (!attributeType.IsSubclassOf(typeof(IAttribute)))
                    throw new ArgumentException(
                        "The interface type '{0}' is not a subclass of IAttribute."
                        .Inject(attributeType.FullName));

                this.AddAttribute(instance = this.Factory.CreateAttributeInstance(attributeType));
            }

            return instance;
        }

        /// <summary>
        /// Adds the attribute.
        /// </summary>
        /// <typeparam name="T">The type of AttributeBase that is to be added.</typeparam>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        public T AddAttribute<T>() where T : AttributeBase, IAttribute
        {
            return (T)this.AddAttribute(typeof(T));
        }

        /// <summary>
        /// Captures the state.
        /// </summary>
        /// <returns>An instance of <see cref="AttributeSourceState"/>.</returns>
        public AttributeSourceState CaptureState()
        {
            AttributeSourceState state = this.GetCurrentState();
            return state == null ? null : state.Clone();
        }



        /// <summary>
        /// Clears the attributes.
        /// </summary>
        public void ClearAttributes()
        {
            for (var state = this.GetCurrentState(); state != null; state = state.Next)
                state.Attribute.Clear();
        }

        /// <summary>
        /// Clones the current <see cref="AttributeSource"/> and injects clones of all of 
        /// the stored <see cref="AttributeBase"/> instances into the <see cref="AttributeSource"/> clone.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     This method can be use to create another <see cref="Lucene.Net.Analysis.TokenStream"/>
        ///     with exactly the same attributes. You can also use this method as a non-performant 
        ///     replacement for <see cref="CaptureState"/> for times you wish to modify or peek at 
        ///     the captured state.
        ///     </para>
        /// </remarks>
        /// <returns>
        /// A new instance of <see cref="AttributeSource"/> with cloned attributes.
        /// </returns>
        public AttributeSource CloneAttributes()
        {
            var clone = new AttributeSource(this.Factory);

            if (this.HasAttributes)
            {
                this.ForEachState((state) => {
                    clone.attributeMap.Add(state.Attribute.GetType(), state.Attribute.Clone());
                });

                foreach (var pair in this.interfaceMap)
                    clone.interfaceMap.Add(pair.Key, pair.Value.Clone());
            }

            return clone;
        }

        /// <summary>
        /// Determines whether the specified type contains attribute.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>
        ///     <c>true</c> if the specified type contains attribute; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsAttribute(Type type)
        {
            if (type.IsInterface)
                return this.interfaceMap.ContainsKey(type);
            else
                return this.attributeMap.ContainsKey(type);
        }

        /// <summary>
        /// Copies the contents of this <see cref="AttributeSource"/> to the specified 
        /// <see cref="AttributeSource"/> <paramref name="source"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     The <paramref name="source"/> has to provide all <see cref="IAttribute"/>s this instance contains. 
        ///     The actual attribute implementations must be identical in both <see cref="AttributeSource"/> instances;
        ///     ideally both <see cref="AttributeSource"/> instances should use the same <see cref="AttributeFactory"/>.
        ///     You can use this method as a replacement for <see cref="RestoreState"/>, if you use
        ///     <see cref="CloneAttributes"/> instead of <see cref="CaptureState"/>.
        ///     </para>
        /// </remarks>
        /// <param name="source">The source.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when the <paramref name="source"/> contains an instance
        ///     of <see cref="IAttribute"/> that current <see cref="AttributeSource"/>
        ///     object does not contain.
        /// </exception>
        public void CopyTo(AttributeSource source)
        {
            this.ForEachState((state) => {
                var attribute = source.attributeMap[state.Attribute.GetType()];
                if (attribute == null)
                    throw new ArgumentException(this.CreateStateExceptionMessage(state), "state");

                state.Attribute.CopyTo(attribute);
            });
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj == this)
                return true;

            AttributeSource compare = (AttributeSource)obj;

            if (compare == null)
                return false;

            if (!this.HasAttributes)
                return !compare.HasAttributes;

            if (!compare.HasAttributes ||
                (compare.attributeMap.Count != this.attributeMap.Count))
                return false;

            var localState = this.GetCurrentState();
            var compareState = compare.GetCurrentState();

            while (localState != null && compareState != null)
            {
                // this differs from java-lucene-core: 
                // .NET's attributes will return false if the types mismatch.  
                if (!localState.Attribute.Equals(compareState.Attribute))
                    return false;

                localState = localState.Next;
                compareState = compareState.Next;
            }

            return true;
        }

        /// <summary>
        /// Finds the specified attribute based on the <typeparamref name="T"/> which must
        /// a type that implements <see cref="AttributeBase"/> and <see cref="IAttribute"/>.
        /// </summary>
        /// <typeparam name="T">The type that </typeparam>
        /// <returns>An instance of <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentException">
        ///     Thrown when the type of <typeparamref name="T"/> could not be found in this instance.
        /// </exception>
        public T FindAttribute<T>() where T : IAttribute
        {
            AttributeBase value;
            if (!this.interfaceMap.TryGetValue(typeof(T), out value))
                throw new ArgumentException(
                    "The specified type '{0}' could not be found, try using " +
                    "ContainsAttribute(Type) first."
                    .Inject(typeof(T).FullName));
      
            object unbox = value;
            return (T)unbox;
        }

        /// <summary>
        /// Finds the specified attribute based on the <paramref name="type"/> which must
        /// be a <see cref="Type"/> that implements <see cref="AttributeBase"/> 
        /// and <see cref="IAttribute"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of attribute to find.</param>
        /// <returns>
        /// An instance of <see cref="AttributeBase"/>.
        /// </returns>
        //// getAttribute(Class<A> attClass)
        public AttributeBase FindAttribute(Type type)
        {
            AttributeBase value;

            if (type.IsInterface)
            {
                if (!this.interfaceMap.TryGetValue(type, out value))
                    throw new ArgumentException(
                        "The specified type '{0}' could not be found, try using " +
                        "ContainsAttribute(Type) first.".Inject(type.FullName));
            } 
            else
            {
                if (!this.attributeMap.TryGetValue(type, out value))
                    throw new ArgumentException(
                        "The specified type '{0}' could not be found, try using " +
                        "ContainsAttribute(Type) first.".Inject(type.FullName));
            }

           
            return value;
        }

        /// <summary>
        /// Returns the enumerator for stored interface types in the same order
        /// they were stored int.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="IEnumerator{Type}"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Due to microsoft's bad design, developers expect the Get[Type]Enumerator name convention. ")]
        public IEnumerator<Type> GetAttributeTypesEnumerator()
        {
            return this.interfaceMap.Keys.GetEnumerator();
        }

        /// <summary>
        /// Creates and returns an <see cref="AttributeEnumerator"/> that will enumerate
        /// throw all available instances of <see cref="AttributeBase"/>. The enumerator
        /// may contain less instances than <see cref="GetAttributeTypesEnumerator"/>.
        /// </summary>
        /// <returns>
        /// An instance of <see cref="IEnumerator{AttributeBase}"/>.
        /// </returns>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate",
            Justification = "Due to microsoft's bad design, developers expect the Get[Type]Enumerator name convention. ")]
        public IEnumerator<AttributeBase> GetAttributeEnumerator()
        {
            return new AttributeEnumerator(this);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int code = 0;
            for (var state = this.GetCurrentState(); state != null; state = state.Next)
                code = (code * 31) + state.Attribute.GetHashCode();

            return code;
        }

        /// <summary>
        /// Restores the attribute source state by copying the values of all
        /// attribute instances that the state contains into the target stream.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The target stream must contain a corresponding instance for 
        ///         each argument contained in this state. It is not possible 
        ///         to restore the state of an <see cref="AttributeSource"/>
        ///         containing a <c>TermAttribute</c> into an <see cref="AttributeSource"/>
        ///         using a <see cref="Token"/> instance.
        ///     </para>
        ///     <note>
        ///         This method does not affect attributes of the target stream
        ///         that are not contained in this state. If the target stream
        ///         contains an <see cref="OffsetAttribute"/>, but this <paramref name="state"/> 
        ///         does not, then the value of the <see cref="OffsetAttribute"/> 
        ///         remains unchanged.
        ///     </note>
        ///     <para>
        ///         It might be desirable to reset its value to the default. 
        ///         The caller should first call <see cref="ClearAttributes"/> on 
        ///         the target stream in order to reset its value.
        ///     </para>
        /// </remarks>
        /// <param name="state">The state.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when a state contains ain attribute that is not found
        ///     within the current instance of <see cref="AttributeSource"/>
        /// </exception>
        public void RestoreState(AttributeSourceState state)
        {
            if (state == null)
                return;
            do
            {
                var attribute = this.attributeMap[state.Attribute.GetType()];

                if (attribute == null)
                    throw new ArgumentException(this.CreateStateExceptionMessage(state));

                state.Attribute.CopyTo(attribute);
                state = state.Next;
            } 
            while (state != null);
        }

        /// <summary>
        /// Enumerates over the stored states in the <see cref="AttributeSource"/>
        /// </summary>
        /// <param name="invoke">The action to invoke on each state.</param>
        protected void ForEachState(Action<AttributeSourceState> invoke)
        {
            for (var state = this.GetCurrentState(); state != null; state = state.Next)
                invoke(state);
        }

        private AttributeSourceState GetCurrentState()
        {
            AttributeSourceState state = this.currentState[0];

            if (state != null || !this.HasAttributes)
                return state;

            AttributeSourceState current = state = this.currentState[0] = new AttributeSourceState();

            var enumerator = this.attributeMap.Values.GetEnumerator();
            enumerator.MoveNext();
            current.Attribute = enumerator.Current;

            while (enumerator.MoveNext())
            {
                current = current.Next = new AttributeSourceState();
                current.Attribute = enumerator.Current;
            }

            return state;
        }

        private string CreateStateExceptionMessage(AttributeSourceState state)
        {
            return 
                "The state contains an attribute of type '{0}' " +
                "that is currently not found within this instance of '{1}#{2}'. "
                .Inject(state.Attribute.GetType(), this.GetType().Name, this.GetHashCode());
        }

        /// <summary>
        /// The enumerator for <see cref="AttributeBase"/> instances stored inside
        /// of an <see cref="AttributeSource"/> instance.
        /// </summary>
        public sealed class AttributeEnumerator : IEnumerator<AttributeBase>
        {
            private AttributeSourceState state;
            private AttributeSource source;

            /// <summary>
            /// Initializes a new instance of the <see cref="AttributeEnumerator"/> class.
            /// </summary>
            /// <param name="source">The source.</param>
            internal AttributeEnumerator(AttributeSource source)
            {
                this.source = source;
                this.state = source.GetCurrentState();
            }

            /// <summary>
            /// Gets the current <see cref="AttributeBase"/> at this position.
            /// </summary>
            /// <value>The current.</value>
            /// <exception cref="ObjectDisposedException">
            ///     Thrown when <see cref="Dispose"/> has already been called on the object.
            /// </exception>
            public AttributeBase Current
            {
                get
                {
                    if (this.source == null)
                        throw new ObjectDisposedException(this.GetType().Name);

                    return this.state.Attribute;
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return this.Current; }
            }


            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                this.state = null;
                this.source = null;
            }

           



            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            /// <exception cref="ObjectDisposedException">
            ///     Thrown when <see cref="Dispose"/> has already been called on the object.
            /// </exception>
            public bool MoveNext()
            {
                if (this.source == null)
                    throw new ObjectDisposedException(this.GetType().Name);

                this.state = this.state.Next;
                return this.state != null;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
            /// <exception cref="ObjectDisposedException">
            ///     Thrown when <see cref="Dispose"/> has already been called on the object.
            /// </exception>
            public void Reset()
            {
                if (this.source == null)
                    throw new ObjectDisposedException(this.GetType().Name);

                this.state = this.source.GetCurrentState();
            }
        }
    }
}
