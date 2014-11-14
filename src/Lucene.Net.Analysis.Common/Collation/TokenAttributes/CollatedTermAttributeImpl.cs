namespace org.apache.lucene.collation.tokenattributes
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

	using CharTermAttributeImpl = org.apache.lucene.analysis.tokenattributes.CharTermAttributeImpl;
	using BytesRef = org.apache.lucene.util.BytesRef;

	/// <summary>
	/// Extension of <seealso cref="CharTermAttributeImpl"/> that encodes the term
	/// text as a binary Unicode collation key instead of as UTF-8 bytes.
	/// </summary>
	public class CollatedTermAttributeImpl : CharTermAttributeImpl
	{
	  private readonly Collator collator;

	  /// <summary>
	  /// Create a new CollatedTermAttributeImpl </summary>
	  /// <param name="collator"> Collation key generator </param>
	  public CollatedTermAttributeImpl(Collator collator)
	  {
		// clone in case JRE doesn't properly sync,
		// or to reduce contention in case they do
		this.collator = (Collator) collator.clone();
	  }

	  public override void fillBytesRef()
	  {
		BytesRef bytes = BytesRef;
		bytes.bytes = collator.getCollationKey(ToString()).toByteArray();
		bytes.offset = 0;
		bytes.length = bytes.bytes.length;
	  }

	}

}