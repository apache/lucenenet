using System;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Lucene3x
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

	using FieldInfo = Lucene.Net.Index.FieldInfo;
	using IndexFileNames = Lucene.Net.Index.IndexFileNames;
	using Directory = Lucene.Net.Store.Directory;
	using IOContext = Lucene.Net.Store.IOContext;
	using IndexOutput = Lucene.Net.Store.IndexOutput;
	using BytesRef = Lucene.Net.Util.BytesRef;
	using IOUtils = Lucene.Net.Util.IOUtils;

	/// <summary>
	/// Writes and Merges Lucene 3.x norms format
	/// @lucene.experimental
	/// </summary>
	internal class PreFlexRWNormsConsumer : DocValuesConsumer
	{

	  /// <summary>
	  /// norms header placeholder </summary>
	  private static readonly sbyte[] NORMS_HEADER = new sbyte[]{'N','R','M',-1};

	  /// <summary>
	  /// Extension of norms file </summary>
	  private const string NORMS_EXTENSION = "nrm";

	  /// <summary>
	  /// Extension of separate norms file </summary>
	  /// @deprecated Only for reading existing 3.x indexes  
	  [Obsolete("Only for reading existing 3.x indexes")]
	  private const string SEPARATE_NORMS_EXTENSION = "s";

	  private readonly IndexOutput @out;
	  private int LastFieldNumber = -1; // only for assert

	  public PreFlexRWNormsConsumer(Directory directory, string segment, IOContext context)
	  {
		string normsFileName = IndexFileNames.segmentFileName(segment, "", NORMS_EXTENSION);
		bool success = false;
		IndexOutput output = null;
		try
		{
		  output = directory.createOutput(normsFileName, context);
		  output.writeBytes(NORMS_HEADER, 0, NORMS_HEADER.Length);
		  @out = output;
		  success = true;
		}
		finally
		{
		  if (!success)
		  {
			IOUtils.closeWhileHandlingException(output);
		  }
		}
	  }

	  public override void AddNumericField(FieldInfo field, IEnumerable<Number> values)
	  {
		Debug.Assert(field.number > LastFieldNumber, "writing norms fields out of order" + LastFieldNumber + " -> " + field.number);
		foreach (Number n in values)
		{
		  if ((long)n < sbyte.MinValue || (long)n > sbyte.MaxValue)
		  {
			throw new System.NotSupportedException("3.x cannot index norms that won't fit in a byte, got: " + (long)n);
		  }
		  @out.writeByte((sbyte)n);
		}
		LastFieldNumber = field.number;
	  }

	  public override void Close()
	  {
		IOUtils.close(@out);
	  }

	  public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
	  {
		throw new AssertionError();
	  }

	  public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrd)
	  {
		throw new AssertionError();
	  }

	  public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<Number> docToOrdCount, IEnumerable<Number> ords)
	  {
		throw new AssertionError();
	  }
	}

}