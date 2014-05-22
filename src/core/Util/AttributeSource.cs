using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Lucene.Net.Support;

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

    using TokenStream = Lucene.Net.Analysis.TokenStream;
    using System.Runtime.CompilerServices;
    
    /// <summary>
	/// An AttributeSource contains a list of different <seealso cref="AttributeImpl"/>s,
	/// and methods to add and get them. There can only be a single instance
	/// of an attribute in the same AttributeSource instance. this is ensured
	/// by passing in the actual type of the Attribute (Class&lt;Attribute&gt;) to 
	/// the <seealso cref="#addAttribute(Class)"/>, which then checks if an instance of
	/// that type is already present. If yes, it returns the instance, otherwise
	/// it creates a new instance and returns it.
	/// </summary>
	public class AttributeSource
	{
	  /// <summary>
	  /// An AttributeFactory creates instances of <seealso cref="AttributeImpl"/>s.
	  /// </summary>
	  public abstract class AttributeFactory
	  {
		/// <summary>
		/// returns an <seealso cref="AttributeImpl"/> for the supplied <seealso cref="Attribute"/> interface class.
		/// </summary>
		public abstract AttributeImpl CreateAttributeInstance(Type attClass);

		/// <summary>
		/// this is the default factory that creates <seealso cref="AttributeImpl"/>s using the
		/// class name of the supplied <seealso cref="Attribute"/> interface class by appending <code>Impl</code> to it.
		/// </summary>
		public static readonly AttributeFactory DEFAULT_ATTRIBUTE_FACTORY = new DefaultAttributeFactory();

		private sealed class DefaultAttributeFactory : AttributeFactory
		{
            //LUCENE TO-DO
		  //internal static readonly WeakIdentityMap<Type, WeakReference> AttClassImplMap = new WeakIdentityMap<Type, WeakReference>();
		  internal static readonly WeakDictionary<Type, WeakReference> AttClassImplMap = new WeakDictionary<Type, WeakReference>();

		  internal DefaultAttributeFactory()
		  {
		  }

		  public override Attribute CreateAttributeInstance(Type attClass)
		  {
              try
              {
                  return (Attribute)System.Activator.CreateInstance(GetClassForInterface(attClass));
              }
              catch (Exception)
              {
                  throw new System.ArgumentException("Could not instantiate implementing class for " + attClass.Name);
              }
		  }

		  internal static Type GetClassForInterface(Type attClass)
		  {
			WeakReference @ref = AttClassImplMap[attClass];
			Type clazz = (@ref == null) ? null : @ref.GetType();
			if (clazz == null)
			{
			  // we have the slight chance that another thread may do the same, but who cares?
			  try
			  {
                  string name = attClass.FullName.Replace(attClass.Name, attClass.Name.Substring(1)) + ", " + attClass.Assembly.FullName;
				  AttClassImplMap.Add(attClass, new WeakReference(clazz = Type.GetType(name, true)));
			  }
			  catch (Exception)
			  {
				throw new System.ArgumentException("Could not find implementing class for " + attClass.Name);
			  }
			}
			return clazz;
		  }
		}
	  }

	  /// <summary>
	  /// this class holds the state of an AttributeSource. </summary>
	  /// <seealso cref= #captureState </seealso>
	  /// <seealso cref= #restoreState </seealso>
	  public sealed class State : ICloneable
	  {
		internal AttributeImpl Attribute;
		internal State Next;

		public override State Clone()
		{
		  State clone = new State();
		  clone.Attribute = Attribute.Clone();

		  if (Next != null)
		  {
			clone.Next = Next.Clone();
		  }

		  return clone;
		}
	  }

	  // These two maps must always be in sync!!!
	  // So they are private, final and read-only from the outside (read-only iterators)
	  private readonly IDictionary<Type, AttributeImpl> Attributes;
	  private readonly IDictionary<Type, AttributeImpl> AttributeImpls;
	  private readonly State[] CurrentState_Renamed;

	  private readonly AttributeFactory Factory;

	  /// <summary>
	  /// An AttributeSource using the default attribute factory <seealso cref="AttributeSource.AttributeFactory#DEFAULT_ATTRIBUTE_FACTORY"/>.
	  /// </summary>
	  public AttributeSource() 
          : this(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY)
	  {
	  }

	  /// <summary>
	  /// An AttributeSource that uses the same attributes as the supplied one.
	  /// </summary>
	  public AttributeSource(AttributeSource input)
	  {
		if (input == null)
		{
		  throw new System.ArgumentException("input AttributeSource must not be null");
		}
		this.Attributes = input.Attributes;
		this.AttributeImpls = input.AttributeImpls;
		this.CurrentState_Renamed = input.CurrentState_Renamed;
		this.Factory = input.Factory;
	  }

	  /// <summary>
	  /// An AttributeSource using the supplied <seealso cref="AttributeFactory"/> for creating new <seealso cref="Attribute"/> instances.
	  /// </summary>
	  public AttributeSource(AttributeFactory factory)
	  {
		this.Attributes = new HashMap<Type, AttributeImpl>();
        this.AttributeImpls = new HashMap<Type, AttributeImpl>();
		this.CurrentState_Renamed = new State[1];
		this.Factory = factory;
	  }

	  /// <summary>
	  /// returns the used AttributeFactory.
	  /// </summary>
	  public AttributeFactory attributeFactory
	  {
		  get
		  {
			return this.Factory;
		  }
	  }

      /*LUCENE TO-DO I don't think this is ever used
	  /// <summary>
	  /// Returns a new iterator that iterates the attribute classes
	  /// in the same order they were added in.
	  /// </summary>
	  public IEnumerator<Type> AttributeClassesIterator
	  {
		  get
		  {
			return Collections.unmodifiableSet(Attributes.Keys).GetEnumerator();
		  }
	  }
	  /// <summary>
	  /// Returns a new iterator that iterates all unique Attribute implementations.
	  /// this iterator may contain less entries that <seealso cref="#getAttributeClassesIterator"/>,
	  /// if one instance implements more than one Attribute interface.
	  /// </summary>
	  public IEnumerator<AttributeImpl> AttributeImplsIterator
	  {
		  get
		  {
			State initState = CurrentState;
			if (initState != null)
			{
			  return new IteratorAnonymousInnerClassHelper(this, initState);
			}
			else
			{
			  return Collections.emptySet<AttributeImpl>().GetEnumerator();
			}
		  }
	  }

	  private class IteratorAnonymousInnerClassHelper : IEnumerator<AttributeImpl>
	  {
		  private readonly AttributeSource OuterInstance;

		  private AttributeSource.State InitState;

		  public IteratorAnonymousInnerClassHelper(AttributeSource outerInstance, AttributeSource.State initState)
		  {
			  this.OuterInstance = outerInstance;
			  this.InitState = initState;
			  state = initState;
		  }

		  private State state;

		  public virtual void Remove()
		  {
			throw new System.NotSupportedException();
		  }

		  public virtual AttributeImpl Next()
		  {
			if (state == null)
			{
			  throw new Exception();
			}
			AttributeImpl att = state.Attribute;
			state = state.Next;
			return att;
		  }

		  public virtual bool HasNext()
		  {
			return state != null;
		  }
	  }
*/
	  /// <summary>
	  /// a cache that stores all interfaces for known implementation classes for performance (slow reflection) </summary>
      private static readonly WeakIdentityMap<Type, LinkedList<WeakReference>> KnownImplClasses = WeakIdentityMap<Type, LinkedList<WeakReference>>.NewHashMap(false);

	  internal static LinkedList<WeakReference> GetAttributeInterfaces(Type clazz)
	  {
		LinkedList<WeakReference> foundInterfaces = KnownImplClasses.Get(clazz);
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
			  if (curInterface != typeof(Attribute) && curInterface.IsSubclassOf(typeof(Attribute)))
			  {
				foundInterfaces.AddLast(new WeakReference(curInterface));
			  }
			}
			actClazz = actClazz.BaseType;
		  } while (actClazz != null);
		  KnownImplClasses.Put(clazz, foundInterfaces);
		}
		return foundInterfaces;
	  }

	  /// <summary>
	  /// <b>Expert:</b> Adds a custom AttributeImpl instance with one or more Attribute interfaces.
	  /// <p><font color="red"><b>Please note:</b> It is not guaranteed, that <code>att</code> is added to
	  /// the <code>AttributeSource</code>, because the provided attributes may already exist.
	  /// You should always retrieve the wanted attributes using <seealso cref="#getAttribute"/> after adding
	  /// with this method and cast to your class.
	  /// The recommended way to use custom implementations is using an <seealso cref="AttributeFactory"/>.
	  /// </font></p>
	  /// </summary>
	  public void AddAttributeImpl(AttributeImpl att)
	  {
		Type clazz = att.GetType();
		if (AttributeImpls.ContainsKey(clazz))
		{
			return;
		}
		LinkedList<WeakReference> foundInterfaces = GetAttributeInterfaces(clazz);

		// add all interfaces of this AttributeImpl to the maps
		foreach (WeakReference curInterfaceRef in foundInterfaces)
		{
		  Type curInterface = curInterfaceRef.GetType();
		  Debug.Assert(curInterface != null, "We have a strong reference on the class holding the interfaces, so they should never get evicted");
		  // Attribute is a superclass of this interface
		  if (!Attributes.ContainsKey(curInterface))
		  {
			// invalidate state to force recomputation in captureState()
			this.CurrentState_Renamed[0] = null;
			Attributes[curInterface] = att;
			AttributeImpls[clazz] = att;
		  }
		}
	  }

      public virtual T AddAttribute<T>() where T : Attribute 
      {
          var attClass = typeof(T);
          if (!Attributes.ContainsKey(attClass))
          {
               if (!(attClass.IsInterface &&  typeof(Attribute).IsAssignableFrom(attClass))) 
                {
                    throw new ArgumentException("AddAttribute() only accepts an interface that extends Attribute, but " + attClass.FullName + " does not fulfil this contract."
                    );
                }
                
                AddAttributeImpl(this.Factory.CreateAttributeInstance(attClass));
          }
          return (T)(Attribute)Attributes[attClass];
      }

	  /// <summary>
	  /// Returns true, iff this AttributeSource has any attributes </summary>
	  public bool HasAttributes()
	  {
		return this.Attributes.Count > 0;
	  }

	  /// <summary>
	  /// The caller must pass in a Class&lt;? extends Attribute&gt; value. 
	  /// Returns true, iff this AttributeSource contains the passed-in Attribute.
	  /// </summary>
	  public bool HasAttribute(Type attClass)
	  {
		return this.Attributes.ContainsKey(attClass);
	  }

	  /// <summary>
	  /// The caller must pass in a Class&lt;? extends Attribute&gt; value. 
	  /// Returns the instance of the passed in Attribute contained in this AttributeSource
	  /// </summary>
	  /// <exception cref="IllegalArgumentException"> if this AttributeSource does not contain the
	  ///         Attribute. It is recommended to always use <seealso cref="#addAttribute"/> even in consumers
	  ///         of TokenStreams, because you cannot know if a specific TokenStream really uses
	  ///         a specific Attribute. <seealso cref="#addAttribute"/> will automatically make the attribute
	  ///         available. If you want to only use the attribute, if it is available (to optimize
	  ///         consuming), use <seealso cref="#HasAttribute"/>. </exception>
	  public virtual T GetAttribute<T>() where T : Attribute
	  {
        var attClass = typeof(T);
        AttributeImpl attImpl = Attributes[attClass];
		if (attImpl == null)
		{
		  throw new System.ArgumentException("this AttributeSource does not have the attribute '" + attClass.Name + "'.");
		}
        return (T)(Attribute)this.Attributes[attClass];
	  }

	  private State CurrentState
	  {
		  get
		  {
			State s = CurrentState_Renamed[0];
			if (s != null || !HasAttributes())
			{
			  return s;
			}
			State c = s = CurrentState_Renamed[0] = new State();
			IEnumerator<AttributeImpl> it = AttributeImpls.Values.GetEnumerator();
            it.MoveNext();
            c.Attribute = it.Current;
			while (it.MoveNext())
			{
			  c.Next = new State();
			  c = c.Next;
			  c.Attribute = it.Current;
			}
			return s;
		  }
	  }

	  /// <summary>
	  /// Resets all Attributes in this AttributeSource by calling
	  /// <seealso cref="AttributeImpl#clear()"/> on each Attribute implementation.
	  /// </summary>
	  public void ClearAttributes()
	  {
		for (State state = CurrentState; state != null; state = state.Next)
		{
		  state.Attribute.Clear();
		}
	  }

	  /// <summary>
	  /// Captures the state of all Attributes. The return value can be passed to
	  /// <seealso cref="#restoreState"/> to restore the state of this or another AttributeSource.
	  /// </summary>
	  public State CaptureState()
	  {
		State state = this.CurrentState;
		return (state == null) ? null : state.Clone();
	  }

	  /// <summary>
	  /// Restores this state by copying the values of all attribute implementations
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
	  /// call <seealso cref="TokenStream#ClearAttributes()"/> on the targetStream.   
	  /// </summary>
	  public void RestoreState(State state)
	  {
		if (state == null)
		{
			return;
		}

		do
		{
		  AttributeImpl targetImpl = AttributeImpls[state.Attribute.GetType()];
		  if (targetImpl == null)
		  {
			throw new System.ArgumentException("State contains AttributeImpl of type " + state.Attribute.GetType().Name + " that is not in in this AttributeSource");
		  }
		  state.Attribute.CopyTo(targetImpl);
		  state = state.Next;
		} while (state != null);
	  }

	  public override int HashCode()
	  {
		int code = 0;
		for (State state = CurrentState; state != null; state = state.Next)
		{
		  code = code * 31 + state.Attribute.GetHashCode();
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
		  AttributeSource other = (AttributeSource) obj;

		  if (HasAttributes())
		  {
			if (!other.HasAttributes())
			{
			  return false;
			}

			if (this.AttributeImpls.Count != other.AttributeImpls.Count)
			{
			  return false;
			}

			// it is only equal if all attribute impls are the same in the same order
			State thisState = this.CurrentState;
			State otherState = other.CurrentState;
			while (thisState != null && otherState != null)
			{
			  if (otherState.Attribute.GetType() != thisState.Attribute.GetType() || !otherState.Attribute.Equals(thisState.Attribute))
			  {
				return false;
			  }
			  thisState = thisState.Next;
			  otherState = otherState.Next;
			}
			return true;
		  }
		  else
		  {
			return !other.HasAttributes();
		  }
		}
		else
		{
		  return false;
		}
	  }

	  /// <summary>
	  /// this method returns the current attribute values as a string in the following format
	  /// by calling the <seealso cref="#reflectWith(AttributeReflector)"/> method:
	  /// 
	  /// <ul>
	  /// <li><em>iff {@code prependAttClass=true}:</em> {@code "AttributeClass#key=value,AttributeClass#key=value"}
	  /// <li><em>iff {@code prependAttClass=false}:</em> {@code "key=value,key=value"}
	  /// </ul>
	  /// </summary>
	  /// <seealso cref= #reflectWith(AttributeReflector) </seealso>
	  public string ReflectAsString(bool prependAttClass)
	  {
		StringBuilder buffer = new StringBuilder();
		ReflectWith(new AttributeReflectorAnonymousInnerClassHelper(this, prependAttClass, buffer));
		return buffer.ToString();
	  }

	  private class AttributeReflectorAnonymousInnerClassHelper : AttributeReflector
	  {
		  private readonly AttributeSource OuterInstance;

		  private bool PrependAttClass;
		  private StringBuilder Buffer;

		  public AttributeReflectorAnonymousInnerClassHelper(AttributeSource outerInstance, bool prependAttClass, StringBuilder buffer)
		  {
			  this.OuterInstance = outerInstance;
			  this.PrependAttClass = prependAttClass;
			  this.Buffer = buffer;
		  }

		  public virtual void Reflect(Type attClass, string key, object value)
		  {
			if (Buffer.Length > 0)
			{
			  Buffer.Append(',');
			}
			if (PrependAttClass)
			{
			  Buffer.Append(attClass.Name).Append('#');
			}
			Buffer.Append(key).Append('=').Append((value == null) ? "null" : value);
		  }
	  }

	  /// <summary>
	  /// this method is for introspection of attributes, it should simply
	  /// add the key/values this AttributeSource holds to the given <seealso cref="AttributeReflector"/>.
	  /// 
	  /// <p>this method iterates over all Attribute implementations and calls the
	  /// corresponding <seealso cref="AttributeImpl#reflectWith"/> method.</p>
	  /// </summary>
	  /// <seealso cref= AttributeImpl#reflectWith </seealso>
	  public void ReflectWith(AttributeReflector reflector)
	  {
		for (State state = CurrentState; state != null; state = state.Next)
		{
		  state.Attribute.ReflectWith(reflector);
		}
	  }

	  /// <summary>
	  /// Performs a clone of all <seealso cref="AttributeImpl"/> instances returned in a new
	  /// {@code AttributeSource} instance. this method can be used to e.g. create another TokenStream
	  /// with exactly the same attributes (using <seealso cref="#AttributeSource(AttributeSource)"/>).
	  /// You can also use it as a (non-performant) replacement for <seealso cref="#captureState"/>, if you need to look
	  /// into / modify the captured state.
	  /// </summary>
	  public AttributeSource CloneAttributes()
	  {
		AttributeSource clone = new AttributeSource(this.Factory);

		if (HasAttributes())
		{
		  // first clone the impls
		  for (State state = CurrentState; state != null; state = state.Next)
		  {
			clone.AttributeImpls[state.Attribute.GetType()] = state.Attribute.Clone();
		  }

		  // now the interfaces
		  foreach (KeyValuePair<Type, AttributeImpl> entry in this.Attributes)
		  {
			clone.Attributes[entry.Key] = clone.AttributeImpls[entry.Value.GetType()];
		  }
		}

		return clone;
	  }

	  /// <summary>
	  /// Copies the contents of this {@code AttributeSource} to the given target {@code AttributeSource}.
	  /// The given instance has to provide all <seealso cref="Attribute"/>s this instance contains. 
	  /// The actual attribute implementations must be identical in both {@code AttributeSource} instances;
	  /// ideally both AttributeSource instances should use the same <seealso cref="AttributeFactory"/>.
	  /// You can use this method as a replacement for <seealso cref="#restoreState"/>, if you use
	  /// <seealso cref="#cloneAttributes"/> instead of <seealso cref="#captureState"/>.
	  /// </summary>
	  public void CopyTo(AttributeSource target)
	  {
		for (State state = CurrentState; state != null; state = state.Next)
		{
		  AttributeImpl targetImpl = target.AttributeImpls[state.Attribute.GetType()];
		  if (targetImpl == null)
		  {
			throw new System.ArgumentException("this AttributeSource contains AttributeImpl of type " + state.Attribute.GetType().Name + " that is not in the target");
		  }
		  state.Attribute.CopyTo(targetImpl);
		}
	  }

	  /// <summary>
	  /// Returns a string consisting of the class's simple name, the hex representation of the identity hash code,
	  /// and the current reflection of all attributes. </summary>
	  /// <seealso cref= #reflectAsString(boolean) </seealso>
	  public override string ToString()
	  {
		return this.GetType().Name + '@' + RuntimeHelpers.GetHashCode(this).ToString("x") + " " + ReflectAsString(false);
	  }
	}

}