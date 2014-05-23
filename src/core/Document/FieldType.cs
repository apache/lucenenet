using System.Text;

namespace Lucene.Net.Document
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

	using Analyzer = Lucene.Net.Analysis.Analyzer; // javadocs
	using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
	using IndexOptions = Lucene.Net.Index.FieldInfo.IndexOptions_e;
	using IndexableFieldType = Lucene.Net.Index.IndexableFieldType;
	using Lucene.Net.Search; // javadocs
	using NumericUtils = Lucene.Net.Util.NumericUtils;

	/// <summary>
	/// Describes the properties of a field.
	/// </summary>
	public class FieldType : IndexableFieldType
	{

	  /// <summary>
	  /// Data type of the numeric value
	  /// @since 3.2
	  /// </summary>
	  public enum NumericType
	  {
		/// <summary>
		/// 32-bit integer numeric type </summary>
		INT,
		/// <summary>
		/// 64-bit long numeric type </summary>
		LONG,
		/// <summary>
		/// 32-bit float numeric type </summary>
		FLOAT,
		/// <summary>
		/// 64-bit double numeric type </summary>
		DOUBLE
	  }

	  private bool Indexed_Renamed;
	  private bool Stored_Renamed;
	  private bool Tokenized_Renamed = true;
	  private bool StoreTermVectors_Renamed;
	  private bool StoreTermVectorOffsets_Renamed;
	  private bool StoreTermVectorPositions_Renamed;
	  private bool StoreTermVectorPayloads_Renamed;
	  private bool OmitNorms_Renamed;
	  private IndexOptions IndexOptions_Renamed = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS;
	  private NumericType NumericType_Renamed;
	  private bool Frozen;
	  private int NumericPrecisionStep_Renamed = NumericUtils.PRECISION_STEP_DEFAULT;
	  private DocValuesType DocValueType_Renamed;

	  /// <summary>
	  /// Create a new mutable FieldType with all of the properties from <code>ref</code>
	  /// </summary>
	  public FieldType(FieldType @ref)
	  {
		this.Indexed_Renamed = @ref.Indexed();
		this.Stored_Renamed = @ref.Stored();
		this.Tokenized_Renamed = @ref.Tokenized();
		this.StoreTermVectors_Renamed = @ref.StoreTermVectors();
		this.StoreTermVectorOffsets_Renamed = @ref.StoreTermVectorOffsets();
		this.StoreTermVectorPositions_Renamed = @ref.StoreTermVectorPositions();
		this.StoreTermVectorPayloads_Renamed = @ref.StoreTermVectorPayloads();
		this.OmitNorms_Renamed = @ref.OmitNorms();
		this.IndexOptions_Renamed = @ref.IndexOptions();
		this.DocValueType_Renamed = @ref.DocValueType();
		this.NumericType_Renamed = @ref.NumericType();
		// Do not copy frozen!
	  }

	  /// <summary>
	  /// Create a new FieldType with default properties.
	  /// </summary>
	  public FieldType()
	  {
	  }

	  private void CheckIfFrozen()
	  {
		if (Frozen)
		{
		  throw new IllegalStateException("this FieldType is already frozen and cannot be changed");
		}
	  }

	  /// <summary>
	  /// Prevents future changes. Note, it is recommended that this is called once
	  /// the FieldTypes's properties have been set, to prevent unintentional state
	  /// changes.
	  /// </summary>
	  public virtual void Freeze()
	  {
		this.Frozen = true;
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>false</code>. </summary>
	  /// <seealso cref= #setIndexed(boolean) </seealso>
	  public override bool Indexed()
	  {
		return this.Indexed_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> to index (invert) this field. </summary>
	  /// <param name="value"> true if this field should be indexed. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #indexed() </seealso>
	  public virtual bool Indexed
	  {
		  set
		  {
			CheckIfFrozen();
			this.Indexed_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>false</code>. </summary>
	  /// <seealso cref= #setStored(boolean) </seealso>
	  public override bool Stored()
	  {
		return this.Stored_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> to store this field. </summary>
	  /// <param name="value"> true if this field should be stored. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #stored() </seealso>
	  public virtual bool Stored
	  {
		  set
		  {
			CheckIfFrozen();
			this.Stored_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>true</code>. </summary>
	  /// <seealso cref= #setTokenized(boolean) </seealso>
	  public override bool Tokenized()
	  {
		return this.Tokenized_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> to tokenize this field's contents via the 
	  /// configured <seealso cref="Analyzer"/>. </summary>
	  /// <param name="value"> true if this field should be tokenized. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #tokenized() </seealso>
	  public virtual bool Tokenized
	  {
		  set
		  {
			CheckIfFrozen();
			this.Tokenized_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>false</code>. </summary>
	  /// <seealso cref= #setStoreTermVectors(boolean) </seealso>
	  public override bool StoreTermVectors()
	  {
		return this.StoreTermVectors_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> if this field's indexed form should be also stored 
	  /// into term vectors. </summary>
	  /// <param name="value"> true if this field should store term vectors. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #storeTermVectors() </seealso>
	  public virtual bool StoreTermVectors
	  {
		  set
		  {
			CheckIfFrozen();
			this.StoreTermVectors_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>false</code>. </summary>
	  /// <seealso cref= #setStoreTermVectorOffsets(boolean) </seealso>
	  public override bool StoreTermVectorOffsets()
	  {
		return this.StoreTermVectorOffsets_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> to also store token character offsets into the term
	  /// vector for this field. </summary>
	  /// <param name="value"> true if this field should store term vector offsets. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #storeTermVectorOffsets() </seealso>
	  public virtual bool StoreTermVectorOffsets
	  {
		  set
		  {
			CheckIfFrozen();
			this.StoreTermVectorOffsets_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>false</code>. </summary>
	  /// <seealso cref= #setStoreTermVectorPositions(boolean) </seealso>
	  public override bool StoreTermVectorPositions()
	  {
		return this.StoreTermVectorPositions_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> to also store token positions into the term
	  /// vector for this field. </summary>
	  /// <param name="value"> true if this field should store term vector positions. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #storeTermVectorPositions() </seealso>
	  public virtual bool StoreTermVectorPositions
	  {
		  set
		  {
			CheckIfFrozen();
			this.StoreTermVectorPositions_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>false</code>. </summary>
	  /// <seealso cref= #setStoreTermVectorPayloads(boolean)  </seealso>
	  public override bool StoreTermVectorPayloads()
	  {
		return this.StoreTermVectorPayloads_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> to also store token payloads into the term
	  /// vector for this field. </summary>
	  /// <param name="value"> true if this field should store term vector payloads. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #storeTermVectorPayloads() </seealso>
	  public virtual bool StoreTermVectorPayloads
	  {
		  set
		  {
			CheckIfFrozen();
			this.StoreTermVectorPayloads_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>false</code>. </summary>
	  /// <seealso cref= #setOmitNorms(boolean) </seealso>
	  public override bool OmitNorms()
	  {
		return this.OmitNorms_Renamed;
	  }

	  /// <summary>
	  /// Set to <code>true</code> to omit normalization values for the field. </summary>
	  /// <param name="value"> true if this field should omit norms. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #omitNorms() </seealso>
	  public virtual bool OmitNorms
	  {
		  set
		  {
			CheckIfFrozen();
			this.OmitNorms_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <seealso cref="IndexOptions#DOCS_AND_FREQS_AND_POSITIONS"/>. </summary>
	  /// <seealso cref= #setIndexOptions(Lucene.Net.Index.FieldInfo.IndexOptions) </seealso>
	  public override IndexOptions IndexOptions()
	  {
		return this.IndexOptions_Renamed;
	  }

	  /// <summary>
	  /// Sets the indexing options for the field: </summary>
	  /// <param name="value"> indexing options </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #indexOptions() </seealso>
	  public virtual IndexOptions IndexOptions
	  {
		  set
		  {
			CheckIfFrozen();
			this.IndexOptions_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Specifies the field's numeric type. </summary>
	  /// <param name="type"> numeric type, or null if the field has no numeric type. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #numericType() </seealso>
	  public virtual NumericType NumericType
	  {
		  set
		  {
			CheckIfFrozen();
			NumericType_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// NumericType: if non-null then the field's value will be indexed
	  /// numerically so that <seealso cref="NumericRangeQuery"/> can be used at 
	  /// search time. 
	  /// <p>
	  /// The default is <code>null</code> (no numeric type) </summary>
	  /// <seealso cref= #setNumericType(NumericType) </seealso>
	  public virtual NumericType NumericType()
	  {
		return NumericType_Renamed;
	  }

	  /// <summary>
	  /// Sets the numeric precision step for the field. </summary>
	  /// <param name="precisionStep"> numeric precision step for the field </param>
	  /// <exception cref="IllegalArgumentException"> if precisionStep is less than 1. </exception>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #numericPrecisionStep() </seealso>
	  public virtual int NumericPrecisionStep
	  {
		  set
		  {
			CheckIfFrozen();
			if (value < 1)
			{
			  throw new System.ArgumentException("precisionStep must be >= 1 (got " + value + ")");
			}
			this.NumericPrecisionStep_Renamed = value;
		  }
	  }

	  /// <summary>
	  /// Precision step for numeric field. 
	  /// <p>
	  /// this has no effect if <seealso cref="#numericType()"/> returns null.
	  /// <p>
	  /// The default is <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> </summary>
	  /// <seealso cref= #setNumericPrecisionStep(int) </seealso>
	  public virtual int NumericPrecisionStep()
	  {
		return NumericPrecisionStep_Renamed;
	  }

	  /// <summary>
	  /// Prints a Field for human consumption. </summary>
	  public override sealed string ToString()
	  {
		StringBuilder result = new StringBuilder();
		if (Stored())
		{
		  result.Append("stored");
		}
		if (Indexed())
		{
		  if (result.Length > 0)
		  {
			result.Append(",");
		  }
		  result.Append("indexed");
		  if (Tokenized())
		  {
			result.Append(",tokenized");
		  }
		  if (StoreTermVectors())
		  {
			result.Append(",termVector");
		  }
		  if (StoreTermVectorOffsets())
		  {
			result.Append(",termVectorOffsets");
		  }
		  if (StoreTermVectorPositions())
		  {
			result.Append(",termVectorPosition");
			if (StoreTermVectorPayloads())
			{
			  result.Append(",termVectorPayloads");
			}
		  }
		  if (OmitNorms())
		  {
			result.Append(",omitNorms");
		  }
		  if (IndexOptions_Renamed != IndexOptions.DOCS_AND_FREQS_AND_POSITIONS)
		  {
			result.Append(",indexOptions=");
			result.Append(IndexOptions_Renamed);
		  }
		  if (NumericType_Renamed != null)
		  {
			result.Append(",numericType=");
			result.Append(NumericType_Renamed);
			result.Append(",numericPrecisionStep=");
			result.Append(NumericPrecisionStep_Renamed);
		  }
		}
		if (DocValueType_Renamed != null)
		{
		  if (result.Length > 0)
		  {
			result.Append(",");
		  }
		  result.Append("docValueType=");
		  result.Append(DocValueType_Renamed);
		}

		return result.ToString();
	  }

	  /// <summary>
	  /// {@inheritDoc}
	  /// <p>
	  /// The default is <code>null</code> (no docValues) </summary>
	  /// <seealso cref= #setDocValueType(Lucene.Net.Index.FieldInfo.DocValuesType) </seealso>
	  /*public override DocValuesType DocValueType()
	  {
		return DocValueType_Renamed;
	  }*/

	  /// <summary>
	  /// Set's the field's DocValuesType </summary>
	  /// <param name="type"> DocValues type, or null if no DocValues should be stored. </param>
	  /// <exception cref="IllegalStateException"> if this FieldType is frozen against
	  ///         future modifications. </exception>
	  /// <seealso cref= #docValueType() </seealso>
	  public virtual DocValuesType DocValueType
	  {
          get
          {
              return DocValueType_Renamed;
          }
          
          set
		  {
			CheckIfFrozen();
			DocValueType_Renamed = value;
		  }
	  }
	}

}