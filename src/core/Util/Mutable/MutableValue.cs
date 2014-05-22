using System;

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
namespace Lucene.Net.Util.Mutable
{

	/// <summary>
	/// Base class for all mutable values.
	///  
	/// @lucene.internal 
	/// </summary>
	public abstract class MutableValue : IComparable<MutableValue>
	{
	  public bool Exists_Renamed = true;

	  public abstract void Copy(MutableValue source);
	  public abstract MutableValue Duplicate();
	  public abstract bool EqualsSameType(object other);
	  public abstract int CompareSameType(object other);
	  public abstract object ToObject();

	  public virtual bool Exists()
	  {
		return Exists_Renamed;
	  }

	  public virtual int CompareTo(MutableValue other)
	  {
		Type c1 = this.GetType();
		Type c2 = other.GetType();
		if (c1 != c2)
		{
		  int c = c1.HashCode() - c2.HashCode();
		  if (c == 0)
		  {
			c = c1.CanonicalName.compareTo(c2.CanonicalName);
		  }
		  return c;
		}
		return CompareSameType(other);
	  }

	  public override bool Equals(object other)
	  {
		return (this.GetType() == other.GetType()) && this.EqualsSameType(other);
	  }

	  public override abstract int HashCode();

	  public override string ToString()
	  {
		return Exists() ? ToObject().ToString() : "(null)";
	  }
	}



}