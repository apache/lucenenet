/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Sharpen;
using Sharpen.Reflect;

namespace Org.Apache.Lucene.Queryparser.Flexible.Messages
{
	/// <summary>MessageBundles classes extend this class, to implement a bundle.</summary>
	/// <remarks>
	/// MessageBundles classes extend this class, to implement a bundle.
	/// For Native Language Support (NLS), system of software internationalization.
	/// This interface is similar to the NLS class in eclipse.osgi.util.NLS class -
	/// initializeMessages() method resets the values of all static strings, should
	/// only be called by classes that extend from NLS (see TestMessages.java for
	/// reference) - performs validation of all message in a bundle, at class load
	/// time - performs per message validation at runtime - see NLSTest.java for
	/// usage reference
	/// MessageBundle classes may subclass this type.
	/// </remarks>
	public class NLS
	{
		private static IDictionary<string, Type> bundles = new Dictionary<string, Type>(0
			);

		public NLS()
		{
		}

		// Do not instantiate
		public static string GetLocalizedMessage(string key)
		{
			return GetLocalizedMessage(key, CultureInfo.CurrentCulture);
		}

		public static string GetLocalizedMessage(string key, CultureInfo locale)
		{
			object message = GetResourceBundleObject(key, locale);
			if (message == null)
			{
				return "Message with key:" + key + " and locale: " + locale + " not found.";
			}
			return message.ToString();
		}

		public static string GetLocalizedMessage(string key, CultureInfo locale, params object
			[] args)
		{
			string str = GetLocalizedMessage(key, locale);
			if (args.Length > 0)
			{
				str = MessageFormat.Format(str, args);
			}
			return str;
		}

		public static string GetLocalizedMessage(string key, params object[] args)
		{
			return GetLocalizedMessage(key, CultureInfo.CurrentCulture, args);
		}

		/// <summary>
		/// Initialize a given class with the message bundle Keys Should be called from
		/// a class that extends NLS in a static block at class load time.
		/// </summary>
		/// <remarks>
		/// Initialize a given class with the message bundle Keys Should be called from
		/// a class that extends NLS in a static block at class load time.
		/// </remarks>
		/// <param name="bundleName">Property file with that contains the message bundle</param>
		/// <param name="clazz">where constants will reside</param>
		protected internal static void InitializeMessages<_T0>(string bundleName, Type<_T0
			> clazz) where _T0:Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS
		{
			try
			{
				Load(clazz);
				if (!bundles.ContainsKey(bundleName))
				{
					bundles.Put(bundleName, clazz);
				}
			}
			catch
			{
			}
		}

		// ignore all errors and exceptions
		// because this function is supposed to be called at class load time.
		private static object GetResourceBundleObject(string messageKey, CultureInfo locale
			)
		{
			// slow resource checking
			// need to loop thru all registered resource bundles
			for (Iterator<string> it = bundles.Keys.Iterator(); it.HasNext(); )
			{
				Type clazz = bundles.Get(it.Next());
				ResourceBundle resourceBundle = ResourceBundle.GetBundle(clazz.FullName, locale);
				if (resourceBundle != null)
				{
					try
					{
						object obj = resourceBundle.GetObject(messageKey);
						if (obj != null)
						{
							return obj;
						}
					}
					catch (MissingResourceException)
					{
					}
				}
			}
			// just continue it might be on the next resource bundle
			// if resource is not found
			return null;
		}

		private static void Load<_T0>(Type<_T0> clazz) where _T0:Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS
		{
			FieldInfo[] fieldArray = Sharpen.Runtime.GetDeclaredFields(clazz);
			bool isFieldAccessible = (clazz.GetModifiers() & Modifier.PUBLIC) != 0;
			// build a map of field names to Field objects
			int len = fieldArray.Length;
			IDictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(len * 2
				);
			for (int i = 0; i < len; i++)
			{
				fields.Put(fieldArray[i].Name, fieldArray[i]);
				LoadfieldValue(fieldArray[i], isFieldAccessible, clazz);
			}
		}

		private static void LoadfieldValue<_T0>(FieldInfo field, bool isFieldAccessible, 
			Type<_T0> clazz) where _T0:Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS
		{
			int MOD_EXPECTED = Modifier.PUBLIC | Modifier.STATIC;
			int MOD_MASK = MOD_EXPECTED | Modifier.FINAL;
			if ((field.GetModifiers() & MOD_MASK) != MOD_EXPECTED)
			{
				return;
			}
			// Set a value for this empty field.
			if (!isFieldAccessible)
			{
				MakeAccessible(field);
			}
			try
			{
				field.SetValue(null, field.Name);
				ValidateMessage(field.Name, clazz);
			}
			catch (ArgumentException)
			{
			}
			catch (MemberAccessException)
			{
			}
		}

		// should not happen
		// should not happen
		/// <param name="key">- Message Key</param>
		private static void ValidateMessage<_T0>(string key, Type<_T0> clazz) where _T0:Org.Apache.Lucene.Queryparser.Flexible.Messages.NLS
		{
			// Test if the message is present in the resource bundle
			try
			{
				ResourceBundle resourceBundle = ResourceBundle.GetBundle(clazz.FullName, CultureInfo
					.CurrentCulture);
				if (resourceBundle != null)
				{
					object obj = resourceBundle.GetObject(key);
				}
			}
			catch (MissingResourceException)
			{
			}
			catch
			{
			}
		}

		//if (obj == null)
		//  System.err.println("WARN: Message with key:" + key + " and locale: "
		//      + Locale.getDefault() + " not found.");
		//System.err.println("WARN: Message with key:" + key + " and locale: "
		//    + Locale.getDefault() + " not found.");
		// ignore all other errors and exceptions
		// since this code is just a test to see if the message is present on the
		// system
		private static void MakeAccessible(FieldInfo field)
		{
			if (Runtime.GetSecurityManager() == null)
			{
			}
			else
			{
				AccessController.DoPrivileged(new _PrivilegedAction_191(field));
			}
		}

		private sealed class _PrivilegedAction_191 : PrivilegedAction<Void>
		{
			public _PrivilegedAction_191(FieldInfo field)
			{
				this.field = field;
			}

			public Void Run()
			{
				return null;
			}

			private readonly FieldInfo field;
		}
	}
}
