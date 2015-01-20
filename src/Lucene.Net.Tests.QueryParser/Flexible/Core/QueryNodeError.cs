/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core
{
	/// <summary>Error class with NLS support</summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS">Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Messages.Message">Org.Apache.Lucene.Queryparser.Flexible.Messages.Message
	/// 	</seealso>
	[System.Serializable]
	public class QueryNodeError : Error, NLSException
	{
		private Message message;

		/// <param name="message">- NLS Message Object</param>
		public QueryNodeError(Message message) : base(message.GetKey())
		{
			this.message = message;
		}

		/// <param name="throwable">- @see java.lang.Error</param>
		public QueryNodeError(Exception throwable) : base(throwable)
		{
		}

		/// <param name="message">- NLS Message Object</param>
		/// <param name="throwable">- @see java.lang.Error</param>
		public QueryNodeError(Message message, Exception throwable) : base(message.GetKey
			(), throwable)
		{
			this.message = message;
		}

		public virtual Message GetMessageObject()
		{
			return this.message;
		}
	}
}
