/*
 * Copyright 2005 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
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
using Analyzer = Lucene.Net.Analysis.Analyzer;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Similarity = Lucene.Net.Search.Similarity;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Index
{
	
	class DocHelper
	{
		public const System.String FIELD_1_TEXT = "field one text";
		public const System.String TEXT_FIELD_1_KEY = "textField1";
		public static Field textField1;
		
		public const System.String FIELD_2_TEXT = "field field field two text";
		//Fields will be lexicographically sorted.  So, the order is: field, text, two
		public static readonly int[] FIELD_2_FREQS = new int[]{3, 1, 1};
		public const System.String TEXT_FIELD_2_KEY = "textField2";
		public static Field textField2;
		
		public const System.String FIELD_3_TEXT = "aaaNoNorms aaaNoNorms bbbNoNorms";
		public const System.String TEXT_FIELD_3_KEY = "textField3";
		public static Field textField3;
		
		public const System.String KEYWORD_TEXT = "Keyword";
		public const System.String KEYWORD_FIELD_KEY = "keyField";
		public static Field keyField;
		
		public const System.String NO_NORMS_TEXT = "omitNormsText";
		public const System.String NO_NORMS_KEY = "omitNorms";
		public static Field noNormsField;
		
		public const System.String UNINDEXED_FIELD_TEXT = "unindexed field text";
		public const System.String UNINDEXED_FIELD_KEY = "unIndField";
		public static Field unIndField;
		
		
		public const System.String UNSTORED_1_FIELD_TEXT = "unstored field text";
		public const System.String UNSTORED_FIELD_1_KEY = "unStoredField1";
		public static Field unStoredField1;
		
		public const System.String UNSTORED_2_FIELD_TEXT = "unstored field text";
		public const System.String UNSTORED_FIELD_2_KEY = "unStoredField2";
		public static Field unStoredField2;
		
		public static System.Collections.IDictionary nameValues = null;
		
		// ordered list of all the fields...
		// could use LinkedHashMap for this purpose if Java1.4 is OK
		public static Field[] fields = new Field[]{textField1, textField2, textField3, keyField, noNormsField, unIndField, unStoredField1, unStoredField2};
		
		// Map<String fieldName, Field field>
		public static System.Collections.IDictionary all = new System.Collections.Hashtable();
		public static System.Collections.IDictionary indexed = new System.Collections.Hashtable();
		public static System.Collections.IDictionary stored = new System.Collections.Hashtable();
		public static System.Collections.IDictionary unstored = new System.Collections.Hashtable();
		public static System.Collections.IDictionary unindexed = new System.Collections.Hashtable();
		public static System.Collections.IDictionary termvector = new System.Collections.Hashtable();
		public static System.Collections.IDictionary notermvector = new System.Collections.Hashtable();
		public static System.Collections.IDictionary noNorms = new System.Collections.Hashtable();
		
		
		private static void  Add(System.Collections.IDictionary map, Field field)
		{
			map[field.Name()] = field;
		}
		
		/// <summary> Adds the fields above to a document </summary>
		/// <param name="doc">The document to write
		/// </param>
		public static void  SetupDoc(Lucene.Net.Documents.Document doc)
		{
			for (int i = 0; i < fields.Length; i++)
			{
				doc.Add(fields[i]);
			}
		}
		
		/// <summary> Writes the document to the directory using a segment named "test"</summary>
		/// <param name="dir">
		/// </param>
		/// <param name="doc">
		/// </param>
		/// <throws>  IOException </throws>
		public static void  WriteDoc(Directory dir, Lucene.Net.Documents.Document doc)
		{
			WriteDoc(dir, "test", doc);
		}
		
		/// <summary> Writes the document to the directory in the given segment</summary>
		/// <param name="dir">
		/// </param>
		/// <param name="segment">
		/// </param>
		/// <param name="doc">
		/// </param>
		/// <throws>  IOException </throws>
		public static void  WriteDoc(Directory dir, System.String segment, Lucene.Net.Documents.Document doc)
		{
			Similarity similarity = Similarity.GetDefault();
			WriteDoc(dir, new WhitespaceAnalyzer(), similarity, segment, doc);
		}
		
		/// <summary> Writes the document to the directory segment named "test" using the specified analyzer and similarity</summary>
		/// <param name="dir">
		/// </param>
		/// <param name="analyzer">
		/// </param>
		/// <param name="similarity">
		/// </param>
		/// <param name="doc">
		/// </param>
		/// <throws>  IOException </throws>
		public static void  WriteDoc(Directory dir, Analyzer analyzer, Similarity similarity, Lucene.Net.Documents.Document doc)
		{
			WriteDoc(dir, analyzer, similarity, "test", doc);
		}
		
		/// <summary> Writes the document to the directory segment using the analyzer and the similarity score</summary>
		/// <param name="dir">
		/// </param>
		/// <param name="analyzer">
		/// </param>
		/// <param name="similarity">
		/// </param>
		/// <param name="segment">
		/// </param>
		/// <param name="doc">
		/// </param>
		/// <throws>  IOException </throws>
		public static void  WriteDoc(Directory dir, Analyzer analyzer, Similarity similarity, System.String segment, Lucene.Net.Documents.Document doc)
		{
			DocumentWriter writer = new DocumentWriter(dir, analyzer, similarity, 50);
			writer.AddDocument(segment, doc);
		}
		
		public static int NumFields(Lucene.Net.Documents.Document doc)
		{
			int result = 0;
			foreach (Field field in doc.Fields())
			{
				System.String name = field.Name();
				name += ""; // avoid compiler warning
				result++;
			}
			return result;
		}

        static DocHelper()
		{
            textField1 = new Field(TEXT_FIELD_1_KEY, FIELD_1_TEXT, Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.NO);
            fields[0] = textField1;
            textField2 = new Field(TEXT_FIELD_2_KEY, FIELD_2_TEXT, Field.Store.YES, Field.Index.TOKENIZED, Field.TermVector.WITH_POSITIONS_OFFSETS);
            fields[1] = textField2;
            textField3 = new Field(TEXT_FIELD_3_KEY, FIELD_3_TEXT, Field.Store.YES, Field.Index.TOKENIZED);
            fields[2] = textField3;
            {
                textField3.SetOmitNorms(true);
            }
            keyField = new Field(KEYWORD_FIELD_KEY, KEYWORD_TEXT, Field.Store.YES, Field.Index.UN_TOKENIZED);
            fields[3] = keyField;
            noNormsField = new Field(NO_NORMS_KEY, NO_NORMS_TEXT, Field.Store.YES, Field.Index.NO_NORMS);
            fields[4] = noNormsField;
            unIndField = new Field(UNINDEXED_FIELD_KEY, UNINDEXED_FIELD_TEXT, Field.Store.YES, Field.Index.NO);
            fields[5] = unIndField;
            unStoredField1 = new Field(UNSTORED_FIELD_1_KEY, UNSTORED_1_FIELD_TEXT, Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.NO);
            fields[6] = unStoredField1;
            unStoredField2 = new Field(UNSTORED_FIELD_2_KEY, UNSTORED_2_FIELD_TEXT, Field.Store.NO, Field.Index.TOKENIZED, Field.TermVector.YES);
            fields[7] = unStoredField2;
			{
				for (int i = 0; i < fields.Length; i++)
				{
					Field f = fields[i];
					Add(all, f);
					if (f.IsIndexed())
						Add(indexed, f);
					else
						Add(unindexed, f);
					if (f.IsTermVectorStored())
						Add(termvector, f);
					if (f.IsIndexed() && !f.IsTermVectorStored())
						Add(notermvector, f);
					if (f.IsStored())
						Add(stored, f);
					else
						Add(unstored, f);
					if (f.GetOmitNorms())
						Add(noNorms, f);
				}
			}
			{
				nameValues = new System.Collections.Hashtable();
				nameValues[TEXT_FIELD_1_KEY] = FIELD_1_TEXT;
				nameValues[TEXT_FIELD_2_KEY] = FIELD_2_TEXT;
				nameValues[TEXT_FIELD_3_KEY] = FIELD_3_TEXT;
				nameValues[KEYWORD_FIELD_KEY] = KEYWORD_TEXT;
				nameValues[NO_NORMS_KEY] = NO_NORMS_TEXT;
				nameValues[UNINDEXED_FIELD_KEY] = UNINDEXED_FIELD_TEXT;
				nameValues[UNSTORED_FIELD_1_KEY] = UNSTORED_1_FIELD_TEXT;
				nameValues[UNSTORED_FIELD_2_KEY] = UNSTORED_2_FIELD_TEXT;
			}
		}
	}
}