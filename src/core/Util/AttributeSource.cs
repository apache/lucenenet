/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using TokenStream = Lucene.Net.Analysis.TokenStream;

namespace Lucene.Net.Util
{
	
	/// <summary> An AttributeSource contains a list of different <see cref="AttributeImpl" />s,
	/// and methods to add and get them. There can only be a single instance
	/// of an attribute in the same AttributeSource instance. This is ensured
	/// by passing in the actual type of the Attribute (Class&lt;Attribute&gt;) to 
	/// the <see cref="AddAttribute(Type)" />, which then checks if an instance of
	/// that type is already present. If yes, it returns the instance, otherwise
	/// it creates a new instance and returns it.
	/// </summary>
	public class AttributeSource
	{
		/// <summary> An AttributeFactory creates instances of <see cref="AttributeImpl" />s.</summary>
		public abstract class AttributeFactory
		{
			/// <summary> returns an <see cref="AttributeImpl" /> for the supplied <see cref="Attribute" /> interface class.
			/// <p/>Signature for Java 1.5: <c>public AttributeImpl createAttributeInstance(Class%lt;? extends Attribute&gt; attClass)</c>
			/// </summary>
			public abstract AttributeImpl CreateAttributeInstance(System.Type attClass);
			
			/// <summary> This is the default factory that creates <see cref="AttributeImpl" />s using the
			/// class name of the supplied <see cref="Attribute" /> interface class by appending <c>Impl</c> to it.
			/// </summary>
			public static readonly AttributeFactory DEFAULT_ATTRIBUTE_FACTORY = new DefaultAttributeFactory();
			
			private sealed class DefaultAttributeFactory:AttributeFactory
			{
                private static readonly SupportClass.WeakHashTable attClassImplMap = new SupportClass.WeakHashTable();
                
				internal DefaultAttributeFactory()
				{
				}
				
				public override AttributeImpl CreateAttributeInstance(System.Type attClass)
				{
					try
					{
						return (AttributeImpl) System.Activator.CreateInstance(GetClassForInterface(attClass));
					}
					catch (System.UnauthorizedAccessException e)
                    {
                        throw new System.ArgumentException("Could not instantiate implementing class for " + attClass.FullName);
					}
					catch (System.Exception e)
					{
                        throw new System.ArgumentException("Could not instantiate implementing class for " + attClass.FullName);
					}
				}
				
				private static System.Type GetClassForInterface(System.Type attClass)
				{
					lock (attClassImplMap)
					{
                        WeakReference refz = (WeakReference) attClassImplMap[attClass];
                        System.Type clazz = (refz == null) ? null : ((System.Type) refz.Target);
						if (clazz == null)
						{
							try
							{
                                string name = attClass.FullName + "Impl," + attClass.Assembly.FullName;
								attClassImplMap.Add(attClass, new WeakReference( clazz = System.Type.GetType(name, true))); //OK
							}
							catch (System.Exception e)
							{
								throw new System.ArgumentException("Could not find implementing class for " + attClass.FullName);
							}
						}
						return clazz;
					}
				}
			}
		}
		
        // These two maps must always be in sync!!!
		// So they are private, final and read-only from the outside (read-only iterators)
		private SupportClass.GeneralKeyedCollection<Type, SupportClass.AttributeImplItem> attributes;
		private SupportClass.GeneralKeyedCollection<Type, SupportClass.AttributeImplItem> attributeImpls;
	    private State[] currentState = null;

		private AttributeFactory factory;
		
		/// <summary> An AttributeSource using the default attribute factory <see cref="AttributeSource.AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY" />.</summary>
		public AttributeSource():this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY)
		{
		}
		
		/// <summary> An AttributeSource that uses the same attributes as the supplied one.</summary>
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
		
		/// <summary> An AttributeSource using the supplied <see cref="AttributeFactory" /> for creating new <see cref="Attribute" /> instances.</summary>
		public AttributeSource(AttributeFactory factory)
		{
            this.attributes = new SupportClass.GeneralKeyedCollection<Type, SupportClass.AttributeImplItem>(att => att.Key);
            this.attributeImpls = new SupportClass.GeneralKeyedCollection<Type, SupportClass.AttributeImplItem>(att => att.Key);
			this.currentState = new State[1];
            this.factory = factory;
		}
		
