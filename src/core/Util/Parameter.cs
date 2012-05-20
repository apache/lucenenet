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

namespace Lucene.Net.Util
{
	
	/// <summary> A serializable Enum class.</summary>
	[Serializable]
    public abstract class Parameter : IEquatable<Parameter>
	{
        internal static Dictionary<string, Parameter> allParameters = new Dictionary<string, Parameter>();
		
		private readonly string name;
		
		private Parameter()
		{
			// typesafe enum pattern, no public constructor
		}
		
		protected internal Parameter(string name)
		{
			// typesafe enum pattern, no public constructor
			this.name = name;
			string key = MakeKey(name);
			
			if (allParameters.ContainsKey(key))
				throw new System.ArgumentException("Parameter name " + key + " already used!");
			
			allParameters[key] = this;
		}
		
		private string MakeKey(string name)
		{
			return GetType() + " " + name;
		}
		
		public override string ToString()
		{
			return name;
		}

		/// <summary> Resolves the deserialized instance to the local reference for accurate
		/// equals() and == comparisons.
		/// 
		/// </summary>
		/// <returns> a reference to Parameter as resolved in the local VM
		/// </returns>
		/// <throws>  ObjectStreamException </throws>
        //protected internal virtual System.Object ReadResolve()
        //{
        //    System.Object par = allParameters[MakeKey(name)];
			
        //    if (par == null)
        //        throw new System.IO.IOException("Unknown parameter value: " + name);
			
        //    return par;
        //}

        public override bool Equals(object obj)
        {
		    if (ReferenceEquals(null, obj)) return false;
		    if (ReferenceEquals(this, obj)) return true;
		    if (obj.GetType() != GetType()) return false;
		    return Equals((Parameter) obj);
        }

	    public bool Equals(Parameter other)
	    {
	        return string.Equals(name, other.name);
	    }

	    public override int GetHashCode()
	    {
	        return (name != null ? name.GetHashCode() : 0);
	    }

	    public static bool operator ==(Parameter left, Parameter right)
	    {
	        return Equals(left, right);
	    }

	    public static bool operator !=(Parameter left, Parameter right)
	    {
	        return !Equals(left, right);
	    }
	}
}