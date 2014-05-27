namespace Lucene.Net.Codecs.cheapbastard
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

	using DiskDocValuesFormat = Lucene.Net.Codecs.Diskdv.DiskDocValuesFormat;
	using DiskNormsFormat = Lucene.Net.Codecs.Diskdv.DiskNormsFormat;
	using Lucene40StoredFieldsFormat = Lucene.Net.Codecs.Lucene40.Lucene40StoredFieldsFormat;
	using Lucene40TermVectorsFormat = Lucene.Net.Codecs.Lucene40.Lucene40TermVectorsFormat;
	using Lucene41PostingsFormat = Lucene.Net.Codecs.Lucene41.Lucene41PostingsFormat;
	using Lucene46Codec = Lucene.Net.Codecs.Lucene46.Lucene46Codec;

	/// <summary>
	/// Codec that tries to use as little ram as possible because he spent all his money on beer </summary>
	// TODO: better name :) 
	// but if we named it "LowMemory" in codecs/ package, it would be irresistible like optimize()!
	public class CheapBastardCodec : FilterCodec
	{

	  // TODO: would be better to have no terms index at all and bsearch a terms dict
	  private readonly PostingsFormat Postings = new Lucene41PostingsFormat(100, 200);
	  // uncompressing versions, waste lots of disk but no ram
	  private readonly StoredFieldsFormat StoredFields = new Lucene40StoredFieldsFormat();
	  private readonly TermVectorsFormat TermVectors = new Lucene40TermVectorsFormat();
	  // these go to disk for all docvalues/norms datastructures
	  private readonly DocValuesFormat DocValues = new DiskDocValuesFormat();
	  private readonly NormsFormat Norms = new DiskNormsFormat();

	  public CheapBastardCodec() : base("CheapBastard", new Lucene46Codec())
	  {
	  }

	  public override PostingsFormat PostingsFormat()
	  {
		return Postings;
	  }

	  public override DocValuesFormat DocValuesFormat()
	  {
		return DocValues;
	  }

	  public override NormsFormat NormsFormat()
	  {
		return Norms;
	  }

	  public override StoredFieldsFormat StoredFieldsFormat()
	  {
		return StoredFields;
	  }

	  public override TermVectorsFormat TermVectorsFormat()
	  {
		return TermVectors;
	  }
	}

}