		/// <summary> returns the used AttributeFactory.</summary>
		public virtual AttributeFactory GetAttributeFactory()
		{
			return this.factory;
		}
		
		/// <summary>Returns a new iterator that iterates the attribute classes
		/// in the same order they were added in.
		/// Signature for Java 1.5: <c>public Iterator&lt;Class&lt;? extends Attribute&gt;&gt; getAttributeClassesIterator()</c>
		///
		/// Note that this return value is different from Java in that it enumerates over the values
		/// and not the keys
		/// </summary>
		public virtual IEnumerable<Type> GetAttributeClassesIterator()
		{
            foreach (SupportClass.AttributeImplItem item in this.attributes)
            {
                yield return item.Key;
            }
		}
		
		/// <summary>Returns a new iterator that iterates all unique Attribute implementations.
		/// This iterator may contain less entries that <see cref="GetAttributeClassesIterator" />,
		/// if one instance implements more than one Attribute interface.
		/// Signature for Java 1.5: <c>public Iterator&lt;AttributeImpl&gt; getAttributeImplsIterator()</c>
		/// </summary>
		public virtual IEnumerable<AttributeImpl> GetAttributeImplsIterator()
		{
		    State initState = GetCurrentState();
            if (initState != null)
            {
                return new AnonymousEnumerable(initState);
            }
            else
            {
                return new List<AttributeImpl>();
            }
		}
		
        class AnonymousEnumerable : IEnumerable<AttributeImpl>
        {
            State state;
            public AnonymousEnumerable(State state)
            {
                this.state = state;
            }
            public IEnumerator<AttributeImpl> GetEnumerator()
            {
                return new AnonymousEnumerator(state);
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new AnonymousEnumerator(state);
            }
        }
		
        class AnonymousEnumerator : IEnumerator<AttributeImpl>
        {
            State state;
            public AnonymousEnumerator(State state)
            {
                this.state = state;
            }

            AttributeImpl InternalCurrent
            {
                get
                {
                    if (state == null)
                        throw new KeyNotFoundException();

                    AttributeImpl att = state.attribute;
                    state = state.next;
                    return att;
                }
            }
 
            public AttributeImpl Current
            {
                get { return InternalCurrent; }
            }

            public void Dispose()
            {}

            object System.Collections.IEnumerator.Current
            {
                get { return InternalCurrent; }
            }

            public bool MoveNext()
            {
                return state != null;
            }

            public void Reset()
            {
                
            }
        }

        /// <summary>a cache that stores all interfaces for known implementation classes for performance (slow reflection) </summary>
		private static readonly SupportClass.WeakHashTable knownImplClasses = new SupportClass.WeakHashTable();

        // {{Aroush-2.9 Port issue, need to mimic java's IdentityHashMap
        /*
         * From Java docs:
         * This class implements the Map interface with a hash table, using 
         * reference-equality in place of object-equality when comparing keys 
         * (and values). In other words, in an IdentityHashMap, two keys k1 and k2 
         * are considered equal if and only if (k1==k2). (In normal Map 
         * implementations (like HashMap) two keys k1 and k2 are considered 
         * equal if and only if (k1==null ? k2==null : k1.equals(k2)).) 
         */
        // Aroush-2.9}}
		
		/// <summary>Adds a custom AttributeImpl instance with one or more Attribute interfaces. </summary>
		public virtual void  AddAttributeImpl(AttributeImpl att)
		{
			System.Type clazz = att.GetType();
			if (attributeImpls.Contains(clazz))
				return ;
			System.Collections.ArrayList foundInterfaces;
			lock (knownImplClasses)
			{
				foundInterfaces = (System.Collections.ArrayList) knownImplClasses[clazz];
				if (foundInterfaces == null)
				{
                    // we have a strong reference to the class instance holding all interfaces in the list (parameter "att"),
                    // so all WeakReferences are never evicted by GC
					knownImplClasses.Add(clazz, foundInterfaces = new System.Collections.ArrayList());
					// find all interfaces that this attribute instance implements
					// and that extend the Attribute interface
					System.Type actClazz = clazz;
					do 
					{
						System.Type[] interfaces = actClazz.GetInterfaces();
						for (int i = 0; i < interfaces.Length; i++)
						{
							System.Type curInterface = interfaces[i];
							if (curInterface != typeof(Attribute) && typeof(Attribute).IsAssignableFrom(curInterface))
							{
								foundInterfaces.Add(new WeakReference(curInterface));
							}
						}
						actClazz = actClazz.BaseType;
					}
					while (actClazz != null);
				}
			}
			
			// add all interfaces of this AttributeImpl to the maps
			for (System.Collections.IEnumerator it = foundInterfaces.GetEnumerator(); it.MoveNext(); )
			{
                WeakReference curInterfaceRef = (WeakReference)it.Current;
				System.Type curInterface = (System.Type) curInterfaceRef.Target;
                System.Diagnostics.Debug.Assert(curInterface != null,"We have a strong reference on the class holding the interfaces, so they should never get evicted");
				// Attribute is a superclass of this interface
				if (!attributes.ContainsKey(curInterface))
				{
					// invalidate state to force recomputation in captureState()
					this.currentState[0] = null;
                    attributes.Add(new SupportClass.AttributeImplItem(curInterface, att));
                    if (!attributeImpls.ContainsKey(clazz))
                    {
                        attributeImpls.Add(new SupportClass.AttributeImplItem(clazz, att));
                    }
				}
			}
		}
		
