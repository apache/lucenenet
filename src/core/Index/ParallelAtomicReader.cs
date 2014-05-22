using System.Collections.Generic;
using System.Text;

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


	using Bits = Lucene.Net.Util.Bits;


	/// <summary>
	/// An <seealso cref="AtomicReader"/> which reads multiple, parallel indexes.  Each index
	/// added must have the same number of documents, but typically each contains
	/// different fields. Deletions are taken from the first reader.
	/// Each document contains the union of the fields of all documents
	/// with the same document number.  When searching, matches for a
	/// query term are from the first index added that has the field.
	/// 
	/// <p>this is useful, e.g., with collections that have large fields which
	/// change rarely and small fields that change more frequently.  The smaller
	/// fields may be re-indexed in a new index and both indexes may be searched
	/// together.
	/// 
	/// <p><strong>Warning:</strong> It is up to you to make sure all indexes
	/// are created and modified the same way. For example, if you add
	/// documents to one index, you need to add the same documents in the
	/// same order to the other indexes. <em>Failure to do so will result in
	/// undefined behavior</em>.
	/// </summary>
	public class ParallelAtomicReader : AtomicReader
	{
		private bool InstanceFieldsInitialized = false;

		private void InitializeInstanceFields()
		{
			Fields_Renamed = new ParallelFields(this);
		}

	  private readonly FieldInfos FieldInfos_Renamed;
	  private ParallelFields Fields_Renamed;
	  private readonly AtomicReader[] ParallelReaders, StoredFieldsReaders;
	  private readonly Set<AtomicReader> CompleteReaderSet = Collections.newSetFromMap(new IdentityHashMap<AtomicReader, bool?>());
	  private readonly bool CloseSubReaders;
	  private readonly int MaxDoc_Renamed, NumDocs_Renamed;
	  private readonly bool HasDeletions;
	  private readonly SortedMap<string, AtomicReader> FieldToReader = new SortedDictionary<string, AtomicReader>();
	  private readonly SortedMap<string, AtomicReader> TvFieldToReader = new SortedDictionary<string, AtomicReader>();

	  /// <summary>
	  /// Create a ParallelAtomicReader based on the provided
	  ///  readers; auto-closes the given readers on <seealso cref="#close()"/>. 
	  /// </summary>
	  public ParallelAtomicReader(params AtomicReader[] readers) : this(true, readers)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
	  }

	  /// <summary>
	  /// Create a ParallelAtomicReader based on the provided
	  ///  readers. 
	  /// </summary>
	  public ParallelAtomicReader(bool closeSubReaders, params AtomicReader[] readers) : this(closeSubReaders, readers, readers)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
	  }

	  /// <summary>
	  /// Expert: create a ParallelAtomicReader based on the provided
	  ///  readers and storedFieldReaders; when a document is
	  ///  loaded, only storedFieldsReaders will be used. 
	  /// </summary>
	  public ParallelAtomicReader(bool closeSubReaders, AtomicReader[] readers, AtomicReader[] storedFieldsReaders)
	  {
		  if (!InstanceFieldsInitialized)
		  {
			  InitializeInstanceFields();
			  InstanceFieldsInitialized = true;
		  }
		this.CloseSubReaders = closeSubReaders;
		if (readers.Length == 0 && storedFieldsReaders.Length > 0)
		{
		  throw new System.ArgumentException("There must be at least one main reader if storedFieldsReaders are used.");
		}
		this.ParallelReaders = readers.clone();
		this.StoredFieldsReaders = storedFieldsReaders.clone();
		if (ParallelReaders.Length > 0)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final AtomicReader first = parallelReaders[0];
		  AtomicReader first = ParallelReaders[0];
		  this.MaxDoc_Renamed = first.MaxDoc();
		  this.NumDocs_Renamed = first.NumDocs();
		  this.HasDeletions = first.HasDeletions();
		}
		else
		{
		  this.MaxDoc_Renamed = this.NumDocs_Renamed = 0;
		  this.HasDeletions = false;
		}
		Collections.addAll(CompleteReaderSet, this.ParallelReaders);
		Collections.addAll(CompleteReaderSet, this.StoredFieldsReaders);

		// check compatibility:
		foreach (AtomicReader reader in CompleteReaderSet)
		{
		  if (reader.MaxDoc() != MaxDoc_Renamed)
		  {
			throw new System.ArgumentException("All readers must have same maxDoc: " + MaxDoc_Renamed + "!=" + reader.MaxDoc());
		  }
		}

		// TODO: make this read-only in a cleaner way?
		FieldInfos.Builder builder = new FieldInfos.Builder();
		// build FieldInfos and fieldToReader map:
		foreach (AtomicReader reader in this.ParallelReaders)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final FieldInfos readerFieldInfos = reader.getFieldInfos();
		  FieldInfos readerFieldInfos = reader.FieldInfos;
		  foreach (FieldInfo fieldInfo in readerFieldInfos)
		  {
			// NOTE: first reader having a given field "wins":
			if (!FieldToReader.containsKey(fieldInfo.Name))
			{
			  builder.Add(fieldInfo);
			  FieldToReader.put(fieldInfo.Name, reader);
			  if (fieldInfo.HasVectors())
			  {
				TvFieldToReader.put(fieldInfo.Name, reader);
			  }
			}
		  }
		}
		FieldInfos_Renamed = builder.Finish();

		// build Fields instance
		foreach (AtomicReader reader in this.ParallelReaders)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Fields readerFields = reader.fields();
		  Fields readerFields = reader.fields();
		  if (readerFields != null)
		  {
			foreach (string field in readerFields)
			{
			  // only add if the reader responsible for that field name is the current:
			  if (FieldToReader.get(field) == reader)
			  {
				this.Fields_Renamed.AddField(field, readerFields.Terms(field));
			  }
			}
		  }
		}

		// do this finally so any Exceptions occurred before don't affect refcounts:
		foreach (AtomicReader reader in CompleteReaderSet)
		{
		  if (!closeSubReaders)
		  {
			reader.IncRef();
		  }
		  reader.RegisterParentReader(this);
		}
	  }

	  public override string ToString()
	  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StringBuilder buffer = new StringBuilder("ParallelAtomicReader(");
		StringBuilder buffer = new StringBuilder("ParallelAtomicReader(");
		for (final IEnumerator<AtomicReader> iter = CompleteReaderSet.GetEnumerator(); iter.hasNext();)
		{
		  buffer.Append(iter.next());
		  if (iter.hasNext())
		  {
			  buffer.Append(", ");
		  }
		}
		return buffer.Append(')').ToString();
	  }

	  // Single instance of this, per ParallelReader instance
	  private sealed class ParallelFields : Fields
	  {
		  private readonly ParallelAtomicReader OuterInstance;

		internal readonly IDictionary<string, Terms> Fields = new SortedDictionary<string, Terms>();

		internal ParallelFields(ParallelAtomicReader outerInstance)
		{
			this.OuterInstance = outerInstance;
		}

		internal void AddField(string fieldName, Terms terms)
		{
		  Fields[fieldName] = terms;
		}

		public override IEnumerator<string> Iterator()
		{
		  return Collections.unmodifiableSet(Fields.Keys).GetEnumerator();
		}

		public override Terms Terms(string field)
		{
		  return Fields[field];
		}

		public override int Size()
		{
		  return Fields.Count;
		}
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// NOTE: the returned field numbers will likely not
	  /// correspond to the actual field numbers in the underlying
	  /// readers, and codec metadata (<seealso cref="FieldInfo#getAttribute(String)"/>
	  /// will be unavailable.
	  /// </summary>
	  public override FieldInfos FieldInfos
	  {
		  get
		  {
			return FieldInfos_Renamed;
		  }
	  }

	  public override Bits LiveDocs
	  {
		  get
		  {
			EnsureOpen();
			return HasDeletions ? ParallelReaders[0].LiveDocs : null;
		  }
	  }

	  public override Fields Fields()
	  {
		EnsureOpen();
		return Fields_Renamed;
	  }

	  public override int NumDocs()
	  {
		// Don't call ensureOpen() here (it could affect performance)
		return NumDocs_Renamed;
	  }

	  public override int MaxDoc()
	  {
		// Don't call ensureOpen() here (it could affect performance)
		return MaxDoc_Renamed;
	  }

	  public override void Document(int docID, StoredFieldVisitor visitor)
	  {
		EnsureOpen();
		foreach (AtomicReader reader in StoredFieldsReaders)
		{
		  reader.document(docID, visitor);
		}
	  }

	  public override Fields GetTermVectors(int docID)
	  {
		EnsureOpen();
		ParallelFields fields = null;
		foreach (KeyValuePair<string, AtomicReader> ent in TvFieldToReader.entrySet())
		{
		  string fieldName = ent.Key;
		  Terms vector = ent.Value.getTermVector(docID, fieldName);
		  if (vector != null)
		  {
			if (fields == null)
			{
			  fields = new ParallelFields(this);
			}
			fields.AddField(fieldName, vector);
		  }
		}

		return fields;
	  }

	  protected internal override void DoClose()
	  {
		  lock (this)
		  {
			IOException ioe = null;
			foreach (AtomicReader reader in CompleteReaderSet)
			{
			  try
			  {
				if (CloseSubReaders)
				{
				  reader.Close();
				}
				else
				{
				  reader.DecRef();
				}
			  }
			  catch (IOException e)
			  {
				if (ioe == null)
				{
					ioe = e;
				}
			  }
			}
			// throw the first exception
			if (ioe != null)
			{
				throw ioe;
			}
		  }
	  }

	  public override NumericDocValues GetNumericDocValues(string field)
	  {
		EnsureOpen();
		AtomicReader reader = FieldToReader.get(field);
		return reader == null ? null : reader.GetNumericDocValues(field);
	  }

	  public override BinaryDocValues GetBinaryDocValues(string field)
	  {
		EnsureOpen();
		AtomicReader reader = FieldToReader.get(field);
		return reader == null ? null : reader.GetBinaryDocValues(field);
	  }

	  public override SortedDocValues GetSortedDocValues(string field)
	  {
		EnsureOpen();
		AtomicReader reader = FieldToReader.get(field);
		return reader == null ? null : reader.GetSortedDocValues(field);
	  }

	  public override SortedSetDocValues GetSortedSetDocValues(string field)
	  {
		EnsureOpen();
		AtomicReader reader = FieldToReader.get(field);
		return reader == null ? null : reader.GetSortedSetDocValues(field);
	  }

	  public override Bits GetDocsWithField(string field)
	  {
		EnsureOpen();
		AtomicReader reader = FieldToReader.get(field);
		return reader == null ? null : reader.GetDocsWithField(field);
	  }

	  public override NumericDocValues GetNormValues(string field)
	  {
		EnsureOpen();
		AtomicReader reader = FieldToReader.get(field);
		NumericDocValues values = reader == null ? null : reader.GetNormValues(field);
		return values;
	  }

	  public override void CheckIntegrity()
	  {
		EnsureOpen();
		foreach (AtomicReader reader in CompleteReaderSet)
		{
		  reader.CheckIntegrity();
		}
	  }
	}

}