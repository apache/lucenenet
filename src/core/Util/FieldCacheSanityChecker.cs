using System;
using System.Collections.Generic;
using System.Text;

namespace Lucene.Net.Util
{
	/// <summary>
	/// Copyright 2009 The Apache Software Foundation
	/// 
	/// Licensed under the Apache License, Version 2.0 (the "License");
	/// you may not use this file except in compliance with the License.
	/// You may obtain a copy of the License at
	/// 
	///     http://www.apache.org/licenses/LICENSE-2.0
	/// 
	/// Unless required by applicable law or agreed to in writing, software
	/// distributed under the License is distributed on an "AS IS" BASIS,
	/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
	/// See the License for the specific language governing permissions and
	/// limitations under the License.
	/// </summary>


	using CompositeReader = Lucene.Net.Index.CompositeReader;
	using IndexReader = Lucene.Net.Index.IndexReader;
	using IndexReaderContext = Lucene.Net.Index.IndexReaderContext;
	using FieldCache = Lucene.Net.Search.FieldCache;
	using FieldCache_CacheEntry = Lucene.Net.Search.FieldCache_CacheEntry;
	using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;

	/// <summary>
	/// Provides methods for sanity checking that entries in the FieldCache 
	/// are not wasteful or inconsistent.
	/// </p>
	/// <p>
	/// Lucene 2.9 Introduced numerous enhancements into how the FieldCache 
	/// is used by the low levels of Lucene searching (for Sorting and 
	/// ValueSourceQueries) to improve both the speed for Sorting, as well 
	/// as reopening of IndexReaders.  But these changes have shifted the 
	/// usage of FieldCache from "top level" IndexReaders (frequently a 
	/// MultiReader or DirectoryReader) down to the leaf level SegmentReaders.  
	/// As a result, existing applications that directly access the FieldCache 
	/// may find RAM usage increase significantly when upgrading to 2.9 or 
	/// Later.  this class provides an API for these applications (or their 
	/// Unit tests) to check at run time if the FieldCache contains "insane" 
	/// usages of the FieldCache.
	/// </p>
	/// @lucene.experimental </summary>
	/// <seealso cref= FieldCache </seealso>
	/// <seealso cref= FieldCacheSanityChecker.Insanity </seealso>
	/// <seealso cref= FieldCacheSanityChecker.InsanityType </seealso>
	public sealed class FieldCacheSanityChecker
	{

	  private bool EstimateRam;

	  public FieldCacheSanityChecker()
	  {
		/* NOOP */
	  }

	  /// <summary>
	  /// If set, estimate size for all CacheEntry objects will be calculateed.
	  /// </summary>
	  public bool RamUsageEstimator
	  {
		  set
		  {
			EstimateRam = value;
		  }
	  }


	  /// <summary>
	  /// Quick and dirty convenience method </summary>
	  /// <seealso cref= #check </seealso>
	  public static Insanity[] CheckSanity(FieldCache cache)
	  {
		return CheckSanity(cache.CacheEntries);
	  }

	  /// <summary>
	  /// Quick and dirty convenience method that instantiates an instance with 
	  /// "good defaults" and uses it to test the CacheEntrys </summary>
	  /// <seealso cref= #check </seealso>
	  public static Insanity[] CheckSanity(params FieldCache_CacheEntry[] cacheEntries)
	  {
		FieldCacheSanityChecker sanityChecker = new FieldCacheSanityChecker();
		sanityChecker.RamUsageEstimator = true;
		return sanityChecker.Check(cacheEntries);
	  }