		/// <summary> The caller must pass in a Class&lt;? extends Attribute&gt; value.
		/// This method first checks if an instance of that class is 
		/// already in this AttributeSource and returns it. Otherwise a
		/// new instance is created, added to this AttributeSource and returned. 
		/// Signature for Java 1.5: <c>public &lt;T extends Attribute&gt; T addAttribute(Class&lt;T&gt;)</c>
		/// </summary>
		public virtual Attribute AddAttribute(System.Type attClass)
		{
			if (!attributes.ContainsKey(attClass))
			{
                if (!(attClass.IsInterface &&  typeof(Attribute).IsAssignableFrom(attClass))) 
                {
                    throw new ArgumentException(
                        "AddAttribute() only accepts an interface that extends Attribute, but " +
                        attClass.FullName + " does not fulfil this contract."
                    );
                }

				AttributeImpl attImpl = this.factory.CreateAttributeInstance(attClass);
				AddAttributeImpl(attImpl);
				return attImpl;
			}
			else
			{
				return attributes[attClass].Value;
			}
		}
		
		/// <summary>Returns true, iff this AttributeSource has any attributes </summary>
		public virtual bool HasAttributes()
		{
			return this.attributes.Count != 0;
		}
		
		/// <summary> The caller must pass in a Class&lt;? extends Attribute&gt; value. 
		/// Returns true, iff this AttributeSource contains the passed-in Attribute.
		/// Signature for Java 1.5: <c>public boolean hasAttribute(Class&lt;? extends Attribute&gt;)</c>
		/// </summary>
		public virtual bool HasAttribute(System.Type attClass)
		{
			return this.attributes.Contains(attClass);
		}
		
		/// <summary> The caller must pass in a Class&lt;? extends Attribute&gt; value. 
		/// Returns the instance of the passed in Attribute contained in this AttributeSource
		/// Signature for Java 1.5: <c>public &lt;T extends Attribute&gt; T getAttribute(Class&lt;T&gt;)</c>
		/// 
		/// </summary>
		/// <throws>  IllegalArgumentException if this AttributeSource does not contain the </throws>
		/// <summary>         Attribute. It is recommended to always use <see cref="AddAttribute" /> even in consumers
		/// of TokenStreams, because you cannot know if a specific TokenStream really uses
		/// a specific Attribute. <see cref="AddAttribute" /> will automatically make the attribute
		/// available. If you want to only use the attribute, if it is available (to optimize
		/// consuming), use <see cref="HasAttribute" />.
		/// </summary>
		public virtual Attribute GetAttribute(System.Type attClass)
		{
            if (!this.attributes.ContainsKey(attClass))
            {
                throw new System.ArgumentException("This AttributeSource does not have the attribute '" + attClass.FullName + "'.");
            }
            else
            {
                return this.attributes[attClass].Value;
            }
		}
		
		/// <summary> This class holds the state of an AttributeSource.</summary>
		/// <seealso cref="CaptureState">
		/// </seealso>
		/// <seealso cref="RestoreState">
		/// </seealso>
		public sealed class State : System.ICloneable
		{
			internal /*private*/ AttributeImpl attribute;
			internal /*private*/ State next;
			
			public System.Object Clone()
			{
				State clone = new State();
				clone.attribute = (AttributeImpl) attribute.Clone();
				
				if (next != null)
				{
					clone.next = (State) next.Clone();
				}
				
				return clone;
			}
		}

