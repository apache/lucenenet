//-------------------------------------------------------------------------------------------
//	Copyright © 2007 - 2013 Tangible Software Solutions Inc.
//	this class can be used by anyone provided that the copyright notice remains intact.
//
//	this class is used to replace most calls to the Java String.split method.
//-------------------------------------------------------------------------------------------
internal static class StringHelperClass // LUCENENET TODO: Remove this class
{
	//------------------------------------------------------------------------------
	//	this method is used to replace most calls to the Java String.split method.
	//------------------------------------------------------------------------------
	internal static string[] Split(this string source, string regexDelimiter, bool trimTrailingEmptyStrings)
	{
		string[] splitArray = System.Text.RegularExpressions.Regex.Split(source, regexDelimiter);

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
}