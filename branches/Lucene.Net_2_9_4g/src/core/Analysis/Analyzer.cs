/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using Fieldable = Lucene.Net.Documents.Fieldable;
using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
using Lucene.Net.Util;
using System.Reflection;
using System.IO;

namespace Lucene.Net.Analysis
{
    // JAVA: src/java/org/apache/lucene/analysis/Analyzer.java
	
	/// <summary>
    ///     An <see cref="Analyzer"/> represents a policy for extracting terms that are 
    ///     indexed from text. The <see cref="Analyzer"/> builds <see cref="TokenStream"/>s, which 
    ///     breaks down text into tokens. 
	/// </summary>
    /// <remarks>
    ///     <para>
    ///         A typical <see cref="Analyzer"/> implementation will first build a <see cref="Tokenizer"/>.
    ///         The <see cref="Tokenizer"/> will break down the stream of characters from the 
    ///         <see cref="System.IO.TextReader"/> into raw <see cref="Token"/>s.  One or 
    ///         more <see cref="TokenFilter"/>s may then be applied to the output of the <see cref="Tokenizer"/>.
    ///     </para>
    /// </remarks>
    // REFACTOR: determine if this class should use IDisposable since it has a Close() method.
	public abstract class Analyzer : IDisposable
	{
        private CloseableThreadLocal<object> tokenStreams = new CloseableThreadLocal<object>();

        /// <summary>
        /// Gets or sets whether this class overrides the <see cref="TokenStream(String, TextReader)"/> method. 
        /// </summary>
        protected internal bool overridesTokenStreamMethod;

		/// <summary>
        /// Creates a <see cref="TokenStream"/> which tokenizes all the text in 
        /// the provided <see cref="TextReader"/>.
        /// </summary>
		/// <param name="fieldName">The name of the <see cref="Lucene.Net.Documents.Field"/>. the fieldName can be <c>null</c>.</param>
		/// <param name="reader">The text reader.</param>
		/// <returns>A <see cref="TokenStream"/>.</returns>
		public abstract TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader);
		
		/// <summary>
        ///     Creates a re-useable previously saved <see cref="TokenStream"/> inside the
        ///     same thread that called this method. Callers that do not need to use more
		///     than one TokenStream at the same time from this analyzer should use this 
        ///     method for better performance.
		/// </summary>
        /// <remarks>
        ///     <para>
        ///         This method defaults to invoking <see cref="TokenStream(String, TextReader)" />
        ///     </para>
        /// </remarks>
		public virtual TokenStream ReusableTokenStream(String fieldName, TextReader reader)
		{
			return TokenStream(fieldName, reader);
		}
		
		
		
		/// <summary>
        /// Gets the previous <see cref="TokenStream"/> used by Analyzers that implement (overrides) 
        /// <see cref="Analyzer.ReusableTokenStream(String, TextReader)"/> to retrieve a 
        /// previously saved <see cref="TokenStream"/> for re-use by the same thread. 
		/// </summary>
        /// <remarks>
        ///     <para>
        ///         This method uses a <see cref="CloseableThreadLocal{T}"/> to store the previous thread and retrieve it.
        ///     </para>
        /// </remarks>
        /// <exception cref="AlreadyClosedException">Throws when there is a null reference exception and the analyzer is closed.</exception>
        /// <exception cref="System.NullReferenceException">
        ///     Throws when there is a null reference to <see cref="CloseableThreadLocal{T}"/> and the
        ///     analyzer is still open.
        /// </exception>
        // REFACTOR: turn into a property.
		protected internal virtual System.Object GetPreviousTokenStream()
		{
			try
			{
				return tokenStreams.Get();
			}
			catch (System.NullReferenceException ex)
			{
                // GLOBALIZATION: get exception message from resource file.
				if (tokenStreams == null)
					throw new AlreadyClosedException("this Analyzer is closed", ex);

                // default to re-throw keep stack trace intact.
				throw;
				
			}
		}
		
		/// <summary>
        ///     Sets the <see cref="TokenStream"/> used by Analyzers that implement (overrides) 
        ///     <see cref="Analyzer.ReusableTokenStream(String, TextReader)"/>
        ///     to save a <see cref="TokenStream" /> for later re-use by the same thread. 
        /// </summary>
		/// <param name="obj">The previous <see cref="TokenStream"/>.</param>
		protected internal virtual void  SetPreviousTokenStream(System.Object obj)
		{
			try
			{
				tokenStreams.Set(obj);
			}
			catch (System.NullReferenceException ex)
			{
                // GLOBALIZATION: get exception message from resource file.
				if (tokenStreams == null)
					throw new AlreadyClosedException("this Analyzer is closed", ex);

                // default to re-throw keep stack trace intact.
                throw;
				
			}
		}
		
       
		
