namespace org.apache.lucene.analysis.payloads
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
	/// Utility methods for encoding payloads.
	/// 
	/// 
	/// </summary>
	public class PayloadHelper
	{

	  public static sbyte[] encodeFloat(float payload)
	  {
		return encodeFloat(payload, new sbyte[4], 0);
	  }

	  public static sbyte[] encodeFloat(float payload, sbyte[] data, int offset)
	  {
		return encodeInt(float.floatToIntBits(payload), data, offset);
	  }

	  public static sbyte[] encodeInt(int payload)
	  {
		return encodeInt(payload, new sbyte[4], 0);
	  }

	  public static sbyte[] encodeInt(int payload, sbyte[] data, int offset)
	  {
		data[offset] = (sbyte)(payload >> 24);
		data[offset + 1] = (sbyte)(payload >> 16);
		data[offset + 2] = (sbyte)(payload >> 8);
		data[offset + 3] = (sbyte) payload;
		return data;
	  }

	  /// <seealso cref= #decodeFloat(byte[], int) </seealso>
	  /// <seealso cref= #encodeFloat(float) </seealso>
	  /// <returns> the decoded float </returns>
	  public static float decodeFloat(sbyte[] bytes)
	  {
		return decodeFloat(bytes, 0);
	  }

	  /// <summary>
	  /// Decode the payload that was encoded using <seealso cref="#encodeFloat(float)"/>.
	  /// NOTE: the length of the array must be at least offset + 4 long. </summary>
	  /// <param name="bytes"> The bytes to decode </param>
	  /// <param name="offset"> The offset into the array. </param>
	  /// <returns> The float that was encoded
	  /// </returns>
	  /// <seealso cref= #encodeFloat(float) </seealso>
	  public static float decodeFloat(sbyte[] bytes, int offset)
	  {

		return float.intBitsToFloat(decodeInt(bytes, offset));
	  }

	  public static int decodeInt(sbyte[] bytes, int offset)
	  {
		return ((bytes[offset] & 0xFF) << 24) | ((bytes[offset + 1] & 0xFF) << 16) | ((bytes[offset + 2] & 0xFF) << 8) | (bytes[offset + 3] & 0xFF);
	  }
	}

}