        private State GetCurrentState()
        {
            State s = currentState[0];
            if (s != null || !HasAttributes())
            {
                return s;
            }

            State c = s = currentState[0] = new State();
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

	    /// <summary> Resets all Attributes in this AttributeSource by calling
		/// <see cref="AttributeImpl.Clear()" /> on each Attribute implementation.
		/// </summary>
		public virtual void  ClearAttributes()
		{
			for (State state = GetCurrentState(); state != null; state = state.next)
			{
				state.attribute.Clear();
			}
	    }
		
		/// <summary> Captures the state of all Attributes. The return value can be passed to
		/// <see cref="RestoreState" /> to restore the state of this or another AttributeSource.
		/// </summary>
		public virtual State CaptureState()
		{
		    State state = this.GetCurrentState();
		    return (state == null) ? null : (State) state.Clone();
		}
		
		/// <summary> Restores this state by copying the values of all attribute implementations
		/// that this state contains into the attributes implementations of the targetStream.
		/// The targetStream must contain a corresponding instance for each argument
		/// contained in this state (e.g. it is not possible to restore the state of
		/// an AttributeSource containing a TermAttribute into a AttributeSource using
		/// a Token instance as implementation).
		/// 
		/// Note that this method does not affect attributes of the targetStream
		/// that are not contained in this state. In other words, if for example
		/// the targetStream contains an OffsetAttribute, but this state doesn't, then
		/// the value of the OffsetAttribute remains unchanged. It might be desirable to
		/// reset its value to the default, in which case the caller should first
        /// call <see cref="AttributeSource.ClearAttributes()" /> on the targetStream.   
		/// </summary>
		public virtual void  RestoreState(State state)
		{
			if (state == null)
				return ;
			
			do 
			{
				if (!attributeImpls.ContainsKey(state.attribute.GetType()))
				{
					throw new System.ArgumentException("State contains an AttributeImpl that is not in this AttributeSource");
				}
				state.attribute.CopyTo(attributeImpls[state.attribute.GetType()].Value);
				state = state.next;
			}
			while (state != null);
		}
		
		public override int GetHashCode()
		{
			int code = 0;
            for (State state = GetCurrentState(); state != null; state = state.next)
            {
                code = code*31 + state.attribute.GetHashCode();
            }

		    return code;
        }
		
		public  override bool Equals(System.Object obj)
		{
			if (obj == this)
			{
				return true;
			}
			
			if (obj is AttributeSource)
			{
				AttributeSource other = (AttributeSource) obj;
				
				if (HasAttributes())
				{
					if (!other.HasAttributes())
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
					return !other.HasAttributes();
				}
			}
			else
				return false;
		}
		
		public override System.String ToString()
		{
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			sb.Append('(');
			
			if (HasAttributes())
			{
				if (currentState[0] == null)
				{
				    currentState[0] = GetCurrentState();
				}
				for (State state = currentState[0]; state != null; state = state.next)
				{
					if (state != currentState[0])
						sb.Append(',');
					sb.Append(state.attribute.ToString());
				}
			}
			sb.Append(')');
			return sb.ToString();
		}
		
		/// <summary> Performs a clone of all <see cref="AttributeImpl" /> instances returned in a new
		/// AttributeSource instance. This method can be used to e.g. create another TokenStream
		/// with exactly the same attributes (using <see cref="AttributeSource(AttributeSource)" />)
		/// </summary>
		public virtual AttributeSource CloneAttributes()
		{
			AttributeSource clone = new AttributeSource(this.factory);
			
			// first clone the impls
			if (HasAttributes())
			{
				for (State state = GetCurrentState() ; state != null; state = state.next)
				{
					AttributeImpl impl = (AttributeImpl) state.attribute.Clone();

                    if (!clone.attributeImpls.ContainsKey(impl.GetType()))
                    {
                        clone.attributeImpls.Add(new SupportClass.AttributeImplItem(impl.GetType(), impl));
                    }
                }
			}
			
			// now the interfaces
            foreach (SupportClass.AttributeImplItem att in this.attributes)
			{
                clone.attributes.Add(new SupportClass.AttributeImplItem(att.Key, clone.attributeImpls[att.Value.GetType()].Value));
			}
			
			return clone;
		}
	}
}