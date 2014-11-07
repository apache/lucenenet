using System;
using System.Diagnostics;
using System.Collections.Generic;

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

namespace org.apache.lucene.analysis.charfilter
{


	using CharsRef = org.apache.lucene.util.CharsRef;
	using IntsRef = org.apache.lucene.util.IntsRef;
	using Builder = org.apache.lucene.util.fst.Builder;
	using CharSequenceOutputs = org.apache.lucene.util.fst.CharSequenceOutputs;
	using FST = org.apache.lucene.util.fst.FST;
	using Outputs = org.apache.lucene.util.fst.Outputs;
	using Util = org.apache.lucene.util.fst.Util;

	// TODO: save/load?

	/// <summary>
	/// Holds a map of String input to String output, to be used
	/// with <seealso cref="MappingCharFilter"/>.  Use the <seealso cref="Builder"/>
	/// to create this.
	/// </summary>
	public class NormalizeCharMap
	{

	  internal readonly FST<CharsRef> map;
	  internal readonly IDictionary<char?, FST.Arc<CharsRef>> cachedRootArcs = new Dictionary<char?, FST.Arc<CharsRef>>();

	  // Use the builder to create:
	  private NormalizeCharMap(FST<CharsRef> map)
	  {
		this.map = map;
		if (map != null)
		{
		  try
		  {
			// Pre-cache root arcs:
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.Arc<org.apache.lucene.util.CharsRef> scratchArc = new org.apache.lucene.util.fst.FST.Arc<>();
			FST.Arc<CharsRef> scratchArc = new FST.Arc<CharsRef>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST.BytesReader fstReader = map.getBytesReader();
			FST.BytesReader fstReader = map.BytesReader;
			map.getFirstArc(scratchArc);
			if (FST.targetHasArcs(scratchArc))
			{
			  map.readFirstRealTargetArc(scratchArc.target, scratchArc, fstReader);
			  while (true)
			  {
				Debug.Assert(scratchArc.label != FST.END_LABEL);
				cachedRootArcs[Convert.ToChar((char) scratchArc.label)] = (new FST.Arc<CharsRef>()).copyFrom(scratchArc);
				if (scratchArc.Last)
				{
				  break;
				}
				map.readNextRealArc(scratchArc, fstReader);
			  }
			}
			//System.out.println("cached " + cachedRootArcs.size() + " root arcs");
		  }
		  catch (IOException ioe)
		  {
			// Bogus FST IOExceptions!!  (will never happen)
			throw new Exception(ioe);
		  }
		}
	  }

	  /// <summary>
	  /// Builds an NormalizeCharMap.
	  /// <para>
	  /// Call add() until you have added all the mappings, then call build() to get a NormalizeCharMap
	  /// @lucene.experimental
	  /// </para>
	  /// </summary>
	  public class Builder
	  {

		internal readonly IDictionary<string, string> pendingPairs = new SortedDictionary<string, string>();

		/// <summary>
		/// Records a replacement to be applied to the input
		///  stream.  Whenever <code>singleMatch</code> occurs in
		///  the input, it will be replaced with
		///  <code>replacement</code>.
		/// </summary>
		/// <param name="match"> input String to be replaced </param>
		/// <param name="replacement"> output String </param>
		/// <exception cref="IllegalArgumentException"> if
		/// <code>match</code> is the empty string, or was
		/// already previously added </exception>
		public virtual void add(string match, string replacement)
		{
		  if (match.Length == 0)
		  {
			throw new System.ArgumentException("cannot match the empty string");
		  }
		  if (pendingPairs.ContainsKey(match))
		  {
			throw new System.ArgumentException("match \"" + match + "\" was already added");
		  }
		  pendingPairs[match] = replacement;
		}

		/// <summary>
		/// Builds the NormalizeCharMap; call this once you
		///  are done calling <seealso cref="#add"/>. 
		/// </summary>
		public virtual NormalizeCharMap build()
		{

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.FST<org.apache.lucene.util.CharsRef> map;
		  FST<CharsRef> map;
		  try
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.Outputs<org.apache.lucene.util.CharsRef> outputs = org.apache.lucene.util.fst.CharSequenceOutputs.getSingleton();
			Outputs<CharsRef> outputs = CharSequenceOutputs.Singleton;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.fst.Builder<org.apache.lucene.util.CharsRef> builder = new org.apache.lucene.util.fst.Builder<>(org.apache.lucene.util.fst.FST.INPUT_TYPE.BYTE2, outputs);
			Builder<CharsRef> builder = new Builder<CharsRef>(FST.INPUT_TYPE.BYTE2, outputs);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final org.apache.lucene.util.IntsRef scratch = new org.apache.lucene.util.IntsRef();
			IntsRef scratch = new IntsRef();
			foreach (KeyValuePair<string, string> ent in pendingPairs.SetOfKeyValuePairs())
			{
			  builder.add(Util.toUTF16(ent.Key, scratch), new CharsRef(ent.Value));
			}
			map = builder.finish();
			pendingPairs.Clear();
		  }
		  catch (IOException ioe)
		  {
			// Bogus FST IOExceptions!!  (will never happen)
			throw new Exception(ioe);
		  }

		  return new NormalizeCharMap(map);
		}
	  }
	}

}