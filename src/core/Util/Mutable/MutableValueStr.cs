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
	/// <seealso cref="MutableValue"/> implementation of type 
	/// <seealso cref="String"/>.
	/// </summary>
	public class MutableValueStr : MutableValue
	{
	  public BytesRef Value = new BytesRef();

	  public override object ToObject()
	  {
		return Exists_Renamed ? Value.Utf8ToString() : null;
	  }

	  public override void Copy(MutableValue source)
	  {
		MutableValueStr s = (MutableValueStr) source;
		Exists_Renamed = s.Exists_Renamed;
		Value.CopyBytes(s.Value);
	  }

	  public override MutableValue Duplicate()
	  {
		MutableValueStr v = new MutableValueStr();
		v.Value.CopyBytes(Value);
		v.Exists_Renamed = this.Exists_Renamed;
		return v;
	  }

	  public override bool EqualsSameType(object other)
	  {
		MutableValueStr b = (MutableValueStr)other;
		return Value.Equals(b.Value) && Exists_Renamed == b.Exists_Renamed;
	  }

	  public override int CompareSameType(object other)
	  {
		MutableValueStr b = (MutableValueStr)other;
		int c = Value.CompareTo(b.Value);
		if (c != 0)
		{
			return c;
		}
		if (Exists_Renamed == b.Exists_Renamed)
		{
			return 0;
		}
		return Exists_Renamed ? 1 : -1;
	  }


	  public override int HashCode()
	  {
		return Value.HashCode();
	  }
	}

}