	  /// <summary>
	  /// Tests a CacheEntry[] for indication of "insane" cache usage.
	  /// <p>
	  /// <B>NOTE:</b>FieldCache CreationPlaceholder objects are ignored.
	  /// (:TODO: is this a bad idea? are we masking a real problem?)
	  /// </p>
	  /// </summary>
	  public Insanity[] Check(params FieldCache_CacheEntry[] cacheEntries)
	  {
		if (null == cacheEntries || 0 == cacheEntries.Length)
		{
		  return new Insanity[0];
		}

		if (EstimateRam)
		{
		  for (int i = 0; i < cacheEntries.Length; i++)
		  {
			cacheEntries[i].EstimateSize();
		  }
		}

		// the indirect mapping lets MapOfSet dedup identical valIds for us
		//
		// maps the (valId) identityhashCode of cache values to 
		// sets of CacheEntry instances
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final MapOfSets<Integer, Lucene.Net.Search.FieldCache_CacheEntry> valIdToItems = new MapOfSets<>(new java.util.HashMap<Integer, java.util.Set<Lucene.Net.Search.FieldCache_CacheEntry>>(17));
		MapOfSets<int?, FieldCache_CacheEntry> valIdToItems = new MapOfSets<int?, FieldCache_CacheEntry>(new Dictionary<int?, Set<FieldCache_CacheEntry>>(17));
		// maps ReaderField keys to Sets of ValueIds
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final MapOfSets<ReaderField, Integer> readerFieldToValIds = new MapOfSets<>(new java.util.HashMap<ReaderField, java.util.Set<Integer>>(17));
		MapOfSets<ReaderField, int?> readerFieldToValIds = new MapOfSets<ReaderField, int?>(new Dictionary<ReaderField, Set<int?>>(17));
		//

		// any keys that we know result in more then one valId
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Set<ReaderField> valMismatchKeys = new java.util.HashSet<>();
		Set<ReaderField> valMismatchKeys = new HashSet<ReaderField>();

		// iterate over all the cacheEntries to get the mappings we'll need
		for (int i = 0; i < cacheEntries.Length; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Lucene.Net.Search.FieldCache_CacheEntry item = cacheEntries[i];
		  FieldCache_CacheEntry item = cacheEntries[i];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object val = item.getValue();
		  object val = item.Value;

		  // It's OK to have dup entries, where one is eg
		  // float[] and the other is the Bits (from
		  // getDocWithField())
		  if (val is Bits)
		  {
			continue;
		  }

		  if (val is Lucene.Net.Search.FieldCache_CreationPlaceholder)
		  {
			continue;
		  }

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ReaderField rf = new ReaderField(item.getReaderKey(), item.getFieldName());
		  ReaderField rf = new ReaderField(item.ReaderKey, item.FieldName);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Integer valId = Integer.valueOf(System.identityHashCode(val));
		  int? valId = Convert.ToInt32(System.identityHashCode(val));

		  // indirect mapping, so the MapOfSet will dedup identical valIds for us
		  valIdToItems.Put(valId, item);
		  if (1 < readerFieldToValIds.Put(rf, valId))
		  {
			valMismatchKeys.add(rf);
		  }
		}

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Insanity> insanity = new java.util.ArrayList<>(valMismatchKeys.size() * 3);
		IList<Insanity> insanity = new List<Insanity>(valMismatchKeys.size() * 3);

		insanity.AddRange(CheckValueMismatch(valIdToItems, readerFieldToValIds, valMismatchKeys));
		insanity.AddRange(CheckSubreaders(valIdToItems, readerFieldToValIds));

		return insanity.ToArray();
	  }

	  /// <summary>
	  /// Internal helper method used by check that iterates over 
	  /// valMismatchKeys and generates a Collection of Insanity 
	  /// instances accordingly.  The MapOfSets are used to populate 
	  /// the Insanity objects. </summary>
	  /// <seealso cref= InsanityType#VALUEMISMATCH </seealso>
	  private ICollection<Insanity> CheckValueMismatch(MapOfSets<int?, FieldCache_CacheEntry> valIdToItems, MapOfSets<ReaderField, int?> readerFieldToValIds, Set<ReaderField> valMismatchKeys)
	  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Insanity> insanity = new java.util.ArrayList<>(valMismatchKeys.size() * 3);
		IList<Insanity> insanity = new List<Insanity>(valMismatchKeys.size() * 3);

