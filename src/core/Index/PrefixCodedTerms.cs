using System;
using System.Diagnostics;
using System.Collections.Generic;

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


	using IndexInput = Lucene.Net.Store.IndexInput;
	using RAMFile = Lucene.Net.Store.RAMFile;
	using RAMInputStream = Lucene.Net.Store.RAMInputStream;
	using RAMOutputStream = Lucene.Net.Store.RAMOutputStream;
	using BytesRef = Lucene.Net.Util.BytesRef;
    using System.IO;

	/// <summary>
	/// Prefix codes term instances (prefixes are shared)
	/// @lucene.experimental
	/// </summary>
	internal class PrefixCodedTerms : IEnumerable<Term>
	{
	  internal readonly RAMFile Buffer;

	  private PrefixCodedTerms(RAMFile buffer)
	  {
		this.Buffer = buffer;
	  }

	  /// <returns> size in bytes </returns>
	  public virtual long SizeInBytes
	  {
		  get
		  {
			return Buffer.SizeInBytes;
		  }
	  }

	  /// <returns> iterator over the bytes </returns>
	  public virtual IEnumerator<Term> GetEnumerator()
	  {
		return new PrefixCodedTermsIterator(this);
	  }

	  internal class PrefixCodedTermsIterator : IEnumerator<Term>
	  {
		  internal bool InstanceFieldsInitialized = false;

		  internal virtual void InitializeInstanceFields()
		  {
			  Term = new Term(Field, Bytes);
		  }

		  private readonly PrefixCodedTerms OuterInstance;

		internal readonly IndexInput Input;
		internal string Field = "";
		internal BytesRef Bytes = new BytesRef();
		internal Term Term;

		internal PrefixCodedTermsIterator(PrefixCodedTerms outerInstance)
		{
			this.OuterInstance = outerInstance;

			if (!InstanceFieldsInitialized)
			{
				InitializeInstanceFields();
				InstanceFieldsInitialized = true;
			}
		  try
		  {
			Input = new RAMInputStream("PrefixCodedTermsIterator", outerInstance.Buffer);
		  }
		  catch (IOException e)
		  {
			throw new Exception(e.Message, e);
		  }
		}

		public override bool HasNext()
		{
		  return Input.FilePointer < Input.Length();
		}

		public override Term Next()
		{
		  Debug.Assert(HasNext());
		  try
		  {
			int code = Input.ReadVInt();
			if ((code & 1) != 0)
			{
			  // new field
			  Field = Input.ReadString();
			}
			int prefix = (int)((uint)code >> 1);
			int suffix = Input.ReadVInt();
			Bytes.Grow(prefix + suffix);
			Input.ReadBytes(Bytes.Bytes, prefix, suffix);
			Bytes.Length = prefix + suffix;
			Term.Set(Field, Bytes);
			return Term;
		  }
		  catch (IOException e)
		  {
			throw new Exception(e.Message, e);
		  }
		}

		public override void Remove()
		{
		  throw new System.NotSupportedException();
		}
	  }

	  /// <summary>
	  /// Builds a PrefixCodedTerms: call add repeatedly, then finish. </summary>
	  public class Builder
	  {
		  internal bool InstanceFieldsInitialized = false;

		  public Builder()
		  {
			  if (!InstanceFieldsInitialized)
			  {
				  InitializeInstanceFields();
				  InstanceFieldsInitialized = true;
			  }
		  }

		  internal virtual void InitializeInstanceFields()
		  {
			  Output = new RAMOutputStream(Buffer);
		  }

		internal RAMFile Buffer = new RAMFile();
		internal RAMOutputStream Output;
		internal Term LastTerm = new Term("");

		/// <summary>
		/// add a term </summary>
		public virtual void Add(Term term)
		{
		  Debug.Assert(LastTerm.Equals(new Term("")) || term.CompareTo(LastTerm) > 0);

		  try
		  {
			int prefix = SharedPrefix(LastTerm.Bytes_Renamed, term.Bytes_Renamed);
			int suffix = term.Bytes_Renamed.Length - prefix;
			if (term.Field_Renamed.Equals(LastTerm.Field_Renamed))
			{
			  Output.WriteVInt(prefix << 1);
			}
			else
			{
			  Output.WriteVInt(prefix << 1 | 1);
			  Output.WriteString(term.Field_Renamed);
			}
			Output.WriteVInt(suffix);
			Output.WriteBytes(term.Bytes_Renamed.Bytes, term.Bytes_Renamed.Offset + prefix, suffix);
			LastTerm.Bytes_Renamed.CopyBytes(term.Bytes_Renamed);
			LastTerm.Field_Renamed = term.Field_Renamed;
		  }
		  catch (IOException e)
		  {
			throw new Exception(e.Message, e);
		  }
		}

		/// <summary>
		/// return finalized form </summary>
		public virtual PrefixCodedTerms Finish()
		{
		  try
		  {
			Output.Close();
			return new PrefixCodedTerms(Buffer);
		  }
		  catch (IOException e)
		  {
              throw new Exception(e.Message, e);
		  }
		}

		internal virtual int SharedPrefix(BytesRef term1, BytesRef term2)
		{
		  int pos1 = 0;
		  int pos1End = pos1 + Math.Min(term1.Length, term2.Length);
		  int pos2 = 0;
		  while (pos1 < pos1End)
		  {
			if (term1.Bytes[term1.Offset + pos1] != term2.Bytes[term2.Offset + pos2])
			{
			  return pos1;
			}
			pos1++;
			pos2++;
		  }
		  return pos1;
		}
	  }
	}

}