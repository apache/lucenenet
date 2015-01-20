/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Lucene.Net.Queryparser.Flexible.Messages;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Messages
{
	/// <summary>Message Interface for a lazy loading.</summary>
	/// <remarks>
	/// Message Interface for a lazy loading.
	/// For Native Language Support (NLS), system of software internationalization.
	/// </remarks>
	public interface Message
	{
		string GetKey();

		object[] GetArguments();

		string GetLocalizedMessage();

		string GetLocalizedMessage(CultureInfo locale);
	}
}
