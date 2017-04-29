using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// Abstract class for enumerating a subset of all terms.
    /// <para/>
    /// Term enumerations are always ordered by
    /// <see cref="Comparer"/>.  Each term in the enumeration is
    /// greater than all that precede it.
    /// <para/><c>Please note:</c> Consumers of this enumeration cannot
    /// call <c>Seek()</c>, it is forward only; it throws
    /// <see cref="NotSupportedException"/> when a seeking method
    /// is called.
    /// </summary>
#if FEATURE_SERIALIZABLE
    [Serializable]
#endif
    public abstract class FilteredTermsEnum : TermsEnum
    {
        private BytesRef initialSeekTerm = null;
        private bool doSeek;
        private BytesRef actualTerm = null;

        private readonly TermsEnum tenum;

        /// <summary>
        /// Return value, if term should be accepted or the iteration should
        /// <see cref="END"/>. The <c>*_SEEK</c> values denote, that after handling the current term
        /// the enum should call <see cref="NextSeekTerm(BytesRef)"/> and step forward. </summary>
        /// <seealso cref="Accept(BytesRef)"/>
        protected internal enum AcceptStatus
        {
            /// <summary>
            /// Accept the term and position the enum at the next term. </summary>
            YES,

            /// <summary>
            /// Accept the term and advance (<see cref="FilteredTermsEnum.NextSeekTerm(BytesRef)"/>)
            /// to the next term.
            /// </summary>
            YES_AND_SEEK,

            /// <summary>
            /// Reject the term and position the enum at the next term. </summary>
            NO,

            /// <summary>
            /// Reject the term and advance (<see cref="FilteredTermsEnum.NextSeekTerm(BytesRef)"/>)
            /// to the next term.
            /// </summary>
            NO_AND_SEEK,

            /// <summary>
            /// Reject the term and stop enumerating. </summary>
            END
        }

        /// <summary>
        /// Return if term is accepted, not accepted or the iteration should ended
        /// (and possibly seek).
        /// </summary>
        protected abstract AcceptStatus Accept(BytesRef term);

        /// <summary>
        /// Creates a filtered <see cref="TermsEnum"/> on a terms enum. </summary>
        /// <param name="tenum"> the terms enumeration to filter. </param>
        public FilteredTermsEnum(TermsEnum tenum)
            : this(tenum, true)
        {
        }

        /// <summary>
        /// Creates a filtered <see cref="TermsEnum"/> on a terms enum. </summary>
        /// <param name="tenum"> the terms enumeration to filter. </param>
        /// <param name="startWithSeek"> start with seek </param>
        public FilteredTermsEnum(TermsEnum tenum, bool startWithSeek)
        {
            Debug.Assert(tenum != null);
            this.tenum = tenum;
            doSeek = startWithSeek;
        }

        /// <summary>
        /// Use this method to set the initial <see cref="BytesRef"/>
        /// to seek before iterating. This is a convenience method for
        /// subclasses that do not override <see cref="NextSeekTerm(BytesRef)"/>.
        /// If the initial seek term is <c>null</c> (default),
        /// the enum is empty.
        /// <para/>You can only use this method, if you keep the default
        /// implementation of <see cref="NextSeekTerm(BytesRef)"/>.
        /// </summary>
        protected void SetInitialSeekTerm(BytesRef term)
        {
            this.initialSeekTerm = term;
        }

        /// <summary>
        /// On the first call to <see cref="Next()"/> or if <see cref="Accept(BytesRef)"/> returns
        /// <see cref="AcceptStatus.YES_AND_SEEK"/> or <see cref="AcceptStatus.NO_AND_SEEK"/>,
        /// this method will be called to eventually seek the underlying <see cref="TermsEnum"/>
        /// to a new position.
        /// On the first call, <paramref name="currentTerm"/> will be <c>null</c>, later
        /// calls will provide the term the underlying enum is positioned at.
        /// This method returns per default only one time the initial seek term
        /// and then <c>null</c>, so no repositioning is ever done.
        /// <para/>
        /// Override this method, if you want a more sophisticated <see cref="TermsEnum"/>,
        /// that repositions the iterator during enumeration.
        /// If this method always returns <c>null</c> the enum is empty.
        /// <para/><c>Please note:</c> this method should always provide a greater term
        /// than the last enumerated term, else the behavior of this enum
        /// violates the contract for <see cref="TermsEnum"/>s.
        /// </summary>
        protected virtual BytesRef NextSeekTerm(BytesRef currentTerm)
        {
            BytesRef t = initialSeekTerm;
            initialSeekTerm = null;
            return t;
        }

        /// <summary>
        /// Returns the related attributes, the returned <see cref="AttributeSource"/>
        /// is shared with the delegate <see cref="TermsEnum"/>.
        /// </summary>
        public override AttributeSource Attributes
        {
            get { return tenum.Attributes; }
        }

        public override BytesRef Term
        {
            get { return tenum.Term; }
        }

        public override IComparer<BytesRef> Comparer
        {
            get
            {
                return tenum.Comparer;
            }
        }

        public override int DocFreq
        {
            get { return tenum.DocFreq; }
        }

        public override long TotalTermFreq
        {
            get { return tenum.TotalTermFreq; }
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="NotSupportedException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override bool SeekExact(BytesRef term)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="NotSupportedException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override SeekStatus SeekCeil(BytesRef term)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="NotSupportedException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override void SeekExact(long ord)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        public override long Ord
        {
            get { return tenum.Ord; }
        }

        public override DocsEnum Docs(IBits bits, DocsEnum reuse, DocsFlags flags)
        {
            return tenum.Docs(bits, reuse, flags);
        }

        public override DocsAndPositionsEnum DocsAndPositions(IBits bits, DocsAndPositionsEnum reuse, DocsAndPositionsFlags flags)
        {
            return tenum.DocsAndPositions(bits, reuse, flags);
        }

        /// <summary>
        /// this enum does not support seeking! </summary>
        /// <exception cref="NotSupportedException"> In general, subclasses do not
        ///         support seeking. </exception>
        public override void SeekExact(BytesRef term, TermState state)
        {
            throw new System.NotSupportedException(this.GetType().Name + " does not support seeking");
        }

        /// <summary>
        /// Returns the filtered enums term state
        /// </summary>
        public override TermState GetTermState()
        {
            Debug.Assert(tenum != null);
            return tenum.GetTermState();
        }

        public override BytesRef Next()
        {
            //System.out.println("FTE.next doSeek=" + doSeek);
            //new Throwable().printStackTrace(System.out);
            for (; ; )
            {
                // Seek or forward the iterator
                if (doSeek)
                {
                    doSeek = false;
                    BytesRef t = NextSeekTerm(actualTerm);
                    //System.out.println("  seek to t=" + (t == null ? "null" : t.utf8ToString()) + " tenum=" + tenum);
                    // Make sure we always seek forward:
                    Debug.Assert(actualTerm == null || t == null || Comparer.Compare(t, actualTerm) > 0, "curTerm=" + actualTerm + " seekTerm=" + t);
                    if (t == null || tenum.SeekCeil(t) == SeekStatus.END)
                    {
                        // no more terms to seek to or enum exhausted
                        //System.out.println("  return null");
                        return null;
                    }
                    actualTerm = tenum.Term;
                    //System.out.println("  got term=" + actualTerm.utf8ToString());
                }
                else
                {
                    actualTerm = tenum.Next();
                    if (actualTerm == null)
                    {
                        // enum exhausted
                        return null;
                    }
                }

                // check if term is accepted
                switch (Accept(actualTerm))
                {
                    case FilteredTermsEnum.AcceptStatus.YES_AND_SEEK:
                        doSeek = true;
                        // term accepted, but we need to seek so fall-through
                        goto case FilteredTermsEnum.AcceptStatus.YES;
                    case FilteredTermsEnum.AcceptStatus.YES:
                        // term accepted
                        return actualTerm;

                    case FilteredTermsEnum.AcceptStatus.NO_AND_SEEK:
                        // invalid term, seek next time
                        doSeek = true;
                        break;

                    case FilteredTermsEnum.AcceptStatus.END:
                        // we are supposed to end the enum
                        return null;
                }
            }
        }
    }
}