/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Xml
{
	/// <summary>
	/// Thrown when the xml queryparser encounters
	/// invalid syntax/configuration.
	/// </summary>
	/// <remarks>
	/// Thrown when the xml queryparser encounters
	/// invalid syntax/configuration.
	/// </remarks>
	[System.Serializable]
	public class ParserException : Exception
	{
		public ParserException() : base()
		{
		}

		public ParserException(string message) : base(message)
		{
		}

		public ParserException(string message, Exception cause) : base(message, cause)
		{
		}

		public ParserException(Exception cause) : base(cause)
		{
		}
	}
}
