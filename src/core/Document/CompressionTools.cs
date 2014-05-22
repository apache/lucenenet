namespace Lucene.Net.Document
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


	using BytesRef = Lucene.Net.Util.BytesRef;
	using CharsRef = Lucene.Net.Util.CharsRef;
	using UnicodeUtil = Lucene.Net.Util.UnicodeUtil;

	/// <summary>
	/// Simple utility class providing static methods to
	///  compress and decompress binary data for stored fields.
	///  this class uses java.util.zip.Deflater and Inflater
	///  classes to compress and decompress.
	/// </summary>

	public class CompressionTools
	{

	  // Export only static methods
	  private CompressionTools()
	  {
	  }

	  /// <summary>
	  /// Compresses the specified byte range using the
	  ///  specified compressionLevel (constants are defined in
	  ///  java.util.zip.Deflater). 
	  /// </summary>
	  public static sbyte[] Compress(sbyte[] value, int offset, int length, int compressionLevel)
	  {

		/* Create an expandable byte array to hold the compressed data.
		 * You cannot use an array that's the same size as the orginal because
		 * there is no guarantee that the compressed data will be smaller than
		 * the uncompressed data. */
		ByteArrayOutputStream bos = new ByteArrayOutputStream(length);

		Deflater compressor = new Deflater();

		try
		{
		  compressor.Level = compressionLevel;
		  compressor.setInput(value, offset, length);
		  compressor.finish();

		  // Compress the data
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] buf = new byte[1024];
		  sbyte[] buf = new sbyte[1024];
		  while (!compressor.finished())
		  {
			int count = compressor.deflate(buf);
			bos.write(buf, 0, count);
		  }
		}
		finally
		{
		  compressor.end();
		}

		return bos.toByteArray();
	  }

	  /// <summary>
	  /// Compresses the specified byte range, with default BEST_COMPRESSION level </summary>
	  public static sbyte[] Compress(sbyte[] value, int offset, int length)
	  {
		return Compress(value, offset, length, Deflater.BEST_COMPRESSION);
	  }

	  /// <summary>
	  /// Compresses all bytes in the array, with default BEST_COMPRESSION level </summary>
	  public static sbyte[] Compress(sbyte[] value)
	  {
		return Compress(value, 0, value.Length, Deflater.BEST_COMPRESSION);
	  }

	  /// <summary>
	  /// Compresses the String value, with default BEST_COMPRESSION level </summary>
	  public static sbyte[] CompressString(string value)
	  {
		return CompressString(value, Deflater.BEST_COMPRESSION);
	  }

	  /// <summary>
	  /// Compresses the String value using the specified
	  ///  compressionLevel (constants are defined in
	  ///  java.util.zip.Deflater). 
	  /// </summary>
	  public static sbyte[] CompressString(string value, int compressionLevel)
	  {
		BytesRef result = new BytesRef();
		UnicodeUtil.UTF16toUTF8(value, 0, value.Length, result);
		return Compress(result.Bytes, 0, result.Length, compressionLevel);
	  }

	  /// <summary>
	  /// Decompress the byte array previously returned by
	  ///  compress (referenced by the provided BytesRef) 
	  /// </summary>
	  public static sbyte[] Decompress(BytesRef bytes)
	  {
		return Decompress(bytes.Bytes, bytes.Offset, bytes.Length);
	  }

	  /// <summary>
	  /// Decompress the byte array previously returned by
	  ///  compress 
	  /// </summary>
	  public static sbyte[] Decompress(sbyte[] value)
	  {
		return Decompress(value, 0, value.Length);
	  }

	  /// <summary>
	  /// Decompress the byte array previously returned by
	  ///  compress 
	  /// </summary>
	  public static sbyte[] Decompress(sbyte[] value, int offset, int length)
	  {
		// Create an expandable byte array to hold the decompressed data
		ByteArrayOutputStream bos = new ByteArrayOutputStream(length);

		Inflater decompressor = new Inflater();

		try
		{
		  decompressor.setInput(value, offset, length);

		  // Decompress the data
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] buf = new byte[1024];
		  sbyte[] buf = new sbyte[1024];
		  while (!decompressor.finished())
		  {
			int count = decompressor.inflate(buf);
			bos.write(buf, 0, count);
		  }
		}
		finally
		{
		  decompressor.end();
		}

		return bos.toByteArray();
	  }

	  /// <summary>
	  /// Decompress the byte array previously returned by
	  ///  compressString back into a String 
	  /// </summary>
	  public static string DecompressString(sbyte[] value)
	  {
		return DecompressString(value, 0, value.Length);
	  }

	  /// <summary>
	  /// Decompress the byte array previously returned by
	  ///  compressString back into a String 
	  /// </summary>
	  public static string DecompressString(sbyte[] value, int offset, int length)
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final byte[] bytes = decompress(value, offset, length);
		sbyte[] bytes = Decompress(value, offset, length);
		CharsRef result = new CharsRef(bytes.Length);
		UnicodeUtil.UTF8toUTF16(bytes, 0, bytes.Length, result);
		return new string(result.Chars, 0, result.Length_Renamed);
	  }

	  /// <summary>
	  /// Decompress the byte array (referenced by the provided BytesRef) 
	  ///  previously returned by compressString back into a String 
	  /// </summary>
	  public static string DecompressString(BytesRef bytes)
	  {
		return DecompressString(bytes.Bytes, bytes.Offset, bytes.Length);
	  }
	}

}