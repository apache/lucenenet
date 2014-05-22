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
	/// Default implementation of <seealso cref="PositionLengthAttribute"/>. </summary>
	public class PositionLengthAttributeImpl : AttributeImpl, PositionLengthAttribute, ICloneable
	{
	  private int PositionLength_Renamed = 1;

	  /// <summary>
	  /// Initializes this attribute with position length of 1. </summary>
	  public PositionLengthAttributeImpl()
	  {
	  }

	  public override int PositionLength
	  {
		  set
		  {
			if (value < 1)
			{
			  throw new System.ArgumentException("Position length must be 1 or greater: got " + value);
			}
			this.PositionLength_Renamed = value;
		  }
		  get
		  {
			return PositionLength_Renamed;
		  }
	  }


	  public override void Clear()
	  {
		this.PositionLength_Renamed = 1;
	  }

	  public override bool Equals(object other)
	  {
		if (other == this)
		{
		  return true;
		}

		if (other is PositionLengthAttributeImpl)
		{
		  PositionLengthAttributeImpl _other = (PositionLengthAttributeImpl) other;
		  return PositionLength_Renamed == _other.PositionLength_Renamed;
		}

		return false;
	  }

	  public override int HashCode()
	  {
		return PositionLength_Renamed;
	  }

	  public override void CopyTo(AttributeImpl target)
	  {
		PositionLengthAttribute t = (PositionLengthAttribute) target;
		t.PositionLength = PositionLength_Renamed;
	  }
	}

}