		if (!valMismatchKeys.Empty)
		{
		  // we have multiple values for some ReaderFields

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<ReaderField, java.util.Set<Integer>> rfMap = readerFieldToValIds.getMap();
		  IDictionary<ReaderField, Set<int?>> rfMap = readerFieldToValIds.Map;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.Map<Integer, java.util.Set<Lucene.Net.Search.FieldCache_CacheEntry>> valMap = valIdToItems.getMap();
		  IDictionary<int?, Set<FieldCache_CacheEntry>> valMap = valIdToItems.Map;
		  foreach (ReaderField rf in valMismatchKeys)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Lucene.Net.Search.FieldCache_CacheEntry> badEntries = new java.util.ArrayList<>(valMismatchKeys.size() * 2);
			IList<FieldCache_CacheEntry> badEntries = new List<FieldCache_CacheEntry>(valMismatchKeys.size() * 2);
			foreach (Integer value in rfMap[rf])
			{
			  foreach (FieldCache_CacheEntry cacheEntry in valMap[value])
			  {
				badEntries.Add(cacheEntry);
			  }
			}

			FieldCache_CacheEntry[] badness = new FieldCache_CacheEntry[badEntries.Count];
			badness = badEntries.toArray(badness);

			insanity.Add(new Insanity(InsanityType.VALUEMISMATCH, "Multiple distinct value objects for " + rf.ToString(), badness));
		  }
		}
		return insanity;
	  }

	  /// <summary>
	  /// Internal helper method used by check that iterates over 
	  /// the keys of readerFieldToValIds and generates a Collection 
	  /// of Insanity instances whenever two (or more) ReaderField instances are 
	  /// found that have an ancestry relationships.  
	  /// </summary>
	  /// <seealso cref= InsanityType#SUBREADER </seealso>
	  private ICollection<Insanity> CheckSubreaders(MapOfSets<int?, FieldCache_CacheEntry> valIdToItems, MapOfSets<ReaderField, int?> readerFieldToValIds)
	  {

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Insanity> insanity = new java.util.ArrayList<>(23);
		IList<Insanity> insanity = new List<Insanity>(23);

		IDictionary<ReaderField, Set<ReaderField>> badChildren = new Dictionary<ReaderField, Set<ReaderField>>(17);
		MapOfSets<ReaderField, ReaderField> badKids = new MapOfSets<ReaderField, ReaderField>(badChildren); // wrapper

		IDictionary<int?, Set<FieldCache_CacheEntry>> viToItemSets = valIdToItems.Map;
		IDictionary<ReaderField, Set<int?>> rfToValIdSets = readerFieldToValIds.Map;

		Set<ReaderField> seen = new HashSet<ReaderField>(17);

		IDictionary<ReaderField, Set<int?>>.KeyCollection readerFields = rfToValIdSets.Keys;
		foreach (ReaderField rf in readerFields)
		{

		  if (seen.contains(rf))
		  {
			  continue;
		  }

		  IList<object> kids = GetAllDescendantReaderKeys(rf.readerKey);
		  foreach (object kidKey in kids)
		  {
			ReaderField kid = new ReaderField(kidKey, rf.fieldName);

			if (badChildren.ContainsKey(kid))
			{
			  // we've already process this kid as RF and found other problems
			  // track those problems as our own
			  badKids.Put(rf, kid);
			  badKids.PutAll(rf, badChildren[kid]);
			  badChildren.Remove(kid);

			}
			else if (rfToValIdSets.ContainsKey(kid))
			{
			  // we have cache entries for the kid
			  badKids.Put(rf, kid);
			}
			seen.add(kid);
		  }
		  seen.add(rf);
		}

		// every mapping in badKids represents an Insanity
		foreach (ReaderField parent in badChildren.Keys)
		{
		  Set<ReaderField> kids = badChildren[parent];

		  IList<FieldCache_CacheEntry> badEntries = new List<FieldCache_CacheEntry>(kids.size() * 2);

		  // put parent entr(ies) in first
		  {
			foreach (Integer value in rfToValIdSets[parent])
			{
			  badEntries.AddRange(viToItemSets[value]);
			}
		  }

		  // now the entries for the descendants
		  foreach (ReaderField kid in kids)
		  {
			foreach (Integer value in rfToValIdSets[kid])
			{
			  badEntries.AddRange(viToItemSets[value]);
			}
		  }

		  FieldCache_CacheEntry[] badness = new FieldCache_CacheEntry[badEntries.Count];
		  badness = badEntries.toArray(badness);

		  insanity.Add(new Insanity(InsanityType.SUBREADER, "Found caches for descendants of " + parent.ToString(), badness));
		}

		return insanity;

	  }

	  /// <summary>
	  /// Checks if the seed is an IndexReader, and if so will walk
	  /// the hierarchy of subReaders building up a list of the objects 
	  /// returned by {@code seed.getCoreCacheKey()}
	  /// </summary>
	  private IList<object> GetAllDescendantReaderKeys(object seed)
	  {
		IList<object> all = new List<object>(17); // will grow as we iter
		all.Add(seed);
		for (int i = 0; i < all.Count; i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Object obj = all.get(i);
		  object obj = all[i];
		  // TODO: We don't check closed readers here (as getTopReaderContext
		  // throws AlreadyClosedException), what should we do? Reflection?
		  if (obj is IndexReader)
		  {
			try
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.List<Lucene.Net.Index.IndexReaderContext> childs = ((Lucene.Net.Index.IndexReader) obj).getContext().children();
			  IList<IndexReaderContext> childs = ((IndexReader) obj).Context.Children();
			  if (childs != null) // it is composite reader
			  {
				foreach (IndexReaderContext ctx in childs)
				{
				  all.Add(ctx.reader().CoreCacheKey);
				}
			  }
			}
			catch (AlreadyClosedException ace)
			{
			  // ignore this reader
			}
		  }
		}
		// need to skip the first, because it was the seed
		return all.subList(1, all.Count);
	  }

	  /// <summary>
	  /// Simple pair object for using "readerKey + fieldName" a Map key
	  /// </summary>
	  private sealed class ReaderField
	  {
		public readonly object ReaderKey;
		public readonly string FieldName;
		public ReaderField(object readerKey, string fieldName)
		{
		  this.ReaderKey = readerKey;
		  this.FieldName = fieldName;
		}
		public override int HashCode()
		{
		  return System.identityHashCode(ReaderKey) * FieldName.HashCode();
		}
		public override bool Equals(object that)
		{
		  if (!(that is ReaderField))
		  {
			  return false;
		  }

		  ReaderField other = (ReaderField) that;
		  return (this.ReaderKey == other.ReaderKey && this.FieldName.Equals(other.FieldName));
		}
		public override string ToString()
		{
		  return ReaderKey.ToString() + "+" + FieldName;
		}
	  }

	  /// <summary>
	  /// Simple container for a collection of related CacheEntry objects that 
	  /// in conjunction with each other represent some "insane" usage of the 
	  /// FieldCache.
	  /// </summary>
	  public sealed class Insanity
	  {
		internal readonly InsanityType Type_Renamed;
		internal readonly string Msg_Renamed;
		internal readonly FieldCache_CacheEntry[] Entries;
		public Insanity(InsanityType type, string msg, params FieldCache_CacheEntry[] entries)
		{
		  if (null == type)
		  {
			throw new System.ArgumentException("Insanity requires non-null InsanityType");
		  }
		  if (null == entries || 0 == entries.Length)
		  {
			throw new System.ArgumentException("Insanity requires non-null/non-empty CacheEntry[]");
		  }
		  this.Type_Renamed = type;
		  this.Msg_Renamed = msg;
		  this.Entries = entries;

		}
		/// <summary>
		/// Type of insane behavior this object represents
		/// </summary>
		public InsanityType Type
		{
			get
			{
				return Type_Renamed;
			}
		}
		/// <summary>
		/// Description of hte insane behavior
		/// </summary>
		public string Msg
		{
			get
			{
				return Msg_Renamed;
			}
		}
		/// <summary>
		/// CacheEntry objects which suggest a problem
		/// </summary>
		public FieldCache_CacheEntry[] CacheEntries
		{
			get
			{
				return Entries;
			}
		}
		/// <summary>
		/// Multi-Line representation of this Insanity object, starting with 
		/// the Type and Msg, followed by each CacheEntry.toString() on it's 
		/// own line prefaced by a tab character
		/// </summary>
		public override string ToString()
		{
		  StringBuilder buf = new StringBuilder();
		  buf.Append(Type).Append(": ");

		  string m = Msg;
		  if (null != m)
		  {
			  buf.Append(m);
		  }

		  buf.Append('\n');

		  FieldCache_CacheEntry[] ce = CacheEntries;
		  for (int i = 0; i < ce.Length; i++)
		  {
			buf.Append('\t').Append(ce[i].ToString()).Append('\n');
		  }

		  return buf.ToString();
		}
	  }

	  /// <summary>
	  /// An Enumeration of the different types of "insane" behavior that 
	  /// may be detected in a FieldCache.
	  /// </summary>
	  /// <seealso cref= InsanityType#SUBREADER </seealso>
	  /// <seealso cref= InsanityType#VALUEMISMATCH </seealso>
	  /// <seealso cref= InsanityType#EXPECTED </seealso>
	  public sealed class InsanityType
	  {
		internal readonly string Label;
		internal InsanityType(string label)
		{
		  this.Label = label;
		}
		public override string ToString()
		{
			return Label;
		}

		/// <summary>
		/// Indicates an overlap in cache usage on a given field 
		/// in sub/super readers.
		/// </summary>
		public static readonly InsanityType SUBREADER = new InsanityType("SUBREADER");

		/// <summary>
		/// <p>
		/// Indicates entries have the same reader+fieldname but 
		/// different cached values.  this can happen if different datatypes, 
		/// or parsers are used -- and while it's not necessarily a bug 
		/// it's typically an indication of a possible problem.
		/// </p>
		/// <p>
		/// <b>NOTE:</b> Only the reader, fieldname, and cached value are actually 
		/// tested -- if two cache entries have different parsers or datatypes but 
		/// the cached values are the same Object (== not just equal()) this method 
		/// does not consider that a red flag.  this allows for subtle variations 
		/// in the way a Parser is specified (null vs DEFAULT_LONG_PARSER, etc...)
		/// </p>
		/// </summary>
		public static readonly InsanityType VALUEMISMATCH = new InsanityType("VALUEMISMATCH");

		/// <summary>
		/// Indicates an expected bit of "insanity".  this may be useful for 
		/// clients that wish to preserve/log information about insane usage 
		/// but indicate that it was expected. 
		/// </summary>
		public static readonly InsanityType EXPECTED = new InsanityType("EXPECTED");
	  }


	}

}