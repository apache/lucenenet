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
using Attribute = Lucene.Net.Util.Attribute;

namespace Lucene.Net.Analysis.Tokenattributes
{
	
	/// <summary> A Token's lexical type. The Default value is "word". </summary>
	[Serializable]
	public class TypeAttribute:Attribute, ITypeAttribute, System.ICloneable
	{

		public const System.String DEFAULT_TYPE = "word";
		
		public TypeAttribute():this(DEFAULT_TYPE)
		{
		}
		
		public TypeAttribute(System.String type)
		{
			Type = type;
		}

	    /// <summary>Returns this Token's lexical type.  Defaults to "word". </summary>
        public virtual string Type { get; set; }

	    public override void  Clear()
		{
			Type = DEFAULT_TYPE;
		}
		
		public  override bool Equals(System.Object other)
		{
			if (other == this)
			{
				return true;
			}
			
			if (other is TypeAttribute)
			{
				return Type.Equals(((TypeAttribute) other).Type);
			}
			
			return false;
		}
		
		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}
		
		public override void  CopyTo(Attribute target)
		{
			ITypeAttribute t = (ITypeAttribute) target;
			t.Type = Type;
		}
		
		override public System.Object Clone()
		{
            TypeAttribute impl = new TypeAttribute();
            impl.Type = Type;
            return impl;
		}

        public override string ToString()
        {
            return "type=" + this.Type.ToString();
        }
	}
}