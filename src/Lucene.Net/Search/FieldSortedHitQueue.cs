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

using IndexReader = Lucene.Net.Index.IndexReader;
using PriorityQueue = Lucene.Net.Util.PriorityQueue;

namespace Lucene.Net.Search
{
	
	/// <summary> Expert: A hit queue for sorting by hits by terms in more than one field.
	/// Uses <code>FieldCache.DEFAULT</code> for maintaining internal term lookup tables.
	/// 
	/// <p>Created: Dec 8, 2003 12:56:03 PM
	/// 
	/// </summary>
	/// <author>   Tim Jones (Nacimiento Software)
	/// </author>
	/// <since>   lucene 1.4
	/// </since>
	/// <version>  $Id: FieldSortedHitQueue.java 605225 2007-12-18 15:13:05Z gsingers $
	/// </version>
	/// <seealso cref="Searcher.Search(Query,Filter,int,Sort)">
	/// </seealso>
	/// <seealso cref="FieldCache">
	/// </seealso>
	public class FieldSortedHitQueue : PriorityQueue
	{
		internal class AnonymousClassCache : FieldCacheImpl.Cache
		{
			
			protected internal override System.Object CreateValue(IndexReader reader, System.Object entryKey)
			{
				FieldCacheImpl.Entry entry = (FieldCacheImpl.Entry) entryKey;
				System.String fieldname = entry.field;
				int type = entry.type;
				System.Globalization.CultureInfo locale = entry.locale;
				SortComparatorSource factory = (SortComparatorSource) entry.custom;
				ScoreDocComparator comparator;
				switch (type)
				{
					
					case SortField.AUTO: 
						comparator = Lucene.Net.Search.FieldSortedHitQueue.ComparatorAuto(reader, fieldname);
						break;
					
					case SortField.INT: 
						comparator = Lucene.Net.Search.FieldSortedHitQueue.ComparatorInt(reader, fieldname);
						break;
					
					case SortField.FLOAT: 
						comparator = Lucene.Net.Search.FieldSortedHitQueue.ComparatorFloat(reader, fieldname);
						break;
					
					case SortField.LONG: 
						comparator = Lucene.Net.Search.FieldSortedHitQueue.ComparatorLong(reader, fieldname);
						break;
					
					case SortField.DOUBLE: 
						comparator = Lucene.Net.Search.FieldSortedHitQueue.ComparatorDouble(reader, fieldname);
						break;
					
					case SortField.STRING: 
						if (locale != null)
							comparator = Lucene.Net.Search.FieldSortedHitQueue.ComparatorStringLocale(reader, fieldname, locale);
						else
							comparator = Lucene.Net.Search.FieldSortedHitQueue.ComparatorString(reader, fieldname);
						break;
					
					case SortField.CUSTOM: 
						comparator = factory.NewComparator(reader, fieldname);
						break;
					
					default: 
						throw new System.SystemException("unknown field type: " + type);
					
				}
				return comparator;
			}
		}
		private class AnonymousClassScoreDocComparator : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator(int[] fieldOrder)
			{
				InitBlock(fieldOrder);
			}
			private void  InitBlock(int[] fieldOrder)
			{
				this.fieldOrder = fieldOrder;
			}
			private int[] fieldOrder;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				int fi = fieldOrder[i.doc];
				int fj = fieldOrder[j.doc];
				if (fi < fj)
					return - 1;
				if (fi > fj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return (System.Int32) fieldOrder[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.INT;
			}
		}
		private class AnonymousClassScoreDocComparator1 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator1(long[] fieldOrder)
			{
				InitBlock(fieldOrder);
			}
			private void  InitBlock(long[] fieldOrder)
			{
				this.fieldOrder = fieldOrder;
			}
			private long[] fieldOrder;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				long li = fieldOrder[i.doc];
				long lj = fieldOrder[j.doc];
				if (li < lj)
					return - 1;
				if (li > lj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return (long) fieldOrder[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.LONG;
			}
		}
		private class AnonymousClassScoreDocComparator2 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator2(float[] fieldOrder)
			{
				InitBlock(fieldOrder);
			}
			private void  InitBlock(float[] fieldOrder)
			{
				this.fieldOrder = fieldOrder;
			}
			private float[] fieldOrder;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				float fi = fieldOrder[i.doc];
				float fj = fieldOrder[j.doc];
				if (fi < fj)
					return - 1;
				if (fi > fj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return (float) fieldOrder[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.FLOAT;
			}
		}
		private class AnonymousClassScoreDocComparator3 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator3(double[] fieldOrder)
			{
				InitBlock(fieldOrder);
			}
			private void  InitBlock(double[] fieldOrder)
			{
				this.fieldOrder = fieldOrder;
			}
			private double[] fieldOrder;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				double di = fieldOrder[i.doc];
				double dj = fieldOrder[j.doc];
				if (di < dj)
					return - 1;
				if (di > dj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return (double) fieldOrder[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.DOUBLE;
			}
		}
		private class AnonymousClassScoreDocComparator4 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator4(Lucene.Net.Search.StringIndex index)
			{
				InitBlock(index);
			}
			private void  InitBlock(Lucene.Net.Search.StringIndex index)
			{
				this.index = index;
			}
			private Lucene.Net.Search.StringIndex index;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				int fi = index.Order[i.doc];
				int fj = index.Order[j.doc];
				if (fi < fj)
					return - 1;
				if (fi > fj)
					return 1;
				return 0;
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return index.Lookup[index.Order[i.doc]];
			}
			