	    /// <summary>
        /// This is only present to preserve
        /// back-compat of classes that subclass a core analyzer
        /// and override tokenStream but not reusableTokenStream.
	    /// </summary>
	    /// <param name="baseClass">The base class type.</param>
        [Obsolete("This is only present to preserve backwards compatibility of classes that subclass a core analyzer and override tokenStream but not reusableTokenStream ")]
		protected internal virtual void  SetOverridesTokenStreamMethod(System.Type baseClass)
		{

            Type[] paramsRenamed = new Type[] { typeof(String), typeof(TextReader) };

			try
			{
                Type[] types = paramsRenamed ?? new Type[0];

				MethodInfo method = this.GetType().GetMethod("TokenStream", types);

                overridesTokenStreamMethod = (method != null && method.DeclaringType != baseClass);
			}
			catch
			{
				overridesTokenStreamMethod = false;
			}
		}
		
		
		/// <summary> 
        ///     Gets the position of the increment gap between two 
        ///     <see cref="Lucene.Net.Documents.Field"/>s using the same name. This 
        ///     is called before indexing a <see cref="Fieldable"/> instance if terms 
        ///     have already been added to that field. 
        /// </summary>
        /// <remarks>
        ///     <para>
        ///     Specifying the position of the increment gap allows custom
        ///     <see cref="Analyzer"/>s to place an automatic position increment gap between
        ///     <see cref="Fieldable"/> instances using the same field name. 
        ///     </para>
        ///     <para>
        ///         The default value position increment gap is 0.  
        ///     </para>
        ///     <para>
        ///         <b>Position Increment Gap</b> - The value that controls the 
        ///         virtual space between the last <see cref="Token"/> of one <see cref="Field"/> 
        ///         instance and the first <see cref="Token"/> of the next instance. 
        ///         Both fields share the same name. 
        ///     </para>
        ///     <para>
        ///         Suppose a document has a multi-valued "author" field. Like this:
        ///     </para>
        ///     <ul>
        ///         <li>author: John Doe</li>
        ///         <li>author: Bob Smith</li>
        ///     </ul>
        ///     <para>
        ///         With a position increment gap of 0, a phrase query of "doe bob" would
        ///         be a match.  With a gap of 100, a phrase query of "doe bob" would not
        ///         match.  The gap of 100 would prevent the phrase queries from matching
        ///         even with a modest slop factor. 
        ///     </para>
        ///     <note>
        ///         This explanation of the position increment gap was pulled from an entry by Erik Hatcher on the 
        ///         <a href="http://mail-archives.apache.org/mod_mbox/lucene-solr-user/200810.mbox/%3C045DC0D3-789D-433E-88B9-9252392BB1D6@ehatchersolutions.com%3E">
        ///         lucene-solr-user list</a>. 
        ///         This was a better explanation than the one found in the code comments from the Lucene-Solr project.
        ///     </note>
		/// </remarks>
		/// <param name="fieldName">The name of the field being indexed. </param>
		/// <returns> 
        ///     The position of the increment gap added to the next token emitted 
        ///     from <see cref="TokenStream(String,TextReader)" />
		/// </returns>
		public virtual int GetPositionIncrementGap(System.String fieldName)
		{
			return 0;
		}
		
		/// <summary> 
        ///     Gets the offset gap for a token in the specified field. By default this method
        ///     returns 1 for tokenized fields and 0 if the field is untokenized.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This method is similar to <see cref="GetPositionIncrementGap(String)"/>
        ///         and is only called if the field produced at least one token for indexing.
        ///     </para>
        /// </remarks>
		/// <param name="field">the field that was just analyzed </param>
		/// <returns> 
        ///     The offset gap, added to the next token emitted 
        ///     from <see cref="TokenStream(String,TextReader)" />.
		/// </returns>
		public virtual int GetOffsetGap(Fieldable field)
		{
			if (field.IsTokenized())
				return 1;
			else
				return 0;
		}
		
		/// <summary>   
        ///     Frees persistent resources used by the <see cref="Analyzer"/>.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         The default implementation closes the internal <see cref="TokenStream"/>s 
        ///         used by the analyzer.
        ///     </para>
        /// </remarks>
		public virtual void  Close()
		{
			if(tokenStreams!=null) tokenStreams.Close();
			tokenStreams = null;
		}

        public virtual void Dispose()
        {
            Close();
        }
	}
}