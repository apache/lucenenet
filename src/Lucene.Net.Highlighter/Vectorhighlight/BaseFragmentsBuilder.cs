/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Document;
using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Vectorhighlight;
using Sharpen;

namespace Lucene.Net.Search.Vectorhighlight
{
	/// <summary>
	/// Base FragmentsBuilder implementation that supports colored pre/post
	/// tags and multivalued fields.
	/// </summary>
	/// <remarks>
	/// Base FragmentsBuilder implementation that supports colored pre/post
	/// tags and multivalued fields.
	/// <p>
	/// Uses
	/// <see cref="BoundaryScanner">BoundaryScanner</see>
	/// to determine fragments.
	/// </remarks>
	public abstract class BaseFragmentsBuilder : FragmentsBuilder
	{
		protected internal string[] preTags;

		protected internal string[] postTags;

		public static readonly string[] COLORED_PRE_TAGS = new string[] { "<b style=\"background:yellow\">"
			, "<b style=\"background:lawngreen\">", "<b style=\"background:aquamarine\">", "<b style=\"background:magenta\">"
			, "<b style=\"background:palegreen\">", "<b style=\"background:coral\">", "<b style=\"background:wheat\">"
			, "<b style=\"background:khaki\">", "<b style=\"background:lime\">", "<b style=\"background:deepskyblue\">"
			, "<b style=\"background:deeppink\">", "<b style=\"background:salmon\">", "<b style=\"background:peachpuff\">"
			, "<b style=\"background:violet\">", "<b style=\"background:mediumpurple\">", "<b style=\"background:palegoldenrod\">"
			, "<b style=\"background:darkkhaki\">", "<b style=\"background:springgreen\">", 
			"<b style=\"background:turquoise\">", "<b style=\"background:powderblue\">" };

		public static readonly string[] COLORED_POST_TAGS = new string[] { "</b>" };

		private char multiValuedSeparator = ' ';

		private readonly BoundaryScanner boundaryScanner;

		private bool discreteMultiValueHighlighting = false;

		public BaseFragmentsBuilder() : this(new string[] { "<b>" }, new string[] { "</b>"
			 })
		{
		}

		protected internal BaseFragmentsBuilder(string[] preTags, string[] postTags) : this
			(preTags, postTags, new SimpleBoundaryScanner())
		{
		}

		protected internal BaseFragmentsBuilder(BoundaryScanner boundaryScanner) : this(new 
			string[] { "<b>" }, new string[] { "</b>" }, boundaryScanner)
		{
		}

		protected internal BaseFragmentsBuilder(string[] preTags, string[] postTags, BoundaryScanner
			 boundaryScanner)
		{
			this.preTags = preTags;
			this.postTags = postTags;
			this.boundaryScanner = boundaryScanner;
		}

		internal static object CheckTagsArgument(object tags)
		{
			if (tags is string)
			{
				return tags;
			}
			else
			{
				if (tags is string[])
				{
					return tags;
				}
			}
			throw new ArgumentException("type of preTags/postTags must be a String or String[]"
				);
		}

		public abstract IList<FieldFragList.WeightedFragInfo> GetWeightedFragInfoList(IList
			<FieldFragList.WeightedFragInfo> src);

		private static readonly Encoder NULL_ENCODER = new DefaultEncoder();

