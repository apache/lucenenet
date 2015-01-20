/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Sharpen;

namespace Org.Apache.Lucene.Search.Highlight
{
	/// <summary>Exception thrown if TokenStream Tokens are incompatible with provided text
	/// 	</summary>
	[System.Serializable]
	public class InvalidTokenOffsetsException : Exception
	{
		public InvalidTokenOffsetsException(string message) : base(message)
		{
		}
	}
}
