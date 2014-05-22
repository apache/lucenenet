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
	/// Default implementation of <seealso cref="KeywordAttribute"/>. </summary>
	public sealed class KeywordAttributeImpl : AttributeImpl, KeywordAttribute
	{
	  private bool Keyword_Renamed;

	  /// <summary>
	  /// Initialize this attribute with the keyword value as false. </summary>
	  public KeywordAttributeImpl()
	  {
	  }

	  public override void Clear()
	  {
		Keyword_Renamed = false;
	  }

	  public override void CopyTo(AttributeImpl target)
	  {
		KeywordAttribute attr = (KeywordAttribute) target;
		attr.Keyword = Keyword_Renamed;
	  }

	  public override int HashCode()
	  {
		return Keyword_Renamed ? 31 : 37;
	  }

	  public override bool Equals(object obj)
	  {
		if (this == obj)
		{
		  return true;
		}
		if (this.GetType() != obj.GetType())
		{
		  return false;
		}
		KeywordAttributeImpl other = (KeywordAttributeImpl) obj;
		return Keyword_Renamed == other.Keyword_Renamed;
	  }

	  public override bool Keyword
	  {
		  get
		  {
			return Keyword_Renamed;
		  }
		  set
		  {
			Keyword_Renamed = value;
		  }
	  }


	}

}