			public virtual int SortType()
			{
				return SortField.STRING;
			}
		}
		private class AnonymousClassScoreDocComparator5 : ScoreDocComparator
		{
			public AnonymousClassScoreDocComparator5(System.String[] index, System.Globalization.CompareInfo collator)
			{
				InitBlock(index, collator);
			}
			private void  InitBlock(System.String[] index, System.Globalization.CompareInfo collator)
			{
				this.index = index;
				this.collator = collator;
			}
			private System.String[] index;
			private System.Globalization.CompareInfo collator;
			
			public int Compare(ScoreDoc i, ScoreDoc j)
			{
				System.String is_Renamed = index[i.doc];
				System.String js = index[j.doc];
				if ((System.Object) is_Renamed == (System.Object) js)
				{
					return 0;
				}
				else if (is_Renamed == null)
				{
					return - 1;
				}
				else if (js == null)
				{
					return 1;
				}
				else
				{
					return collator.Compare(is_Renamed.ToString(), js.ToString());
				}
			}
			
			public virtual System.IComparable SortValue(ScoreDoc i)
			{
				return index[i.doc];
			}
			
			public virtual int SortType()
			{
				return SortField.STRING;
			}
		}
		
		/// <summary> Creates a hit queue sorted by the given list of fields.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fields">Fieldable names, in priority order (highest priority first).  Cannot be <code>null</code> or empty.
		/// </param>
		/// <param name="size"> The number of hits to retain.  Must be greater than zero.
		/// </param>
		/// <throws>  IOException </throws>
		public FieldSortedHitQueue(IndexReader reader, SortField[] fields, int size)
		{
			int n = fields.Length;
			comparators = new ScoreDocComparator[n];
			this.fields = new SortField[n];
			for (int i = 0; i < n; ++i)
			{
				System.String fieldname = fields[i].GetField();
				comparators[i] = GetCachedComparator(reader, fieldname, fields[i].GetType(), fields[i].GetLocale(), fields[i].GetFactory());
				
				if (comparators[i].SortType() == SortField.STRING)
				{
					this.fields[i] = new SortField(fieldname, fields[i].GetLocale(), fields[i].GetReverse());
				}
				else
				{
					this.fields[i] = new SortField(fieldname, comparators[i].SortType(), fields[i].GetReverse());
				}
			}
			Initialize(size);
		}
		
		
		/// <summary>Stores a comparator corresponding to each field being sorted by </summary>
		protected internal ScoreDocComparator[] comparators;
		
		/// <summary>Stores the sort criteria being used. </summary>
		protected internal SortField[] fields;
		
		/// <summary>Stores the maximum score value encountered, needed for normalizing. </summary>
		protected internal float maxscore = System.Single.NegativeInfinity;
		
		/// <summary>returns the maximum score encountered by elements inserted via insert()</summary>
		public virtual float GetMaxScore()
		{
			return maxscore;
		}
		
		// Update maxscore.
		private void  UpdateMaxScore(FieldDoc fdoc)
		{
			maxscore = System.Math.Max(maxscore, fdoc.score);
		}
		
		// The signature of this method takes a FieldDoc in order to avoid
		// the unneeded cast to retrieve the score.
		// inherit javadoc
		public virtual bool Insert(FieldDoc fdoc)
		{
			UpdateMaxScore(fdoc);
			return base.Insert(fdoc);
		}
		
		// This overrides PriorityQueue.insert() so that insert(FieldDoc) that
		// keeps track of the score isn't accidentally bypassed.  
		// inherit javadoc
		public override bool Insert(System.Object fdoc)
		{
			return Insert((FieldDoc) fdoc);
		}
		
		// This overrides PriorityQueue.insertWithOverflow() so that
		// updateMaxScore(FieldDoc) that keeps track of the score isn't accidentally
		// bypassed.
		public override System.Object InsertWithOverflow(System.Object element)
		{
			UpdateMaxScore((FieldDoc) element);
			return base.InsertWithOverflow(element);
		}
		
		/// <summary> Returns whether <code>a</code> is less relevant than <code>b</code>.</summary>
		/// <param name="a">ScoreDoc
		/// </param>
		/// <param name="b">ScoreDoc
		/// </param>
		/// <returns> <code>true</code> if document <code>a</code> should be sorted after document <code>b</code>.
		/// </returns>
		public override bool LessThan(System.Object a, System.Object b)
		{
			ScoreDoc docA = (ScoreDoc) a;
			ScoreDoc docB = (ScoreDoc) b;
			
			// run comparators
			int n = comparators.Length;
			int c = 0;
			for (int i = 0; i < n && c == 0; ++i)
			{
				c = (fields[i].reverse) ? comparators[i].Compare(docB, docA) : comparators[i].Compare(docA, docB);
			}
			// avoid random sort order that could lead to duplicates (bug #31241):
			if (c == 0)
				return docA.doc > docB.doc;
			return c > 0;
		}
		
		
		/// <summary> Given a FieldDoc object, stores the values used
		/// to sort the given document.  These values are not the raw
		/// values out of the index, but the internal representation
		/// of them.  This is so the given search hit can be collated
		/// by a MultiSearcher with other search hits.
		/// </summary>
		/// <param name="doc"> The FieldDoc to store sort values into.
		/// </param>
		/// <returns>  The same FieldDoc passed in.
		/// </returns>
		/// <seealso cref="Searchable.Search(Weight,Filter,int,Sort)">
		/// </seealso>
		internal virtual FieldDoc FillFields(FieldDoc doc)
		{
			int n = comparators.Length;
			System.IComparable[] fields = new System.IComparable[n];
			for (int i = 0; i < n; ++i)
				fields[i] = comparators[i].SortValue(doc);
			doc.fields = fields;
			//if (maxscore > 1.0f) doc.score /= maxscore;   // normalize scores
			return doc;
		}
		
		
		/// <summary>Returns the SortFields being used by this hit queue. </summary>
		internal virtual SortField[] GetFields()
		{
			return fields;
		}
		
		internal static ScoreDocComparator GetCachedComparator(IndexReader reader, System.String field, int type, System.Globalization.CultureInfo locale, SortComparatorSource factory)
		{
			if (type == SortField.DOC)
				return Lucene.Net.Search.ScoreDocComparator_Fields.INDEXORDER;
			if (type == SortField.SCORE)
				return Lucene.Net.Search.ScoreDocComparator_Fields.RELEVANCE;
			FieldCacheImpl.Entry entry = (factory != null) ? new FieldCacheImpl.Entry(field, factory) : new FieldCacheImpl.Entry(field, type, locale);
			return (ScoreDocComparator) Comparators.Get(reader, entry);
		}
		
		/// <summary>Internal cache of comparators. Similar to FieldCache, only
		/// caches comparators instead of term values. 
		/// </summary>
		internal static readonly FieldCacheImpl.Cache Comparators;
		
		/// <summary> Returns a comparator for sorting hits according to a field containing integers.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Fieldable containg integer values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorInt(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			int[] fieldOrder = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetInts(reader, field);
			return new AnonymousClassScoreDocComparator(fieldOrder);
		}
		
		/// <summary> Returns a comparator for sorting hits according to a field containing integers.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Fieldable containg integer values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorLong(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			long[] fieldOrder = Lucene.Net.Search.ExtendedFieldCache_Fields.EXT_DEFAULT.GetLongs(reader, field);
			return new AnonymousClassScoreDocComparator1(fieldOrder);
		}
		
		
		/// <summary> Returns a comparator for sorting hits according to a field containing floats.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Fieldable containg float values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorFloat(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			float[] fieldOrder = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetFloats(reader, field);
			return new AnonymousClassScoreDocComparator2(fieldOrder);
		}
		
		/// <summary> Returns a comparator for sorting hits according to a field containing doubles.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Fieldable containg float values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorDouble(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			double[] fieldOrder = Lucene.Net.Search.ExtendedFieldCache_Fields.EXT_DEFAULT.GetDoubles(reader, field);
			return new AnonymousClassScoreDocComparator3(fieldOrder);
		}
		
		/// <summary> Returns a comparator for sorting hits according to a field containing strings.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Fieldable containg string values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorString(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			Lucene.Net.Search.StringIndex index = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetStringIndex(reader, field);
			return new AnonymousClassScoreDocComparator4(index);
		}
		
		/// <summary> Returns a comparator for sorting hits according to a field containing strings.</summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Fieldable containg string values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorStringLocale(IndexReader reader, System.String fieldname, System.Globalization.CultureInfo locale)
		{
			System.Globalization.CompareInfo collator = locale.CompareInfo;
			System.String field = String.Intern(fieldname);
			System.String[] index = Lucene.Net.Search.FieldCache_Fields.DEFAULT.GetStrings(reader, field);
			return new AnonymousClassScoreDocComparator5(index, collator);
		}
		
		/// <summary> Returns a comparator for sorting hits according to values in the given field.
		/// The terms in the field are looked at to determine whether they contain integers,
		/// floats or strings.  Once the type is determined, one of the other static methods
		/// in this class is called to get the comparator.
		/// </summary>
		/// <param name="reader"> Index to use.
		/// </param>
		/// <param name="fieldname"> Fieldable containg values.
		/// </param>
		/// <returns>  Comparator for sorting hits.
		/// </returns>
		/// <throws>  IOException If an error occurs reading the index. </throws>
		internal static ScoreDocComparator ComparatorAuto(IndexReader reader, System.String fieldname)
		{
			System.String field = String.Intern(fieldname);
			System.Object lookupArray = Lucene.Net.Search.ExtendedFieldCache_Fields.EXT_DEFAULT.GetAuto(reader, field);
			if (lookupArray is Lucene.Net.Search.StringIndex)
			{
				return ComparatorString(reader, field);
			}
			else if (lookupArray is int[])
			{
				return ComparatorInt(reader, field);
			}
			else if (lookupArray is long[])
			{
				return ComparatorLong(reader, field);
			}
			else if (lookupArray is float[])
			{
				return ComparatorFloat(reader, field);
			}
			else if (lookupArray is System.String[])
			{
				return ComparatorString(reader, field);
			}
			else
			{
				throw new System.SystemException("unknown data type in field '" + field + "'");
			}
		}
		static FieldSortedHitQueue()
		{
			Comparators = new AnonymousClassCache();
		}
	}
}