/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Messages
{
	/// <summary>Interface that exceptions should implement to support lazy loading of messages.
	/// 	</summary>
	/// <remarks>
	/// Interface that exceptions should implement to support lazy loading of messages.
	/// For Native Language Support (NLS), system of software internationalization.
	/// This Interface should be implemented by all exceptions that require
	/// translation
	/// </remarks>
	public interface NLSException
	{
		/// <returns>a instance of a class that implements the Message interface</returns>
		Message GetMessageObject();
	}
}
