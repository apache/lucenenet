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

namespace Lucene.Net.Search
{
	
	
	/// <summary>A Scorer for queries with a required subscorer and an excluding (prohibited) subscorer.
	/// <br>
	/// This <code>Scorer</code> implements {@link Scorer#SkipTo(int)},
	/// and it uses the skipTo() on the given scorers.
	/// </summary>
	public class ReqExclScorer : Scorer
	{
		private Scorer reqScorer, exclScorer;
		
		/// <summary>Construct a <code>ReqExclScorer</code>.</summary>
		/// <param name="reqScorer">The scorer that must match, except where
		/// </param>
		/// <param name="exclScorer">indicates exclusion.
		/// </param>
		public ReqExclScorer(Scorer reqScorer, Scorer exclScorer) : base(null)
		{ // No similarity used.
			this.reqScorer = reqScorer;
			this.exclScorer = exclScorer;
		}
		
		private bool firstTime = true;
		
		public override bool Next()
		{
			if (firstTime)
			{
				if (!exclScorer.Next())
				{
					exclScorer = null; // exhausted at start
				}
				firstTime = false;
			}
			if (reqScorer == null)
			{
				return false;
			}
			if (!reqScorer.Next())
			{
				reqScorer = null; // exhausted, nothing left
				return false;
			}
			if (exclScorer == null)
			{
				return true; // reqScorer.next() already returned true
			}
			return ToNonExcluded();
		}
		
		/// <summary>Advance to non excluded doc.
		/// <br>On entry:
		/// <ul>
		/// <li>reqScorer != null,
		/// <li>exclScorer != null,
		/// <li>reqScorer was advanced once via next() or skipTo()
		/// and reqScorer.doc() may still be excluded.
		/// </ul>
		/// Advances reqScorer a non excluded required doc, if any.
		/// </summary>
		/// <returns> true iff there is a non excluded required doc.
		/// </returns>
		private bool ToNonExcluded()
		{
			int exclDoc = exclScorer.Doc();
			do 
			{
				int reqDoc = reqScorer.Doc(); // may be excluded
				if (reqDoc < exclDoc)
				{
					return true; // reqScorer advanced to before exclScorer, ie. not excluded
				}
				else if (reqDoc > exclDoc)
				{
					if (!exclScorer.SkipTo(reqDoc))
					{
						exclScorer = null; // exhausted, no more exclusions
						return true;
					}
					exclDoc = exclScorer.Doc();
					if (exclDoc > reqDoc)
					{
						return true; // not excluded
					}
				}
			}
			while (reqScorer.Next());
			reqScorer = null; // exhausted, nothing left
			return false;
		}
		
		public override int Doc()
		{
			return reqScorer.Doc(); // reqScorer may be null when next() or skipTo() already return false
		}
		
		/// <summary>Returns the score of the current document matching the query.
		/// Initially invalid, until {@link #next()} is called the first time.
		/// </summary>
		/// <returns> The score of the required scorer.
		/// </returns>
		public override float Score()
		{
			return reqScorer.Score(); // reqScorer may be null when next() or skipTo() already return false
		}
		
		/// <summary>Skips to the first match beyond the current whose document number is
		/// greater than or equal to a given target.
		/// <br>When this method is used the {@link #Explain(int)} method should not be used.
		/// </summary>
		/// <param name="target">The target document number.
		/// </param>
		/// <returns> true iff there is such a match.
		/// </returns>
		public override bool SkipTo(int target)
		{
			if (firstTime)
			{
				firstTime = false;
				if (!exclScorer.SkipTo(target))
				{
					exclScorer = null; // exhausted
				}
			}
			if (reqScorer == null)
			{
				return false;
			}
			if (exclScorer == null)
			{
				return reqScorer.SkipTo(target);
			}
			if (!reqScorer.SkipTo(target))
			{
				reqScorer = null;
				return false;
			}
			return ToNonExcluded();
		}
		
		public override Explanation Explain(int doc)
		{
			Explanation res = new Explanation();
			if (exclScorer.SkipTo(doc) && (exclScorer.Doc() == doc))
			{
				res.SetDescription("excluded");
			}
			else
			{
				res.SetDescription("not excluded");
				res.AddDetail(reqScorer.Explain(doc));
			}
			return res;
		}
	}
}