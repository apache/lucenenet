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

using TokenStream = Lucene.Net.Analysis.TokenStream;

namespace Lucene.Net.Util
{
	
	/// <summary> An AttributeSource contains a list of different {@link AttributeImpl}s,
	/// and methods to add and get them. There can only be a single instance
	/// of an attribute in the same AttributeSource instance. This is ensured
	/// by passing in the actual type of the Attribute (Class&lt;Attribute&gt;) to 
	/// the {@link #AddAttribute(Class)}, which then checks if an instance of
	/// that type is already present. If yes, it returns the instance, otherwise
	/// it creates a new instance and returns it.
	/// </summary>
	public class AttributeSource
	{
		private class AnonymousClassIterator : System.Collections.IEnumerator
		{
			public AnonymousClassIterator(Lucene.Net.Util.AttributeSource.State initState, AttributeSource enclosingInstance)
			{
				InitBlock(initState, enclosingInstance);
			}
			private void  InitBlock(Lucene.Net.Util.AttributeSource.State initState, AttributeSource enclosingInstance)
			{
				this.initState = initState;
				this.enclosingInstance = enclosingInstance;
				state = initState;
			}
			private Lucene.Net.Util.AttributeSource.State initState;
			private AttributeSource enclosingInstance;
			public virtual System.Object Current
			{
				get
				{
					if (state == null)
						throw new System.ArgumentOutOfRangeException();
					AttributeImpl att = state.attribute;
					state = state.next;
					return att;
				}
				
			}
			public AttributeSource Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			private State state;
			
			public virtual void  Remove()
			{
				throw new System.NotSupportedException();
			}
			
			public virtual bool MoveNext()
			{
				return state != null;
			}
			
			virtual public void  Reset()
			{
                System.Diagnostics.Debug.Fail("Port issue:", "Lets see if we need this"); // {{Aroush-2.9}}
			}
		}
		/// <summary> An AttributeFactory creates instances of {@link AttributeImpl}s.</summary>
		public abstract class AttributeFactory
		{
			/// <summary> returns an {@link AttributeImpl} for the supplied {@link Attribute} interface class.
			/// <p>Signature for Java 1.5: <code>public AttributeImpl createAttributeInstance(Class%lt;? extends Attribute&gt; attClass)</code>
			/// </summary>
			public abstract AttributeImpl CreateAttributeInstance(System.Type attClass);
			
			/// <summary> This is the default factory that creates {@link AttributeImpl}s using the
			/// class name of the supplied {@link Attribute} interface class by appending <code>Impl</code> to it.
			/// </summary>
			public static readonly AttributeFactory DEFAULT_ATTRIBUTE_FACTORY = new DefaultAttributeFactory();
			
			private sealed class DefaultAttributeFactory:AttributeFactory
			{
				private static readonly System.Collections.Hashtable attClassImplMap = new System.Collections.Hashtable();

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
						throw new System.ArgumentException("Could not instantiate class " + attClass.FullName);
					}
					catch (System.Exception e)
					{
						throw new System.ArgumentException("Could not instantiate class " + attClass.FullName);
					}
				}
				
