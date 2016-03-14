using System;
using System.Collections.Generic;
using System.Reflection;
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
	/// Base class for Attributes that can be added to a 
	/// <seealso cref="Lucene.Net.Util.AttributeSource"/>.
	/// <p>
	/// Attributes are used to add data in a dynamic, yet type-safe way to a source
	/// of usually streamed objects, e. g. a <seealso cref="Lucene.Net.Analysis.TokenStream"/>.
	/// </summary>
	public abstract class AttributeImpl : IAttribute 
	{
	  /// <summary>
	  /// Clears the values in this AttributeImpl and resets it to its 
	  /// default value. If this implementation implements more than one Attribute interface
	  /// it clears all.
	  /// </summary>
	  public abstract void Clear();

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

	  private class AttributeReflectorAnonymousInnerClassHelper : IAttributeReflector
	  {
		  private readonly AttributeImpl OuterInstance;

		  private bool PrependAttClass;
		  private StringBuilder Buffer;

		  public AttributeReflectorAnonymousInnerClassHelper(AttributeImpl outerInstance, bool prependAttClass, StringBuilder buffer)
		  {
			  this.OuterInstance = outerInstance;
			  this.PrependAttClass = prependAttClass;
			  this.Buffer = buffer;
		  }

	      public void Reflect<T>(string key, object value) where T : IAttribute
	      {
	          throw new NotImplementedException();
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
	  /// add the key/values this attribute holds to the given <seealso cref="AttributeReflector"/>.
	  /// 
	  /// <p>The default implementation calls <seealso cref="AttributeReflector#reflect"/> for all
	  /// non-static fields from the implementing class, using the field name as key
	  /// and the field value as value. The Attribute class is also determined by reflection.
	  /// Please note that the default implementation can only handle single-Attribute
	  /// implementations.
	  /// 
	  /// <p>Custom implementations look like this (e.g. for a combined attribute implementation):
	  /// <pre class="prettyprint">
	  ///   public void reflectWith(AttributeReflector reflector) {
	  ///     reflector.reflect(CharTermAttribute.class, "term", term());
	  ///     reflector.reflect(PositionIncrementAttribute.class, "positionIncrement", getPositionIncrement());
	  ///   }
	  /// </pre>
	  /// 
	  /// <p>If you implement this method, make sure that for each invocation, the same set of <seealso cref="Attribute"/>
	  /// interfaces and keys are passed to <seealso cref="AttributeReflector#reflect"/> in the same order, but possibly
	  /// different values. So don't automatically exclude e.g. {@code null} properties!
	  /// </summary>
	  /// <seealso cref= #reflectAsString(boolean) </seealso>
	  public virtual void ReflectWith(IAttributeReflector reflector)
	  {
		Type clazz = this.GetType();
		LinkedList<WeakReference> interfaces = AttributeSource.GetAttributeInterfaces(clazz);
		if (interfaces.Count != 1)
		{
		  throw new System.NotSupportedException(clazz.Name + " implements more than one Attribute interface, the default reflectWith() implementation cannot handle this.");
		}
        //LUCENE-TODO unsure about GetType()
        Type interf = (Type)interfaces.First.Value.GetType();
        FieldInfo[] fields = clazz.GetFields(BindingFlags.Instance | BindingFlags.Public);
		try
		{
		  for (int i = 0; i < fields.Length; i++)
		  {
			FieldInfo f = fields[i];
			if (f.IsStatic)
			{
				continue;
			}
			reflector.Reflect(interf, f.Name, f.GetValue(this));
		  }
		}
		catch (Exception)
		{
		  // this should never happen, because we're just accessing fields
		  // from 'this'
		  throw new Exception("Unknown Error");
		}
	  }

	  /// <summary>
	  /// Copies the values from this Attribute into the passed-in
	  /// target attribute. The target implementation must support all the
	  /// Attributes this implementation supports.
	  /// </summary>
	  public abstract void CopyTo(AttributeImpl target);

	  /// <summary>
	  /// Shallow clone. Subclasses must override this if they 
	  /// need to clone any members deeply,
	  /// </summary>
	  public object Clone()
	  {
		AttributeImpl clone = null;
		try
		{
		  clone = (AttributeImpl)base.MemberwiseClone();
		}
		catch (Exception)
		{
		  throw new Exception("Clone not supported"); // shouldn't happen
		}
		return clone;
	  }
	}

}