		/// <exception cref="System.IO.IOException"></exception>
		public virtual string CreateFragment(IndexReader reader, int docId, string fieldName
			, FieldFragList fieldFragList)
		{
			return CreateFragment(reader, docId, fieldName, fieldFragList, preTags, postTags, 
				NULL_ENCODER);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual string[] CreateFragments(IndexReader reader, int docId, string fieldName
			, FieldFragList fieldFragList, int maxNumFragments)
		{
			return CreateFragments(reader, docId, fieldName, fieldFragList, maxNumFragments, 
				preTags, postTags, NULL_ENCODER);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual string CreateFragment(IndexReader reader, int docId, string fieldName
			, FieldFragList fieldFragList, string[] preTags, string[] postTags, Encoder encoder
			)
		{
			string[] fragments = CreateFragments(reader, docId, fieldName, fieldFragList, 1, 
				preTags, postTags, encoder);
			if (fragments == null || fragments.Length == 0)
			{
				return null;
			}
			return fragments[0];
		}

		/// <exception cref="System.IO.IOException"></exception>
		public virtual string[] CreateFragments(IndexReader reader, int docId, string fieldName
			, FieldFragList fieldFragList, int maxNumFragments, string[] preTags, string[] postTags
			, Encoder encoder)
		{
			if (maxNumFragments < 0)
			{
				throw new ArgumentException("maxNumFragments(" + maxNumFragments + ") must be positive number."
					);
			}
			IList<FieldFragList.WeightedFragInfo> fragInfos = fieldFragList.GetFragInfos();
			Field[] values = GetFields(reader, docId, fieldName);
			if (values.Length == 0)
			{
				return null;
			}
			if (discreteMultiValueHighlighting && values.Length > 1)
			{
				fragInfos = DiscreteMultiValueHighlighting(fragInfos, values);
			}
			fragInfos = GetWeightedFragInfoList(fragInfos);
			int limitFragments = maxNumFragments < fragInfos.Count ? maxNumFragments : fragInfos
				.Count;
			IList<string> fragments = new AList<string>(limitFragments);
			StringBuilder buffer = new StringBuilder();
			int[] nextValueIndex = new int[] { 0 };
			for (int n = 0; n < limitFragments; n++)
			{
				FieldFragList.WeightedFragInfo fragInfo = fragInfos[n];
				fragments.AddItem(MakeFragment(buffer, nextValueIndex, values, fragInfo, preTags, 
					postTags, encoder));
			}
			return Sharpen.Collections.ToArray(fragments, new string[fragments.Count]);
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal virtual Field[] GetFields(IndexReader reader, int docId, string
			 fieldName)
		{
			// according to javadoc, doc.getFields(fieldName) cannot be used with lazy loaded field???
			IList<Field> fields = new AList<Field>();
			reader.Document(docId, new _StoredFieldVisitor_152(fields, fieldName));
			return Sharpen.Collections.ToArray(fields, new Field[fields.Count]);
		}

		private sealed class _StoredFieldVisitor_152 : StoredFieldVisitor
		{
			public _StoredFieldVisitor_152(IList<Field> fields, string fieldName)
			{
				this.fields = fields;
				this.fieldName = fieldName;
			}

			public override void StringField(FieldInfo fieldInfo, string value)
			{
				FieldType ft = new FieldType(TextField.TYPE_STORED);
				ft.SetStoreTermVectors(fieldInfo.HasVectors());
				fields.AddItem(new Field(fieldInfo.name, value, ft));
			}

			public override StoredFieldVisitor.Status NeedsField(FieldInfo fieldInfo)
			{
				return fieldInfo.name.Equals(fieldName) ? StoredFieldVisitor.Status.YES : StoredFieldVisitor.Status
					.NO;
			}

			private readonly IList<Field> fields;

			private readonly string fieldName;
		}

		protected internal virtual string MakeFragment(StringBuilder buffer, int[] index, 
			Field[] values, FieldFragList.WeightedFragInfo fragInfo, string[] preTags, string
			[] postTags, Encoder encoder)
		{
			StringBuilder fragment = new StringBuilder();
			int s = fragInfo.GetStartOffset();
			int[] modifiedStartOffset = new int[] { s };
			string src = GetFragmentSourceMSO(buffer, index, values, s, fragInfo.GetEndOffset
				(), modifiedStartOffset);
			int srcIndex = 0;
			foreach (FieldFragList.WeightedFragInfo.SubInfo subInfo in fragInfo.GetSubInfos())
			{
				foreach (FieldPhraseList.WeightedPhraseInfo.Toffs to in subInfo.GetTermsOffsets())
				{
					fragment.Append(encoder.EncodeText(Sharpen.Runtime.Substring(src, srcIndex, to.GetStartOffset
						() - modifiedStartOffset[0]))).Append(GetPreTag(preTags, subInfo.GetSeqnum())).Append
						(encoder.EncodeText(Sharpen.Runtime.Substring(src, to.GetStartOffset() - modifiedStartOffset
						[0], to.GetEndOffset() - modifiedStartOffset[0]))).Append(GetPostTag(postTags, subInfo
						.GetSeqnum()));
					srcIndex = to.GetEndOffset() - modifiedStartOffset[0];
				}
			}
			fragment.Append(encoder.EncodeText(Sharpen.Runtime.Substring(src, srcIndex)));
			return fragment.ToString();
		}

		protected internal virtual string GetFragmentSourceMSO(StringBuilder buffer, int[]
			 index, Field[] values, int startOffset, int endOffset, int[] modifiedStartOffset
			)
		{
			while (buffer.Length < endOffset && index[0] < values.Length)
			{
				buffer.Append(values[index[0]++].StringValue());
				buffer.Append(GetMultiValuedSeparator());
			}
			int bufferLength = buffer.Length;
			// we added the multi value char to the last buffer, ignore it
			if (values[index[0] - 1].FieldType().Tokenized())
			{
				bufferLength--;
			}
			int eo = bufferLength < endOffset ? bufferLength : boundaryScanner.FindEndOffset(
				buffer, endOffset);
			modifiedStartOffset[0] = boundaryScanner.FindStartOffset(buffer, startOffset);
			return buffer.Substring(modifiedStartOffset[0], eo);
		}

		protected internal virtual string GetFragmentSource(StringBuilder buffer, int[] index
			, Field[] values, int startOffset, int endOffset)
		{
			while (buffer.Length < endOffset && index[0] < values.Length)
			{
				buffer.Append(values[index[0]].StringValue());
				buffer.Append(multiValuedSeparator);
				index[0]++;
			}
			int eo = buffer.Length < endOffset ? buffer.Length : endOffset;
			return buffer.Substring(startOffset, eo);
		}

		protected internal virtual IList<FieldFragList.WeightedFragInfo> DiscreteMultiValueHighlighting
			(IList<FieldFragList.WeightedFragInfo> fragInfos, Field[] fields)
		{
			IDictionary<string, IList<FieldFragList.WeightedFragInfo>> fieldNameToFragInfos = 
				new Dictionary<string, IList<FieldFragList.WeightedFragInfo>>();
			foreach (Field field in fields)
			{
				fieldNameToFragInfos.Put(field.Name(), new AList<FieldFragList.WeightedFragInfo>(
					));
			}
			foreach (FieldFragList.WeightedFragInfo fragInfo in fragInfos)
			{
				int fieldStart;
				int fieldEnd = 0;
				foreach (Field field_1 in fields)
				{
					if (field_1.StringValue().IsEmpty())
					{
						fieldEnd++;
						continue;
					}
					fieldStart = fieldEnd;
					fieldEnd += field_1.StringValue().Length + 1;
					// + 1 for going to next field with same name.
					if (fragInfo.GetStartOffset() >= fieldStart && fragInfo.GetEndOffset() >= fieldStart
						 && fragInfo.GetStartOffset() <= fieldEnd && fragInfo.GetEndOffset() <= fieldEnd)
					{
						fieldNameToFragInfos.Get(field_1.Name()).AddItem(fragInfo);
						goto fragInfos_continue;
					}
					if (fragInfo.GetSubInfos().IsEmpty())
					{
						goto fragInfos_continue;
					}
					FieldPhraseList.WeightedPhraseInfo.Toffs firstToffs = fragInfo.GetSubInfos()[0].GetTermsOffsets
						()[0];
					if (fragInfo.GetStartOffset() >= fieldEnd || firstToffs.GetStartOffset() >= fieldEnd)
					{
						continue;
					}
					int fragStart = fieldStart;
					if (fragInfo.GetStartOffset() > fieldStart && fragInfo.GetStartOffset() < fieldEnd)
					{
						fragStart = fragInfo.GetStartOffset();
					}
					int fragEnd = fieldEnd;
					if (fragInfo.GetEndOffset() > fieldStart && fragInfo.GetEndOffset() < fieldEnd)
					{
						fragEnd = fragInfo.GetEndOffset();
					}
					IList<FieldFragList.WeightedFragInfo.SubInfo> subInfos = new AList<FieldFragList.WeightedFragInfo.SubInfo
						>();
					Iterator<FieldFragList.WeightedFragInfo.SubInfo> subInfoIterator = fragInfo.GetSubInfos
						().Iterator();
					float boost = 0.0f;
					//  The boost of the new info will be the sum of the boosts of its SubInfos
					while (subInfoIterator.HasNext())
					{
						FieldFragList.WeightedFragInfo.SubInfo subInfo = subInfoIterator.Next();
						IList<FieldPhraseList.WeightedPhraseInfo.Toffs> toffsList = new AList<FieldPhraseList.WeightedPhraseInfo.Toffs
							>();
						Iterator<FieldPhraseList.WeightedPhraseInfo.Toffs> toffsIterator = subInfo.GetTermsOffsets
							().Iterator();
						while (toffsIterator.HasNext())
						{
							FieldPhraseList.WeightedPhraseInfo.Toffs toffs = toffsIterator.Next();
							if (toffs.GetStartOffset() >= fieldStart && toffs.GetEndOffset() <= fieldEnd)
							{
								toffsList.AddItem(toffs);
								toffsIterator.Remove();
							}
						}
						if (!toffsList.IsEmpty())
						{
							subInfos.AddItem(new FieldFragList.WeightedFragInfo.SubInfo(subInfo.GetText(), toffsList
								, subInfo.GetSeqnum(), subInfo.GetBoost()));
							boost += subInfo.GetBoost();
						}
						if (subInfo.GetTermsOffsets().IsEmpty())
						{
							subInfoIterator.Remove();
						}
					}
					FieldFragList.WeightedFragInfo weightedFragInfo = new FieldFragList.WeightedFragInfo
						(fragStart, fragEnd, subInfos, boost);
					fieldNameToFragInfos.Get(field_1.Name()).AddItem(weightedFragInfo);
				}
			}
fragInfos_break: ;
			IList<FieldFragList.WeightedFragInfo> result = new AList<FieldFragList.WeightedFragInfo
				>();
			foreach (IList<FieldFragList.WeightedFragInfo> weightedFragInfos in fieldNameToFragInfos
				.Values)
			{
				Sharpen.Collections.AddAll(result, weightedFragInfos);
			}
			result.Sort(new _IComparer_293());
			return result;
		}

		private sealed class _IComparer_293 : IComparer<FieldFragList.WeightedFragInfo>
		{
			public _IComparer_293()
			{
			}

			public int Compare(FieldFragList.WeightedFragInfo info1, FieldFragList.WeightedFragInfo
				 info2)
			{
				return info1.GetStartOffset() - info2.GetStartOffset();
			}
		}

		public virtual void SetMultiValuedSeparator(char separator)
		{
			multiValuedSeparator = separator;
		}

		public virtual char GetMultiValuedSeparator()
		{
			return multiValuedSeparator;
		}

		public virtual bool IsDiscreteMultiValueHighlighting()
		{
			return discreteMultiValueHighlighting;
		}

		public virtual void SetDiscreteMultiValueHighlighting(bool discreteMultiValueHighlighting
			)
		{
			this.discreteMultiValueHighlighting = discreteMultiValueHighlighting;
		}

		protected internal virtual string GetPreTag(int num)
		{
			return GetPreTag(preTags, num);
		}

		protected internal virtual string GetPostTag(int num)
		{
			return GetPostTag(postTags, num);
		}

		protected internal virtual string GetPreTag(string[] preTags, int num)
		{
			int n = num % preTags.Length;
			return preTags[n];
		}

		protected internal virtual string GetPostTag(string[] postTags, int num)
		{
			int n = num % postTags.Length;
			return postTags[n];
		}
	}
}
