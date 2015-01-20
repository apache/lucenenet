/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Globalization;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core
{
	/// <summary>
	/// <p>
	/// This exception should be thrown if something wrong happens when dealing with
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// s.
	/// </p>
	/// <p>
	/// It also supports NLS messages.
	/// </p>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Messages.Message">Org.Apache.Lucene.Queryparser.Flexible.Messages.Message
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS">Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Messages.NLSException">Org.Apache.Lucene.Queryparser.Flexible.Messages.NLSException
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</seealso>
	[System.Serializable]
	public class QueryNodeException : Exception, NLSException
	{
		protected internal Org.Apache.Lucene.Queryparser.Flexible.Messages.Message message
			 = new MessageImpl(QueryParserMessages.EMPTY_MESSAGE);

		public QueryNodeException(Org.Apache.Lucene.Queryparser.Flexible.Messages.Message
			 message) : base(message.GetKey())
		{
			this.message = message;
		}

		public QueryNodeException(Exception throwable) : base(throwable)
		{
		}

		public QueryNodeException(Org.Apache.Lucene.Queryparser.Flexible.Messages.Message
			 message, Exception throwable) : base(message.GetKey(), throwable)
		{
			this.message = message;
		}

		public virtual Org.Apache.Lucene.Queryparser.Flexible.Messages.Message GetMessageObject
			()
		{
			return this.message;
		}

		public override string Message
		{
			get
			{
				return GetLocalizedMessage();
			}
		}

		public override string GetLocalizedMessage()
		{
			return GetLocalizedMessage(CultureInfo.CurrentCulture);
		}

		public virtual string GetLocalizedMessage(CultureInfo locale)
		{
			return this.message.GetLocalizedMessage(locale);
		}

		public override string ToString()
		{
			return this.message.GetKey() + ": " + GetLocalizedMessage();
		}
	}
}
