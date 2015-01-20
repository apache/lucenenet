/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Config
{
	/// <summary>
	/// <p>
	/// This class is the base of
	/// <see cref="QueryConfigHandler">QueryConfigHandler</see>
	/// and
	/// <see cref="FieldConfig">FieldConfig</see>
	/// .
	/// It has operations to set, unset and get configuration values.
	/// </p>
	/// <p>
	/// Each configuration is is a key-&gt;value pair. The key should be an unique
	/// <see cref="ConfigurationKey{T}">ConfigurationKey&lt;T&gt;</see>
	/// instance and it also holds the value's type.
	/// </p>
	/// </summary>
	/// <seealso cref="ConfigurationKey{T}">ConfigurationKey&lt;T&gt;</seealso>
	public abstract class AbstractQueryConfig
	{
		private readonly Dictionary<ConfigurationKey<object>, object> configMap = new Dictionary
			<ConfigurationKey<object>, object>();

		public AbstractQueryConfig()
		{
		}

		// although this class is public, it can only be constructed from package
		/// <summary>Returns the value held by the given key.</summary>
		/// <remarks>Returns the value held by the given key.</remarks>
		/// <?></?>
		/// <param name="key">the key, cannot be <code>null</code></param>
		/// <returns>the value held by the given key</returns>
		public virtual T Get<T>(ConfigurationKey<T> key)
		{
			if (key == null)
			{
				throw new ArgumentException("key cannot be null!");
			}
			return (T)this.configMap.Get(key);
		}

		/// <summary>Returns true if there is a value set with the given key, otherwise false.
		/// 	</summary>
		/// <remarks>Returns true if there is a value set with the given key, otherwise false.
		/// 	</remarks>
		/// <?></?>
		/// <param name="key">the key, cannot be <code>null</code></param>
		/// <returns>true if there is a value set with the given key, otherwise false</returns>
		public virtual bool Has<T>(ConfigurationKey<T> key)
		{
			if (key == null)
			{
				throw new ArgumentException("key cannot be null!");
			}
			return this.configMap.ContainsKey(key);
		}

		/// <summary>Sets a key and its value.</summary>
		/// <remarks>Sets a key and its value.</remarks>
		/// <?></?>
		/// <param name="key">the key, cannot be <code>null</code></param>
		/// <param name="value">value to set</param>
		public virtual void Set<T>(ConfigurationKey<T> key, T value)
		{
			if (key == null)
			{
				throw new ArgumentException("key cannot be null!");
			}
			if (value == null)
			{
				Unset(key);
			}
			else
			{
				this.configMap.Put(key, value);
			}
		}

		/// <summary>Unsets the given key and its value.</summary>
		/// <remarks>Unsets the given key and its value.</remarks>
		/// <?></?>
		/// <param name="key">the key</param>
		/// <returns>true if the key and value was set and removed, otherwise false</returns>
		public virtual bool Unset<T>(ConfigurationKey<T> key)
		{
			if (key == null)
			{
				throw new ArgumentException("key cannot be null!");
			}
			return Sharpen.Collections.Remove(this.configMap, key) != null;
		}
	}
}
