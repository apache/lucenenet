// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Sinks
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

    /// <summary>
    /// This TokenFilter provides the ability to set aside attribute states
    /// that have already been analyzed.  This is useful in situations where multiple fields share
    /// many common analysis steps and then go their separate ways.
    /// <para/>
    /// It is also useful for doing things like entity extraction or proper noun analysis as
    /// part of the analysis workflow and saving off those tokens for use in another field.
    /// <para/>
    /// <code>
    /// TeeSinkTokenFilter source1 = new TeeSinkTokenFilter(new WhitespaceTokenizer(version, reader1));
    /// TeeSinkTokenFilter.SinkTokenStream sink1 = source1.NewSinkTokenStream();
    /// TeeSinkTokenFilter.SinkTokenStream sink2 = source1.NewSinkTokenStream();
    /// 
    /// TeeSinkTokenFilter source2 = new TeeSinkTokenFilter(new WhitespaceTokenizer(version, reader2));
    /// source2.AddSinkTokenStream(sink1);
    /// source2.AddSinkTokenStream(sink2);
    /// 
    /// TokenStream final1 = new LowerCaseFilter(version, source1);
    /// TokenStream final2 = source2;
    /// TokenStream final3 = new EntityDetect(sink1);
    /// TokenStream final4 = new URLDetect(sink2);
    /// 
    /// d.Add(new TextField("f1", final1, Field.Store.NO));
    /// d.Add(new TextField("f2", final2, Field.Store.NO));
    /// d.Add(new TextField("f3", final3, Field.Store.NO));
    /// d.Add(new TextField("f4", final4, Field.Store.NO));
    /// </code>
    /// In this example, <c>sink1</c> and <c>sink2</c> will both get tokens from both
    /// <c>reader1</c> and <c>reader2</c> after whitespace tokenizer
    /// and now we can further wrap any of these in extra analysis, and more "sources" can be inserted if desired.
    /// It is important, that tees are consumed before sinks (in the above example, the field names must be
    /// less the sink's field names). If you are not sure, which stream is consumed first, you can simply
    /// add another sink and then pass all tokens to the sinks at once using <see cref="ConsumeAllTokens"/>.
    /// This <see cref="TokenFilter"/> is exhausted after this. In the above example, change
    /// the example above to:
    /// <code>
    /// ...
    /// TokenStream final1 = new LowerCaseFilter(version, source1.NewSinkTokenStream());
    /// TokenStream final2 = source2.NewSinkTokenStream();
    /// sink1.ConsumeAllTokens();
    /// sink2.ConsumeAllTokens();
    /// ...
    /// </code>
    /// In this case, the fields can be added in any order, because the sources are not used anymore and all sinks are ready.
    /// <para>Note, the EntityDetect and URLDetect TokenStreams are for the example and do not currently exist in Lucene.
    /// </para>
    /// </summary>
    public sealed class TeeSinkTokenFilter : TokenFilter
    {
        private readonly ICollection<WeakReference<SinkTokenStream>> sinks = new LinkedList<WeakReference<SinkTokenStream>>();

        /// <summary>
        /// Instantiates a new <see cref="TeeSinkTokenFilter"/>.
        /// </summary>
        public TeeSinkTokenFilter(TokenStream input)
            : base(input)
        {
        }

        /// <summary>
        /// Returns a new <see cref="SinkTokenStream"/> that receives all tokens consumed by this stream.
        /// </summary>
        public SinkTokenStream NewSinkTokenStream()
        {
            return NewSinkTokenStream(ACCEPT_ALL_FILTER);
        }

        /// <summary>
        /// Returns a new <see cref="SinkTokenStream"/> that receives all tokens consumed by this stream
        /// that pass the supplied filter. </summary>
        /// <seealso cref="SinkFilter"/>
        public SinkTokenStream NewSinkTokenStream(SinkFilter filter)
        {
            var sink = new SinkTokenStream(CloneAttributes(), filter);
            this.sinks.Add(new WeakReference<SinkTokenStream>(sink));
            return sink;
        }

        /// <summary>
        /// Adds a <see cref="SinkTokenStream"/> created by another <see cref="TeeSinkTokenFilter"/>
        /// to this one. The supplied stream will also receive all consumed tokens.
        /// This method can be used to pass tokens from two different tees to one sink.
        /// </summary>
        public void AddSinkTokenStream(SinkTokenStream sink)
        {
            // check that sink has correct factory
            if (!GetAttributeFactory().Equals(sink.GetAttributeFactory()))
            {
                throw new ArgumentException("The supplied sink is not compatible to this tee");
            }
            // add eventually missing attribute impls to the existing sink
            for (var it = CloneAttributes().GetAttributeImplsEnumerator(); it.MoveNext();)
            {
                sink.AddAttributeImpl(it.Current);
            }
            this.sinks.Add(new WeakReference<SinkTokenStream>(sink));
        }

        /// <summary>
        /// <see cref="TeeSinkTokenFilter"/> passes all tokens to the added sinks
        /// when itself is consumed. To be sure, that all tokens from the input
        /// stream are passed to the sinks, you can call this methods.
        /// This instance is exhausted after this, but all sinks are instant available.
        /// </summary>
        public void ConsumeAllTokens()
        {
            while (IncrementToken())
            {
            }
        }

        public override bool IncrementToken()
        {
            if (m_input.IncrementToken())
            {
                // capture state lazily - maybe no SinkFilter accepts this state
                AttributeSource.State state = null;
                foreach (WeakReference<SinkTokenStream> @ref in sinks)
                {
                    SinkTokenStream sink;
                    if (@ref.TryGetTarget(out sink))
                    {
                        if (sink.Accept(this))
                        {
                            if (state is null)
                            {
                                state = CaptureState();
                            }
                            sink.AddState(state);
                        }
                    }
                }
                return true;
            }

            return false;
        }

        public override sealed void End()
        {
            base.End();
            AttributeSource.State finalState = CaptureState();
            foreach (WeakReference<SinkTokenStream> @ref in sinks)
            {
                SinkTokenStream sink;
                if (@ref.TryGetTarget(out sink))
                {
                    sink.SetFinalState(finalState);
                }
            }
        }

        /// <summary>
        /// A filter that decides which <see cref="AttributeSource"/> states to store in the sink.
        /// </summary>
        public abstract class SinkFilter
        {
            /// <summary>
            /// Returns true, iff the current state of the passed-in <see cref="AttributeSource"/> shall be stored
            /// in the sink. 
            /// </summary>
            public abstract bool Accept(AttributeSource source);

            /// <summary>
            /// Called by <see cref="SinkTokenStream.Reset()"/>. This method does nothing by default
            /// and can optionally be overridden.
            /// </summary>
            public virtual void Reset()
            {
                // nothing to do; can be overridden
            }
        }

        /// <summary>
        /// <see cref="TokenStream"/> output from a tee with optional filtering.
        /// </summary>
        public sealed class SinkTokenStream : TokenStream
        {
            private readonly IList<AttributeSource.State> cachedStates = new JCG.List<AttributeSource.State>();
            private AttributeSource.State finalState;
            private IEnumerator<AttributeSource.State> it = null;
            private readonly SinkFilter filter; // LUCENENET: marked readonly

            internal SinkTokenStream(AttributeSource source, SinkFilter filter)
                : base(source)
            {
                this.filter = filter;
            }

            internal bool Accept(AttributeSource source)
            {
                return filter.Accept(source);
            }

            internal void AddState(AttributeSource.State state)
            {
                if (it != null)
                {
                    throw IllegalStateException.Create("The tee must be consumed before sinks are consumed.");
                }
                cachedStates.Add(state);
            }

            internal void SetFinalState(AttributeSource.State finalState)
            {
                this.finalState = finalState;
            }

            public override sealed bool IncrementToken()
            {
                // lazy init the iterator
                if (it is null)
                {
                    it = cachedStates.GetEnumerator();
                }

                if (!it.MoveNext())
                    return false;

                var state = it.Current;
                RestoreState(state);
                return true;
            }

            public override sealed void End()
            {
                if (finalState != null)
                {
                    RestoreState(finalState);
                }
            }

            public override sealed void Reset()
            {
                it = cachedStates.GetEnumerator();
            }
        }

        private static readonly SinkFilter ACCEPT_ALL_FILTER = new SinkFilterAnonymousClass();

        private sealed class SinkFilterAnonymousClass : SinkFilter
        {
            public override bool Accept(AttributeSource source)
            {
                return true;
            }
        }
    }
}