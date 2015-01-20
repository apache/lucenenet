/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using System.Text;
using Lucene.Net.Queryparser.Flexible.Messages;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Messages
{
	/// <summary>Default implementation of Message interface.</summary>
	/// <remarks>
	/// Default implementation of Message interface.
	/// For Native Language Support (NLS), system of software internationalization.
	/// </remarks>
	public class MessageImpl : Message
	{
		private string key;

		private object[] arguments = new object[0];

		public MessageImpl(string key)
		{
			this.key = key;
		}

		public MessageImpl(string key, params object[] args) : this(key)
		{
			this.arguments = args;
		}

		public virtual object[] GetArguments()
		{
			return this.arguments;
		}

		public virtual string GetKey()
		{
			return this.key;
		}

		public virtual string GetLocalizedMessage()
		{
			return GetLocalizedMessage(CultureInfo.CurrentCulture);
		}

		public virtual string GetLocalizedMessage(CultureInfo locale)
		{
			return NLS.GetLocalizedMessage(GetKey(), locale, GetArguments());
		}

		public override string ToString()
		{
			object[] args = GetArguments();
			StringBuilder sb = new StringBuilder(GetKey());
			if (args != null)
			{
				for (int i = 0; i < args.Length; i++)
				{
					sb.Append(i == 0 ? " " : ", ").Append(args[i]);
				}
			}
			return sb.ToString();
		}
	}
}
