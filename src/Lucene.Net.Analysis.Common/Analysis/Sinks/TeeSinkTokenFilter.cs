using System.Collections.Generic;

namespace org.apache.lucene.analysis.sinks
{

	/*
	 * Licensed to the Apache Software Foundation (ASF) under one or more
	 * contributor license agreements.  See the NOTICE file distributed with
	 * this work for additional information regarding copyright ownership.
	 * The ASF licenses this file to You under the Apache License, Version 2.0
	 * (the "License"); you may not use this file except in compliance with
	 * the License.  You may obtain a copy of the License at
	 *
	 *     http://www.apache.org/licenses/LICENSE-2.0
	 *
	 * Unless required by applicable law or agreed to in writing, software
	 * distributed under the License is distributed on an "AS IS" BASIS,
	 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	 * See the License for the specific language governing permissions and
	 * limitations under the License.
	 */


	using AttributeImpl = org.apache.lucene.util.AttributeImpl;
	using AttributeSource = org.apache.lucene.util.AttributeSource;

	/// <summary>
	/// This TokenFilter provides the ability to set aside attribute states
	/// that have already been analyzed.  This is useful in situations where multiple fields share
	/// many common analysis steps and then go their separate ways.
	/// <p/>
	/// It is also useful for doing things like entity extraction or proper noun analysis as
	/// part of the analysis workflow and saving off those tokens for use in another field.
	/// 
	/// <pre class="prettyprint">
	/// TeeSinkTokenFilter source1 = new TeeSinkTokenFilter(new WhitespaceTokenizer(version, reader1));
	/// TeeSinkTokenFilter.SinkTokenStream sink1 = source1.newSinkTokenStream();
	/// TeeSinkTokenFilter.SinkTokenStream sink2 = source1.newSinkTokenStream();
	/// 
	/// TeeSinkTokenFilter source2 = new TeeSinkTokenFilter(new WhitespaceTokenizer(version, reader2));
	/// source2.addSinkTokenStream(sink1);
	/// source2.addSinkTokenStream(sink2);
	/// 
	/// TokenStream final1 = new LowerCaseFilter(version, source1);
	/// TokenStream final2 = source2;
	/// TokenStream final3 = new EntityDetect(sink1);
	/// TokenStream final4 = new URLDetect(sink2);
	/// 
	/// d.add(new TextField("f1", final1, Field.Store.NO));
	/// d.add(new TextField("f2", final2, Field.Store.NO));
	/// d.add(new TextField("f3", final3, Field.Store.NO));
	/// d.add(new TextField("f4", final4, Field.Store.NO));
	/// </pre>
	/// In this example, <code>sink1</code> and <code>sink2</code> will both get tokens from both
	/// <code>reader1</code> and <code>reader2</code> after whitespace tokenizer
	/// and now we can further wrap any of these in extra analysis, and more "sources" can be inserted if desired.
	/// It is important, that tees are consumed before sinks (in the above example, the field names must be
	/// less the sink's field names). If you are not sure, which stream is consumed first, you can simply
	/// add another sink and then pass all tokens to the sinks at once using <seealso cref="#consumeAllTokens"/>.
	/// This TokenFilter is exhausted after this. In the above example, change
	/// the example above to:
	/// <pre class="prettyprint">
	/// ...
	/// TokenStream final1 = new LowerCaseFilter(version, source1.newSinkTokenStream());
	/// TokenStream final2 = source2.newSinkTokenStream();
	/// sink1.consumeAllTokens();
	/// sink2.consumeAllTokens();
	/// ...
	/// </pre>
	/// In this case, the fields can be added in any order, because the sources are not used anymore and all sinks are ready.
	/// <para>Note, the EntityDetect and URLDetect TokenStreams are for the example and do not currently exist in Lucene.
	/// </para>
	/// </summary>
	public sealed class TeeSinkTokenFilter : TokenFilter
	{
	  private readonly IList<WeakReference<SinkTokenStream>> sinks = new LinkedList<WeakReference<SinkTokenStream>>();

	  /// <summary>
	  /// Instantiates a new TeeSinkTokenFilter.
	  /// </summary>
	  public TeeSinkTokenFilter(TokenStream input) : base(input)
	  {
	  }

	  /// <summary>
	  /// Returns a new <seealso cref="SinkTokenStream"/> that receives all tokens consumed by this stream.
	  /// </summary>
	  public SinkTokenStream newSinkTokenStream()
	  {
		return newSinkTokenStream(ACCEPT_ALL_FILTER);
	  }

	  /// <summary>
	  /// Returns a new <seealso cref="SinkTokenStream"/> that receives all tokens consumed by this stream
	  /// that pass the supplied filter. </summary>
	  /// <seealso cref= SinkFilter </seealso>
	  public SinkTokenStream newSinkTokenStream(SinkFilter filter)
	  {
		SinkTokenStream sink = new SinkTokenStream(this.cloneAttributes(), filter);
		this.sinks.Add(new WeakReference<>(sink));
		return sink;
	  }

