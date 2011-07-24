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

using FlagsAttribute = Lucene.Net.Analysis.Tokenattributes.FlagsAttribute;
using OffsetAttribute = Lucene.Net.Analysis.Tokenattributes.OffsetAttribute;
using PayloadAttribute = Lucene.Net.Analysis.Tokenattributes.PayloadAttribute;
using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
using TermAttribute = Lucene.Net.Analysis.Tokenattributes.TermAttribute;
using TypeAttribute = Lucene.Net.Analysis.Tokenattributes.TypeAttribute;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Attribute = Lucene.Net.Util.IAttribute;
using AttributeImpl = Lucene.Net.Util.AttributeImpl;
using AttributeSource = Lucene.Net.Util.AttributeSource;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis
{
    // JAVA: src/java/org/apache/lucene/analysis/TokenStream.java
	
	/// <summary> 
    ///     A <see cref="Lucene.Net.Analysis.TokenStream"/> enumerates the sequence of tokens, either from
    ///     <see cref="Lucene.Net.Documents.Field"/>s of a <see cref="Lucene.Net.Documents.Document"/> 
    ///     or from query text.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A new <see cref="Lucene.Net.Analysis.TokenStream"/>  API has been introduced with Lucene 2.9. This API
    ///         has moved from being <see cref="Lucene.Net.Analysis.Token"/> based to <see cref="Lucene.Net.Util.IAttribute" /> based. While
    ///         <see cref="Lucene.Net.Analysis.Token"/> still exists in 2.9 as a convenience class, the preferred way
    ///         to store the information of a <see cref="Lucene.Net.Analysis.Token"/> is to use <see cref="Lucene.Net.Util.AttributeImpl" />s.
    ///     </para>
	///     <para>
    ///         <c>TokenStream</c> now extends <see cref="Lucene.Net.Util.AttributeSource" />, which provides
    ///         access to all of the token <see cref="Lucene.Net.Util.IAttribute"/>s for the <c>TokenStream</c>.
    ///         Note that only one instance per <see cref="Lucene.Net.Util.AttributeImpl" /> is created and reused
	///         for every token. This approach reduces object creation and allows local
    ///         caching of references to the <see cref="Lucene.Net.Util.AttributeImpl" />s. See
	///         <see cref="IncrementToken"/> for further details.
    ///     </para>
	///     <para>
    ///         <b>The workflow of the new <c>TokenStream</c> API is as follows:</b>
    ///     </para>
	///     <ol>
	///         <li>
    ///             Instantiation of <see cref="TokenStream" /> / <see cref="TokenFilter"/>s which add/get
	///             attributes to/from the <see cref="Lucene.Net.Util.AttributeSource"/>.
    ///         </li>
	///         <li>
    ///             The consumer calls <see cref="Reset()"/>.
    ///         </li>
	///         <li>
    ///             The consumer retrieves attributes from the stream and stores local
	///             references to all attributes it wants to access.
    ///         </li>
	///         <li>
    ///             The consumer calls <see cref="IncrementToken()"/> until it returns false and
	///             consumes the attributes after each call.
    ///         </li>
	///         <li>
    ///             The consumer calls <see cref="End()"/> so that any end-of-stream operations
	///             can be performed.
    ///         </li>
	///         <li>
    ///             The consumer calls <see cref="Close()"/> to release any resource when finished
	///             using the <c>TokenStream</c>
    ///         </li>
	///     </ol>
    /// </remarks>
	public abstract class TokenStream : AttributeSource
	{
        // REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
        private static readonly AttributeFactory DEFAULT_TOKEN_WRAPPER_ATTRIBUTE_FACTORY = new TokenWrapperAttributeFactory(AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY);

        // REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
        private TokenWrapper tokenWrapper;


        // REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
        private static bool onlyUseNewAPI = false;

        // REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
        private MethodSupport supportedMethods;

		private void  InitBlock()
        {
            // REMOVE: in 3.0
            #pragma warning disable 618
            supportedMethods = GetSupportedMethods(this.GetType());
            #pragma warning restore 618
		}
		
		
		
		// REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
		private sealed class MethodSupport
		{
			internal bool hasIncrementToken;
			internal bool hasReusableNext;
			internal bool hasNext;
			
			internal MethodSupport(System.Type clazz)
			{
				hasIncrementToken = IsMethodOverridden(clazz, "IncrementToken", METHOD_NO_PARAMS);
				hasReusableNext = IsMethodOverridden(clazz, "Next", METHOD_TOKEN_PARAM);
				hasNext = IsMethodOverridden(clazz, "Next", METHOD_NO_PARAMS);
			}
			
			private static bool IsMethodOverridden(System.Type clazz, System.String name, System.Type[] params_Renamed)
			{
				try
				{
					return clazz.GetMethod(name, params_Renamed).DeclaringType != typeof(TokenStream);
				}
				catch (System.MethodAccessException e)
				{
					// should not happen
					throw new System.SystemException(e.Message, e);
				}
			}
			
			private static readonly System.Type[] METHOD_NO_PARAMS = new System.Type[0];
			private static readonly System.Type[] METHOD_TOKEN_PARAM = new System.Type[]{typeof(Token)};
		}

        // REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
        private static readonly Support.Dictionary<Type, MethodSupport> knownMethodSupport = new Support.Dictionary<Type, MethodSupport>();

        // {{Aroush-2.9 Port issue, need to mimic java's IdentityHashMap
        /*
         * From Java docs:
         * This class implements the Map interface with a hash table, using 
         * reference-equality in place of object-equality when comparing keys 
         * (and values). In other words, in an IdentityHashMap, two keys k1 and k2 
         * are considered equal if and only if (k1==k2). (In normal Map 
         * implementations (like HashMap) two keys k1 and k2 are considered 
         * equal if and only if (k1==null ? k2==null : k1.equals(k2)).) 
         */
        // Aroush-2.9}}

        // REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
		private static MethodSupport GetSupportedMethods(System.Type clazz)
		{
			MethodSupport supportedMethods;
			lock (knownMethodSupport)
			{
				supportedMethods = knownMethodSupport[clazz];
				if (supportedMethods == null)
				{
					knownMethodSupport.Add(clazz, supportedMethods = new MethodSupport(clazz));
				}
			}
			return supportedMethods;
		}

        // REMOVE: in 3.0
        [Obsolete("Remove this when old API is removed! ")]
		private sealed class TokenWrapperAttributeFactory:AttributeFactory
		{
			private AttributeFactory delegate_Renamed;
			
			internal TokenWrapperAttributeFactory(AttributeFactory delegate_Renamed)
			{
				this.delegate_Renamed = delegate_Renamed;
			}
			
			public override AttributeImpl CreateAttributeInstance(System.Type attClass)
			{
				return attClass.IsAssignableFrom(typeof(TokenWrapper))?new TokenWrapper():delegate_Renamed.CreateAttributeInstance(attClass);
			}
			
			// this is needed for TeeSinkTokenStream's check for compatibility of AttributeSource,
			// so two TokenStreams using old API have the same AttributeFactory wrapped by this one.
			public  override bool Equals(System.Object other)
			{
				if (this == other)
					return true;
				if (other is TokenWrapperAttributeFactory)
				{
					TokenWrapperAttributeFactory af = (TokenWrapperAttributeFactory) other;
					return this.delegate_Renamed.Equals(af.delegate_Renamed);
				}
				return false;
			}
			
			public override int GetHashCode()
			{
				return delegate_Renamed.GetHashCode() ^ 0x0a45ff31;
			}
		}

        /// <summary> A <see cref="TokenStream"/> using the default attribute factory.</summary>
        #pragma warning disable 618
        protected internal TokenStream() : 
            base( onlyUseNewAPI ? AttributeFactory.DEFAULT_ATTRIBUTE_FACTORY : TokenStream.DEFAULT_TOKEN_WRAPPER_ATTRIBUTE_FACTORY)
		{
			InitBlock();
			tokenWrapper = InitTokenWrapper(null);
			Check();
		}
        #pragma warning restore 618

        /// <summary> A <see cref="TokenStream"/> that uses the same attributes as the supplied one.</summary>
		protected internal TokenStream(AttributeSource input):base(input)
		{
			InitBlock();
            
            // REMOVE: in 3.0
            #pragma warning disable 618
            tokenWrapper = InitTokenWrapper(input);
			Check();
            #pragma warning restore 618
        }

        /// <summary> 
        ///     A <see cref="TokenStream"/> using the supplied AttributeFactory for creating 
        ///     new <see cref="IAttribute"/> instances.
        /// </summary>
        #pragma warning disable 618
        protected internal TokenStream(AttributeFactory factory)
            :base( onlyUseNewAPI? factory: new TokenWrapperAttributeFactory(factory))
		{
			InitBlock();

            // REMOVE: in 3.0
           
            tokenWrapper = InitTokenWrapper(null);
			Check();
            #pragma warning restore 618
        }
		
        [Obsolete("Remove this when old API is removed! ")]
		private TokenWrapper InitTokenWrapper(AttributeSource input)
		{
			if (onlyUseNewAPI)
			{
				// no wrapper needed
				return null;
			}
			else
			{
				// if possible get the wrapper from the filter's input stream
				if (input is TokenStream && ((TokenStream) input).tokenWrapper != null)
				{
					return ((TokenStream) input).tokenWrapper;
				}
				// check that all attributes are implemented by the same TokenWrapper instance
				IAttribute att = AddAttribute(typeof(TermAttribute));
				if (att is TokenWrapper && AddAttribute(typeof(TypeAttribute)) == att && AddAttribute(typeof(PositionIncrementAttribute)) == att && AddAttribute(typeof(FlagsAttribute)) == att && AddAttribute(typeof(OffsetAttribute)) == att && AddAttribute(typeof(PayloadAttribute)) == att)
				{
					return (TokenWrapper) att;
				}
				else
				{
					throw new System.NotSupportedException("If onlyUseNewAPI is disabled, all basic Attributes must be implemented by the internal class " + "TokenWrapper. Please make sure, that all TokenStreams/TokenFilters in this chain have been " + "instantiated with this flag disabled and do not add any custom instances for the basic Attributes!");
				}
			}
		}
		
	
        [Obsolete("Remove this when old API is removed! ")]
		private void  Check()
		{
			if (onlyUseNewAPI && !supportedMethods.hasIncrementToken)
			{
				throw new System.NotSupportedException(GetType().FullName + " does not implement incrementToken() which is needed for onlyUseNewAPI.");
			}
			
			// a TokenStream subclass must at least implement one of the methods!
			if (!(supportedMethods.hasIncrementToken || supportedMethods.hasNext || supportedMethods.hasReusableNext))
			{
				throw new System.NotSupportedException(GetType().FullName + " does not implement any of incrementToken(), next(Token), next().");
			}
		}
		
		/// <summary> 
        ///     <para>
        ///         For extra performance you can globally enable the new
		///         <see cref="IncrementToken()"/> API using <see cref="IAttribute"/>s. There will be a
		///         small, but in most cases negligible performance increase by enabling this,
		///         but it only works if <b>all</b> <c>TokenStream</c>s use the new API and
		///         implement <see cref="IncrementToken()"/>. This setting can only be enabled
		///         globally.
        ///     </para>
        /// </summary>
        /// <remarks>
		///     <para>
        ///         This setting only affects <see cref="TokenStream"/>s instantiated after this
		///         call. All <c>TokenStream</c>s already created use the other setting.
        ///     </para>
        ///     <para>
        ///         All core <see cref="Lucene.Net.Analysis.Analyzer"/>s are compatible with this setting, if you have
		///         your own <c>TokenStream</c>s that are also compatible, you should enable
		///         this.
        ///     </para>
		///     <para>
		///         When enabled, tokenization may throw <see cref="System.NotSupportedException"/>s. 
		///         If the whole tokenizer chain is not compatible e.g. one of the
		///         <c>TokenStream</c>s does not implement the new <c>TokenStream</c> API.
        ///     </para>
        ///     <para>
		///         The default is <c>false</c>, so there is the fallback to the old API
		///         available.
        ///     </para>
        /// </remarks>
        /// <exception cref="System.NotSupportedException">When enabled, it make throw this exception</exception>
        [Obsolete("This setting will no longer be needed in Lucene 3.0 as the old API will be removed.")]
		public static void  SetOnlyUseNewAPI(bool onlyUseNewAPI)
        {
            #pragma warning disable 618
            TokenStream.onlyUseNewAPI = onlyUseNewAPI;
            #pragma warning restore 618
        }
		
		/// <summary> 
        ///     Returns <c>true</c> if the new API is used, otherwise <c>false</c>.
		/// </summary>
        [Obsolete("This setting will no longer be needed in Lucene 3.0 as the old API will be removed.")]
		public static bool GetOnlyUseNewAPI()
        {
            #pragma warning disable 618
            return onlyUseNewAPI;
            #pragma warning restore 618
        }
		
		/// 
        /// <summary> 
        ///     Consumers, like <see cref="Lucene.Net.Index.IndexWriter"/>, use this 
        ///     method to advance the stream to the next token. Implementing classes must 
        ///     implement this method and update the appropriate <see cref="Lucene.Net.Util.AttributeImpl"/>s 
        ///     with the attributes of the next token.
        /// </summary>
		/// <remarks>
        ///     <para>
		///         The producer must make no assumptions about the attributes after the
		///         method has been returned: the caller may arbitrarily change it. If the
		///         producer needs to preserve the state for subsequent calls, it can use
		///         <see cref="AttributeSource.CaptureState()"/> to create a copy of the 
        ///         current attribute state.
        ///     </para>
        ///     <para>
		///         This method is called for every token of a document, so an efficient
		///         implementation is crucial for good performance. To avoid calls to
		///         <see cref="AttributeSource.AddAttribute(Type)"/> and <see cref="AttributeSource.GetAttribute(Type)"/> or downcasts,
		///         references to all <see cref="AttributeImpl" />s that this stream uses should be
		///         retrieved during instantiation.
        ///     </para>
        ///     <para>
		///         To ensure that filters and consumers know which attributes are available,
		///         the attributes must be added during instantiation. Filters and consumers
		///         are not required to check for availability of attributes in
		///         <see cref="IncrementToken()" />.
        ///     </para>
        /// </remarks>
        /// <returns> <c>true</c> if the stream has <b>not</b> reached its end, otherwise <c>false</c>. </returns>
        
		public virtual bool IncrementToken()
        {
            // CHANGE: IncrementToken becomes an empty abstract method in 3.0 
            #pragma warning disable 618
            System.Diagnostics.Debug.Assert(tokenWrapper != null);
			
			Token token;
			if (supportedMethods.hasReusableNext)
			{
				token = Next(tokenWrapper.delegate_Renamed);
			}
			else
			{
				System.Diagnostics.Debug.Assert(supportedMethods.hasNext);
				token = Next();
			}
			
            if (token == null)
				return false;
			
            tokenWrapper.delegate_Renamed = token;
			return true;
            
            #pragma warning restore 618
        }
		
		/// <summary> 
        ///     This method is called by the consumer after the last token has been
		///     consumed, after <see cref="IncrementToken()" /> returned <c>false</c>
		///     Using the new <c>TokenStream</c> API, Streams implementing the old API
		///     should upgrade to use this feature.
        /// </summary>
		/// <remarks>
        ///     <para>
		///         This method can be used to perform any end-of-stream operations, like
		///         setting the final offset of a stream. The final offset of a stream might
		///         differ from the offset of the last token. e.g. in case one or more whitespaces
		///         followed after the last token and a <see cref="WhitespaceTokenizer"/> was used.
        ///     </para>
        /// </remarks>
        /// <exception cref="System.IO.IOException" />
		public virtual void  End()
		{
			// do nothing by default
		}
		
		/// <summary> 
        ///     Returns the next token in the stream, or <c>null</c> at end-of-stream.
        /// </summary>
		/// <remarks>
        ///     <para>
        ///         The input Token should be used as the Token that is returned when possible, which will 
        ///         give the fastest tokenization performance. However, this is not required. A new Token may be
        ///         returned. Callers may re-use a single Token instance for successive calls
        ///         to this method.
        ///     </para>
        ///     <para>
		///         This implicitly defines a "contract" between consumers, the callers of this
		///         method, and producers, the implementations of this method that are the source
		///         for tokens:
        ///     </para>
		///     <ul>
		///         <li>
        ///             A consumer must fully consume the previously returned <see cref="Token" />
		///             before calling this method again.
        ///         </li>
		///         <li>
        ///             A producer must call <see cref="Token.Clear()"/> before setting the fields in
		///             it and returning it.
        ///         </li>
		///     </ul>
        ///     <para>
		///         Also, the producer must make no assumptions about a <see cref="Token" /> after it
		///         has been returned: the caller may arbitrarily change it. If the producer
		///         needs to hold onto the <see cref="Token" /> for subsequent calls, it must clone()
		///         it before storing it. Note that a <see cref="TokenFilter" /> is considered a
		///         consumer.
        ///     </para>
        /// </remarks>
		/// <param name="reusableToken">
        ///     A <see cref="Token"/> that may or may not be used to return;
		///     this parameter should never be null. The callee is not required to
		///     check for null before using it, but it is a good idea to assert that
		///     it is not null.
		/// </param>
        /// <returns> 
        ///     The next <see cref="Token"/> in the stream or <c>null</c> if the end-of-stream was hit.
		/// </returns>
        [Obsolete("The new IncrementToken() and AttributeSource APIs should be used instead.")]
		public virtual Token Next(Token reusableToken)
		{
			System.Diagnostics.Debug.Assert(reusableToken != null);
			
			if (tokenWrapper == null)
				throw new System.NotSupportedException("This TokenStream only supports the new Attributes API.");
			
			if (supportedMethods.hasIncrementToken)
			{
				tokenWrapper.delegate_Renamed = reusableToken;
				return IncrementToken()?tokenWrapper.delegate_Renamed:null;
			}
			else
			{
				System.Diagnostics.Debug.Assert(supportedMethods.hasNext);
				return Next();
			}
		}
		
		/// <summary> 
        /// Returns the next <see cref="Token" /> in the stream, or null at EOS.
		/// </summary>
		/// <remarks>
        ///     <para>
        ///         The returned Token is a "full private copy" (not re-used across
		///         calls to <see cref="Next()" />) but will be slower than calling
		///         <see cref="Next(Token)" /> or using the new <see cref="IncrementToken()" />
		///         method with the new <see cref="AttributeSource" /> API.
        ///     </para>
        /// </remarks>
        [Obsolete("The returned Token is a \"full private copy\" (not re-used across calls to Next()) but will be slower than calling {@link #Next(Token)} or using the new IncrementToken() method with the new AttributeSource API.")]
		public virtual Token Next()
		{
            #pragma warning disable 618

			if (tokenWrapper == null)
				throw new System.NotSupportedException("This TokenStream only supports the new Attributes API.");
			
			Token nextToken;
			if (supportedMethods.hasIncrementToken)
			{
				Token savedDelegate = tokenWrapper.delegate_Renamed;
				tokenWrapper.delegate_Renamed = new Token();
				nextToken = IncrementToken()?tokenWrapper.delegate_Renamed:null;
				tokenWrapper.delegate_Renamed = savedDelegate;
			}
			else
			{
				System.Diagnostics.Debug.Assert(supportedMethods.hasReusableNext);
				nextToken = Next(new Token());
			}
			
			if (nextToken != null)
			{
				Lucene.Net.Index.Payload p = nextToken.GetPayload();
				if (p != null)
				{
					nextToken.SetPayload((Lucene.Net.Index.Payload) p.Clone());
				}
			}
			
            return nextToken;

            #pragma warning restore 618
		}
		
		/// <summary>
        /// Resets this stream to the beginning. This is an optional operation, so
		/// subclasses may or may not implement this method. <see cref="Reset()" /> is not needed for
		/// the standard indexing process.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         However, if the tokens of a <c>TokenStream</c> are intended to be 
        ///         consumed more than once, it is necessary to implement <see cref="Reset()" />. 
        ///         Note that if your <c>TokenStream</c> caches tokens and feeds them back again
        ///         after a reset, it is imperative that you clone the tokens when you 
        ///         store them away on the first pass as well as when you return 
        ///         them on future passes after <see cref="Reset()" />.
        ///     </para>
        /// </remarks>
		public virtual void  Reset()
		{
		}
		
		/// <summary>Releases resources associated with this stream. </summary>
		public virtual void  Close()
		{
		}
	}
}