using System;

namespace Lucene.Net.Analysis.Tokenattributes
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

    using AttributeImpl = Lucene.Net.Util.AttributeImpl;

	/// <summary>
	/// Default implementation of <seealso cref="TypeAttribute"/>. </summary>
	public class TypeAttributeImpl : AttributeImpl, TypeAttribute, ICloneable
	{
	  private string type;

	  /// <summary>
	  /// Initialize this attribute with <seealso cref="TypeAttribute#DEFAULT_TYPE"/> </summary>
	  public TypeAttributeImpl() : this(TypeAttribute_Fields.DEFAULT_TYPE)
	  {
	  }

	  /// <summary>
	  /// Initialize this attribute with <code>type</code> </summary>
	  public TypeAttributeImpl(string type)
	  {
		this.type = type;
	  }

	  public override string Type()
	  {
		return type;
	  }

	  public override string Type
	  {
		  set
		  {
			this.type = value;
		  }
	  }

	  public override void Clear()
	  {
		type = TypeAttribute_Fields.DEFAULT_TYPE;
	  }

	  public override bool Equals(object other)
	  {
		if (other == this)
		{
		  return true;
		}

		if (other is TypeAttributeImpl)
		{
		  TypeAttributeImpl o = (TypeAttributeImpl) other;
		  return (this.type == null ? o.type == null : this.type.Equals(o.type));
		}

		return false;
	  }

	  public override int HashCode()
	  {
		return (type == null) ? 0 : type.GetHashCode();
	  }

	  public override void CopyTo(AttributeImpl target)
	  {
		TypeAttribute t = (TypeAttribute) target;
		t.type = type;
	  }
	}

}