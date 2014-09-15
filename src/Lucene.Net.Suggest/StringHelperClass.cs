//-------------------------------------------------------------------------------------------
//	Copyright © 2007 - 2014 Tangible Software Solutions Inc.
//	This class can be used by anyone provided that the copyright notice remains intact.
//
//	This class is used to convert some aspects of the Java String class.
//-------------------------------------------------------------------------------------------
internal static class StringHelperClass
{
	//----------------------------------------------------------------------------------
	//	This method replaces the Java String.substring method when 'start' is a
	//	method call or calculated value to ensure that 'start' is obtained just once.
	//----------------------------------------------------------------------------------
	internal static string SubstringSpecial(this string self, int start, int end)
	{
		return self.Substring(start, end - start);
	}

	//------------------------------------------------------------------------------------
	//	This method is used to replace calls to the 2-arg Java String.startsWith method.
	//------------------------------------------------------------------------------------
	internal static bool StartsWith(this string self, string prefix, int toffset)
	{
		return self.IndexOf(prefix, toffset, System.StringComparison.Ordinal) == toffset;
	}

	//------------------------------------------------------------------------------
	//	This method is used to replace most calls to the Java String.split method.
	//------------------------------------------------------------------------------
	internal static string[] Split(this string self, string regexDelimiter, bool trimTrailingEmptyStrings)
	{
		string[] splitArray = System.Text.RegularExpressions.Regex.Split(self, regexDelimiter);

		if (trimTrailingEmptyStrings)
		{
			if (splitArray.Length > 1)
			{
				for (int i = splitArray.Length; i > 0; i--)
				{
					if (splitArray[i - 1].Length > 0)
					{
						if (i < splitArray.Length)
							System.Array.Resize(ref splitArray, i);

						break;
					}
				}
			}
		}

		return splitArray;
	}

	//-----------------------------------------------------------------------------
	//	These methods are used to replace calls to some Java String constructors.
	//-----------------------------------------------------------------------------
	internal static string NewString(sbyte[] bytes)
	{
		return NewString(bytes, 0, bytes.Length);
	}
	internal static string NewString(sbyte[] bytes, int index, int count)
	{
		return System.Text.Encoding.UTF8.GetString((byte[])(object)bytes, index, count);
	}
	internal static string NewString(sbyte[] bytes, string encoding)
	{
		return NewString(bytes, 0, bytes.Length, encoding);
	}
	internal static string NewString(sbyte[] bytes, int index, int count, string encoding)
	{
		return System.Text.Encoding.GetEncoding(encoding).GetString((byte[])(object)bytes, index, count);
	}

	//--------------------------------------------------------------------------------
	//	These methods are used to replace calls to the Java String.getBytes methods.
	//--------------------------------------------------------------------------------
	internal static sbyte[] GetBytes(this string self)
	{
		return GetSBytesForEncoding(System.Text.Encoding.UTF8, self);
	}
	internal static sbyte[] GetBytes(this string self, string encoding)
	{
		return GetSBytesForEncoding(System.Text.Encoding.GetEncoding(encoding), self);
	}
	private static sbyte[] GetSBytesForEncoding(System.Text.Encoding encoding, string s)
	{
		sbyte[] sbytes = new sbyte[encoding.GetByteCount(s)];
		encoding.GetBytes(s, 0, s.Length, (byte[])(object)sbytes, 0);
		return sbytes;
	}
}