	  /// <summary>
	  /// Adds a <seealso cref="SinkTokenStream"/> created by another <code>TeeSinkTokenFilter</code>
	  /// to this one. The supplied stream will also receive all consumed tokens.
	  /// This method can be used to pass tokens from two different tees to one sink.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public void addSinkTokenStream(final SinkTokenStream sink)
	  public void addSinkTokenStream(SinkTokenStream sink)
	  {
		// check that sink has correct factory
		if (!this.AttributeFactory.Equals(sink.AttributeFactory))
		{
		  throw new System.ArgumentException("The supplied sink is not compatible to this tee");
		}
		// add eventually missing attribute impls to the existing sink
		for (IEnumerator<AttributeImpl> it = this.cloneAttributes().AttributeImplsIterator; it.MoveNext();)
		{
		  sink.addAttributeImpl(it.Current);
		}
		this.sinks.Add(new WeakReference<>(sink));
	  }

	  /// <summary>
	  /// <code>TeeSinkTokenFilter</code> passes all tokens to the added sinks
	  /// when itself is consumed. To be sure, that all tokens from the input
	  /// stream are passed to the sinks, you can call this methods.
	  /// This instance is exhausted after this, but all sinks are instant available.
	  /// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void consumeAllTokens() throws java.io.IOException
	  public void consumeAllTokens()
	  {
		while (incrementToken())
		{
		}
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public boolean incrementToken() throws java.io.IOException
	  public override bool incrementToken()
	  {
		if (input.incrementToken())
		{
		  // capture state lazily - maybe no SinkFilter accepts this state
		  AttributeSource.State state = null;
		  foreach (WeakReference<SinkTokenStream> @ref in sinks)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SinkTokenStream sink = ref.get();
			SinkTokenStream sink = @ref.get();
			if (sink != null)
			{
			  if (sink.accept(this))
			  {
				if (state == null)
				{
				  state = this.captureState();
				}
				sink.addState(state);
			  }
			}
		  }
		  return true;
		}

		return false;
	  }

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: @Override public final void end() throws java.io.IOException
	  public override void end()
	  {
		base.end();
		AttributeSource.State finalState = captureState();
		foreach (WeakReference<SinkTokenStream> @ref in sinks)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final SinkTokenStream sink = ref.get();
		  SinkTokenStream sink = @ref.get();
		  if (sink != null)
		  {
			sink.FinalState = finalState;
		  }
		}
	  }

	  /// <summary>
	  /// A filter that decides which <seealso cref="AttributeSource"/> states to store in the sink.
	  /// </summary>
	  public abstract class SinkFilter
	  {
		/// <summary>
		/// Returns true, iff the current state of the passed-in <seealso cref="AttributeSource"/> shall be stored
		/// in the sink. 
		/// </summary>
		public abstract bool accept(AttributeSource source);

		/// <summary>
		/// Called by <seealso cref="SinkTokenStream#reset()"/>. This method does nothing by default
		/// and can optionally be overridden.
		/// </summary>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: public void reset() throws java.io.IOException
		public virtual void reset()
		{
		  // nothing to do; can be overridden
		}
	  }

	  /// <summary>
	  /// TokenStream output from a tee with optional filtering.
	  /// </summary>
	  public sealed class SinkTokenStream : TokenStream
	  {
		internal readonly IList<AttributeSource.State> cachedStates = new LinkedList<AttributeSource.State>();
		internal AttributeSource.State finalState;
		internal IEnumerator<AttributeSource.State> it = null;
		internal SinkFilter filter;

		internal SinkTokenStream(AttributeSource source, SinkFilter filter) : base(source)
		{
		  this.filter = filter;
		}

		internal bool accept(AttributeSource source)
		{
		  return filter.accept(source);
		}

		internal void addState(AttributeSource.State state)
		{
		  if (it != null)
		  {
			throw new System.InvalidOperationException("The tee must be consumed before sinks are consumed.");
		  }
		  cachedStates.Add(state);
		}

		internal AttributeSource.State FinalState
		{
			set
			{
			  this.finalState = value;
			}
		}

		public override bool incrementToken()
		{
		  // lazy init the iterator
		  if (it == null)
		  {
			it = cachedStates.GetEnumerator();
		  }

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  if (!it.hasNext())
		  {
			return false;
		  }

//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
		  AttributeSource.State state = it.next();
		  restoreState(state);
		  return true;
		}

		public override void end()
		{
		  if (finalState != null)
		  {
			restoreState(finalState);
		  }
		}

		public override void reset()
		{
		  it = cachedStates.GetEnumerator();
		}
	  }

	  private static readonly SinkFilter ACCEPT_ALL_FILTER = new SinkFilterAnonymousInnerClassHelper();

	  private class SinkFilterAnonymousInnerClassHelper : SinkFilter
	  {
		  public SinkFilterAnonymousInnerClassHelper()
		  {
		  }

		  public override bool accept(AttributeSource source)
		  {
			return true;
		  }
	  }

	}

}