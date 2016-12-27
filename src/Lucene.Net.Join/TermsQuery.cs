using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.Join
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
	/// A query that has an array of terms from a specific field. This query will match documents have one or more terms in
	/// the specified field that match with the terms specified in the array.
	/// 
	/// @lucene.experimental
	/// </summary>
    internal class TermsQuery : MultiTermQuery
    {
        private readonly BytesRefHash _terms;
        private readonly int[] _ords;
        private readonly Query _fromQuery; // Used for equals() only

        /// <summary>
        /// 
        /// </summary>
        /// <param name="field">The field that should contain terms that are specified in the previous parameter.</param>
        /// <param name="fromQuery"></param>
        /// <param name="terms">The terms that matching documents should have. The terms must be sorted by natural order.</param>
        internal TermsQuery(string field, Query fromQuery, BytesRefHash terms) : base(field)
        {
            _fromQuery = fromQuery;
            _terms = terms;
            _ords = terms.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            if (_terms.Size == 0)
            {
                return TermsEnum.EMPTY;
            }

            return new SeekingTermSetTermsEnum(terms.Iterator(null), _terms, _ords);

        }

        public override string ToString(string field)
        {
            return string.Format("TermsQuery{{field={0}}}", field);
        }

        private class SeekingTermSetTermsEnum : FilteredTermsEnum
        {
            private readonly BytesRefHash Terms;
            private readonly int[] Ords;
            private readonly int _lastElement;

            private readonly BytesRef _lastTerm;
            private readonly BytesRef _spare = new BytesRef();
            private readonly IComparer<BytesRef> _comparator;

            private BytesRef _seekTerm;
            private int _upto;

            internal SeekingTermSetTermsEnum(TermsEnum tenum, BytesRefHash terms, int[] ords) : base(tenum)
            {
                Terms = terms;
                Ords = ords;
                _comparator = BytesRef.UTF8SortedAsUnicodeComparer;
                _lastElement = terms.Size - 1;
                _lastTerm = terms.Get(ords[_lastElement], new BytesRef());
                _seekTerm = terms.Get(ords[_upto], _spare);
            }


            
            protected override BytesRef NextSeekTerm(BytesRef currentTerm)
            {
                BytesRef temp = _seekTerm;
                _seekTerm = null;
                return temp;
            }
            
            protected override AcceptStatus Accept(BytesRef term)
            {
                if (_comparator.Compare(term, _lastTerm) > 0)
                {
                    return AcceptStatus.END;
                }

                BytesRef currentTerm = Terms.Get(Ords[_upto], _spare);
                if (_comparator.Compare(term, currentTerm) == 0)
                {
                    if (_upto == _lastElement)
                    {
                        return AcceptStatus.YES;
                    }

                    _seekTerm = Terms.Get(Ords[++_upto], _spare);
                    return AcceptStatus.YES_AND_SEEK;
                }

                if (_upto == _lastElement)
                {
                    return AcceptStatus.NO;
                } // Our current term doesn't match the the given term.

                int cmp;
                do // We maybe are behind the given term by more than one step. Keep incrementing till we're the same or higher.
                {
                    if (_upto == _lastElement)
                    {
                        return AcceptStatus.NO;
                    }
                    // typically the terms dict is a superset of query's terms so it's unusual that we have to skip many of
                    // our terms so we don't do a binary search here
                    _seekTerm = Terms.Get(Ords[++_upto], _spare);
                } while ((cmp = _comparator.Compare(_seekTerm, term)) < 0);
                if (cmp == 0)
                {
                    if (_upto == _lastElement)
                    {
                        return AcceptStatus.YES;
                    }
                    _seekTerm = Terms.Get(Ords[++_upto], _spare);
                    return AcceptStatus.YES_AND_SEEK;
                }

                return AcceptStatus.NO_AND_SEEK;
            }
        }
    }
}