				private static System.Type GetClassForInterface(System.Type attClass)
				{
					lock (attClassImplMap)
					{
						System.Type clazz = (System.Type) attClassImplMap[attClass];
						if (clazz == null)
						{
							try
							{
								attClassImplMap.Add(attClass, clazz = System.Type.GetType(attClass.FullName + "Impl"));
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
		private System.Collections.IDictionary attributes;
		private System.Collections.Hashtable attributeImpls;
		
		private AttributeFactory factory;
		
		/// <summary> An AttributeSource using the default attribute factory {@link AttributeSource.AttributeFactory#DEFAULT_ATTRIBUTE_FACTORY}.</summary>
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
			this.factory = input.factory;
		}
		
		/// <summary> An AttributeSource using the supplied {@link AttributeFactory} for creating new {@link Attribute} instances.</summary>
		public AttributeSource(AttributeFactory factory)
		{
            this.attributes = new System.Collections.Hashtable();       // {{Aroush-2.9}} Port issue; in Java, this is java.util.LinkedHashMap
            this.attributeImpls = new System.Collections.Hashtable();   // {{Aroush-2.9}} Port issue; in Java, this is java.util.LinkedHashMap
			this.factory = factory;
		}
		
		/// <summary> returns the used AttributeFactory.</summary>
		public virtual AttributeFactory GetAttributeFactory()
		{
			return this.factory;
		}
		
		/// <summary>Returns a new iterator that iterates the attribute classes
		/// in the same order they were added in.
		/// <p>Signature for Java 1.5: <code>public Iterator&lt;Class&lt;? extends Attribute&gt;&gt; getAttributeClassesIterator()</code>
		/// </summary>
		public virtual System.Collections.IEnumerator GetAttributeClassesIterator()
		{
			return (new System.Collections.Hashtable(attributes)).GetEnumerator();
		}
		
		/// <summary>Returns a new iterator that iterates all unique Attribute implementations.
		/// This iterator may contain less entries that {@link #getAttributeClassesIterator},
		/// if one instance implements more than one Attribute interface.
		/// <p>Signature for Java 1.5: <code>public Iterator&lt;AttributeImpl&gt; getAttributeImplsIterator()</code>
		/// </summary>
		public virtual System.Collections.IEnumerator GetAttributeImplsIterator()
		{
			if (HasAttributes())
			{
				if (currentState == null)
				{
					ComputeCurrentState();
				}
				State initState = currentState;
				return new AnonymousClassIterator(initState, this);
			}
			else
			{
				return ((System.Collections.Hashtable) new System.Collections.Hashtable()).GetEnumerator();
			}
		}
		
		/// <summary>a cache that stores all interfaces for known implementation classes for performance (slow reflection) </summary>
		private static readonly System.Collections.Hashtable knownImplClasses = new System.Collections.Hashtable();

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
								foundInterfaces.Add(curInterface);
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
				System.Type curInterface = (System.Type) it.Current;
				// Attribute is a superclass of this interface
				if (!attributes.Contains(curInterface))
				{
					// invalidate state to force recomputation in captureState()
					this.currentState = null;
					attributes[curInterface] = att;
					attributeImpls[clazz] = att;
				}
			}
		}
		
		/// <summary> The caller must pass in a Class&lt;? extends Attribute&gt; value.
		/// This method first checks if an instance of that class is 
		/// already in this AttributeSource and returns it. Otherwise a
		/// new instance is created, added to this AttributeSource and returned. 
		/// <p>Signature for Java 1.5: <code>public &lt;T extends Attribute&gt; T addAttribute(Class&lt;T&gt;)</code>
		/// </summary>
		public virtual Attribute AddAttribute(System.Type attClass)
		{
			Attribute att = (Attribute) attributes[attClass];
			if (att == null)
			{
				AttributeImpl attImpl = this.factory.CreateAttributeInstance(attClass);
				AddAttributeImpl(attImpl);
				return attImpl;
			}
			else
			{
				return att;
			}
		}
		
		/// <summary>Returns true, iff this AttributeSource has any attributes </summary>
		public virtual bool HasAttributes()
		{
			return !(this.attributes.Count == 0);
		}
		
		/// <summary> The caller must pass in a Class&lt;? extends Attribute&gt; value. 
		/// Returns true, iff this AttributeSource contains the passed-in Attribute.
		/// <p>Signature for Java 1.5: <code>public boolean hasAttribute(Class&lt;? extends Attribute&gt;)</code>
		/// </summary>
		public virtual bool HasAttribute(System.Type attClass)
		{
			return this.attributes.Contains(attClass);
		}
		
		/// <summary> The caller must pass in a Class&lt;? extends Attribute&gt; value. 
		/// Returns the instance of the passed in Attribute contained in this AttributeSource
		/// <p>Signature for Java 1.5: <code>public &lt;T extends Attribute&gt; T getAttribute(Class&lt;T&gt;)</code>
		/// 
		/// </summary>
		/// <throws>  IllegalArgumentException if this AttributeSource does not contain the </throws>
		/// <summary>         Attribute. It is recommended to always use {@link #addAttribute} even in consumers
		/// of TokenStreams, because you cannot know if a specific TokenStream really uses
		/// a specific Attribute. {@link #addAttribute} will automatically make the attribute
		/// available. If you want to only use the attribute, if it is available (to optimize
		/// consuming), use {@link #hasAttribute}.
		/// </summary>
		public virtual Attribute GetAttribute(System.Type attClass)
		{
			Attribute att = (Attribute) this.attributes[attClass];
			if (att == null)
			{
				throw new System.ArgumentException("This AttributeSource does not have the attribute '" + attClass.FullName + "'.");
			}
			
			return att;
		}
		
		/// <summary> This class holds the state of an AttributeSource.</summary>
		/// <seealso cref="captureState">
		/// </seealso>
		/// <seealso cref="restoreState">
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
		
		private State currentState = null;
		
		private void  ComputeCurrentState()
		{
			currentState = new State();
			State c = currentState;
            System.Collections.IEnumerator it = attributeImpls.GetEnumerator();
            if (it.MoveNext())
			    c.attribute = (AttributeImpl) ((System.Collections.DictionaryEntry) it.Current).Value;
			while (it.MoveNext())
			{
				c.next = new State();
				c = c.next;
                c.attribute = (AttributeImpl) ((System.Collections.DictionaryEntry) it.Current).Value;
			}
		}
		
		/// <summary> Resets all Attributes in this AttributeSource by calling
		/// {@link AttributeImpl#Clear()} on each Attribute implementation.
		/// </summary>
		public virtual void  ClearAttributes()
		{
			if (HasAttributes())
			{
				if (currentState == null)
				{
					ComputeCurrentState();
				}
				for (State state = currentState; state != null; state = state.next)
				{
					state.attribute.Clear();
				}
			}
		}
		
		/// <summary> Captures the state of all Attributes. The return value can be passed to
		/// {@link #restoreState} to restore the state of this or another AttributeSource.
		/// </summary>
		public virtual State CaptureState()
		{
			if (!HasAttributes())
			{
				return null;
			}
			
			if (currentState == null)
			{
				ComputeCurrentState();
			}
			return (State) this.currentState.Clone();
		}
		
		/// <summary> Restores this state by copying the values of all attribute implementations
		/// that this state contains into the attributes implementations of the targetStream.
		/// The targetStream must contain a corresponding instance for each argument
		/// contained in this state (e.g. it is not possible to restore the state of
		/// an AttributeSource containing a TermAttribute into a AttributeSource using
		/// a Token instance as implementation).
		/// <p>
		/// Note that this method does not affect attributes of the targetStream
		/// that are not contained in this state. In other words, if for example
		/// the targetStream contains an OffsetAttribute, but this state doesn't, then
		/// the value of the OffsetAttribute remains unchanged. It might be desirable to
		/// reset its value to the default, in which case the caller should first
		/// call {@link TokenStream#ClearAttributes()} on the targetStream.   
		/// </summary>
		public virtual void  RestoreState(State state)
		{
			if (state == null)
				return ;
			
			do 
			{
				AttributeImpl targetImpl = (AttributeImpl) attributeImpls[state.attribute.GetType()];
				if (targetImpl == null)
					throw new System.ArgumentException("State contains an AttributeImpl that is not in this AttributeSource");
				state.attribute.CopyTo(targetImpl);
				state = state.next;
			}
			while (state != null);
		}
		
		public override int GetHashCode()
		{
			int code = 0;
			if (HasAttributes())
			{
				if (currentState == null)
				{
					ComputeCurrentState();
				}
				for (State state = currentState; state != null; state = state.next)
				{
					code = code * 31 + state.attribute.GetHashCode();
				}
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
					if (this.currentState == null)
					{
						this.ComputeCurrentState();
					}
					State thisState = this.currentState;
					if (other.currentState == null)
					{
						other.ComputeCurrentState();
					}
					State otherState = other.currentState;
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
				if (currentState == null)
				{
					ComputeCurrentState();
				}
				for (State state = currentState; state != null; state = state.next)
				{
					if (state != currentState)
						sb.Append(',');
					sb.Append(state.attribute.ToString());
				}
			}
			sb.Append(')');
			return sb.ToString();
		}
		
		/// <summary> Performs a clone of all {@link AttributeImpl} instances returned in a new
		/// AttributeSource instance. This method can be used to e.g. create another TokenStream
		/// with exactly the same attributes (using {@link #AttributeSource(AttributeSource)})
		/// </summary>
		public virtual AttributeSource CloneAttributes()
		{
			AttributeSource clone = new AttributeSource(this.factory);
			
			// first clone the impls
			if (HasAttributes())
			{
				if (currentState == null)
				{
					ComputeCurrentState();
				}
				for (State state = currentState; state != null; state = state.next)
				{
					clone.attributeImpls[state.attribute.GetType()] = state.attribute.Clone();
				}
			}
			
			// now the interfaces
			System.Collections.IEnumerator attIt = new System.Collections.Hashtable(this.attributes).GetEnumerator();
			while (attIt.MoveNext())
			{
				System.Collections.DictionaryEntry entry = (System.Collections.DictionaryEntry) attIt.Current;
				clone.attributes[entry.Key] = clone.attributeImpls[entry.Value.GetType()];
			}
			
			return clone;
		}
	}
}