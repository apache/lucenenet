using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Toffs = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo.Toffs;
using SubInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo.SubInfo;
using WeightedFragInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo;

namespace Lucene.Net.Search.VectorHighlight
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

    /// <summary>
    /// Base <see cref="IFragmentsBuilder"/> implementation that supports colored pre/post
    /// tags and multivalued fields.
    /// <para/>
    /// Uses <see cref="BoundaryScanner"/> to determine fragments.
    /// </summary>
    public abstract class BaseFragmentsBuilder : IFragmentsBuilder
    {
        protected string[] preTags, postTags;
        public static readonly string[] COLORED_PRE_TAGS = {
            "<b style=\"background:yellow\">", "<b style=\"background:lawngreen\">", "<b style=\"background:aquamarine\">",
            "<b style=\"background:magenta\">", "<b style=\"background:palegreen\">", "<b style=\"background:coral\">",
            "<b style=\"background:wheat\">", "<b style=\"background:khaki\">", "<b style=\"background:lime\">",
            "<b style=\"background:deepskyblue\">", "<b style=\"background:deeppink\">", "<b style=\"background:salmon\">",
            "<b style=\"background:peachpuff\">", "<b style=\"background:violet\">", "<b style=\"background:mediumpurple\">",
            "<b style=\"background:palegoldenrod\">", "<b style=\"background:darkkhaki\">", "<b style=\"background:springgreen\">",
            "<b style=\"background:turquoise\">", "<b style=\"background:powderblue\">"
        };
        public static readonly string[] COLORED_POST_TAGS = { "</b>" };
        private char multiValuedSeparator = ' ';
        private readonly IBoundaryScanner boundaryScanner;
        private bool discreteMultiValueHighlighting = false;

        protected BaseFragmentsBuilder()
            : this(new string[] { "<b>" }, new string[] { "</b>" })
        {
        }

        protected BaseFragmentsBuilder(string[] preTags, string[] postTags)
            : this(preTags, postTags, new SimpleBoundaryScanner())
        {
        }

        protected BaseFragmentsBuilder(IBoundaryScanner boundaryScanner)
            : this(new string[] { "<b>" }, new string[] { "</b>" }, boundaryScanner)
        {
        }

        protected BaseFragmentsBuilder(string[] preTags, string[] postTags, IBoundaryScanner boundaryScanner)
        {
            this.preTags = preTags;
            this.postTags = postTags;
            this.boundaryScanner = boundaryScanner;
        }

        internal static object CheckTagsArgument(object tags)
        {
            if (tags is string) return tags;
            else if (tags is string[]) return tags;
            throw new ArgumentException("type of preTags/postTags must be a String or String[]");
        }

        public abstract IList<WeightedFragInfo> GetWeightedFragInfoList(IList<WeightedFragInfo> src);

        private static readonly IEncoder NULL_ENCODER = new DefaultEncoder();

        public virtual string CreateFragment(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList)
        {
            return CreateFragment(reader, docId, fieldName, fieldFragList,
                preTags, postTags, NULL_ENCODER);
        }


        public virtual string[] CreateFragments(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList, int maxNumFragments)
        {
            return CreateFragments(reader, docId, fieldName, fieldFragList, maxNumFragments,
                preTags, postTags, NULL_ENCODER);
        }

        public virtual string CreateFragment(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList, string[] preTags, string[] postTags,
            IEncoder encoder)
        {
            string[]
            fragments = CreateFragments(reader, docId, fieldName, fieldFragList, 1,
        preTags, postTags, encoder);
            if (fragments == null || fragments.Length == 0) return null;
            return fragments[0];
        }


        public virtual string[] CreateFragments(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList, int maxNumFragments,
            string[] preTags, string[] postTags, IEncoder encoder)
        {

            if (maxNumFragments < 0)
            {
                throw new ArgumentException("maxNumFragments(" + maxNumFragments + ") must be positive number.");
            }

            IList<FieldFragList.WeightedFragInfo> fragInfos = fieldFragList.FragInfos;
            Field[]
            values = GetFields(reader, docId, fieldName);
            if (values.Length == 0)
            {
                return null;
            }

            if (discreteMultiValueHighlighting && values.Length > 1)
            {
                fragInfos = DiscreteMultiValueHighlighting(fragInfos, values);
            }

            fragInfos = GetWeightedFragInfoList(fragInfos);
            int limitFragments = maxNumFragments < fragInfos.Count ? maxNumFragments : fragInfos.Count;
            List<string> fragments = new List<string>(limitFragments);

            StringBuilder buffer = new StringBuilder();
            int[] nextValueIndex = { 0 };
            for (int n = 0; n < limitFragments; n++)
            {
                FieldFragList.WeightedFragInfo fragInfo = fragInfos[n];
                fragments.Add(MakeFragment(buffer, nextValueIndex, values, fragInfo, preTags, postTags, encoder));
            }
            return fragments.ToArray(/* new String[fragments.size()] */);
        }

        protected virtual Field[] GetFields(IndexReader reader, int docId, string fieldName)
        {
            // according to javadoc, doc.getFields(fieldName) cannot be used with lazy loaded field???
            List<Field> fields = new List<Field>();
            reader.Document(docId, new GetFieldsStoredFieldsVisitorAnonymousHelper(fields, fieldName));
            //    reader.Document(docId, new StoredFieldVisitor()
            //{

            //    @Override
            //        public void stringField(FieldInfo fieldInfo, String value)
            //{
            //    FieldType ft = new FieldType(TextField.TYPE_STORED);
            //    ft.setStoreTermVectors(fieldInfo.hasVectors());
            //    fields.add(new Field(fieldInfo.name, value, ft));
            //}

            //@Override
            //        public Status needsField(FieldInfo fieldInfo)
            //{
            //    return fieldInfo.name.equals(fieldName) ? Status.YES : Status.NO;
            //}
            //      });
            return fields.ToArray(/*new Field[fields.size()]*/);
        }

        internal class GetFieldsStoredFieldsVisitorAnonymousHelper : StoredFieldVisitor
        {
            private readonly IList<Field> fields;
            private readonly string fieldName;
            public GetFieldsStoredFieldsVisitorAnonymousHelper(IList<Field> fields, string fieldName)
            {
                this.fields = fields;
                this.fieldName = fieldName;
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.StoreTermVectors = fieldInfo.HasVectors();
                fields.Add(new Field(fieldInfo.Name, value, ft));
            }

            public override Status NeedsField(FieldInfo fieldInfo)
            {
                return fieldInfo.Name.Equals(fieldName, StringComparison.Ordinal) ? Status.YES : Status.NO;
            }
        }

        protected virtual string MakeFragment(StringBuilder buffer, int[] index, Field[] values, WeightedFragInfo fragInfo,
            string[] preTags, string[] postTags, IEncoder encoder)
        {
            StringBuilder fragment = new StringBuilder();
            int s = fragInfo.StartOffset;
            int[] modifiedStartOffset = { s };
            string src = GetFragmentSourceMSO(buffer, index, values, s, fragInfo.EndOffset, modifiedStartOffset);
            int srcIndex = 0;
            foreach (SubInfo subInfo in fragInfo.SubInfos)
            {
                foreach (Toffs to in subInfo.TermsOffsets)
                {
                    fragment
                        .Append(encoder.EncodeText(src.Substring(srcIndex, (to.StartOffset - modifiedStartOffset[0]) - srcIndex)))
                        .Append(GetPreTag(preTags, subInfo.Seqnum))
                        .Append(encoder.EncodeText(src.Substring(to.StartOffset - modifiedStartOffset[0], (to.EndOffset - modifiedStartOffset[0]) - (to.StartOffset - modifiedStartOffset[0]))))
                        .Append(GetPostTag(postTags, subInfo.Seqnum));
                    srcIndex = to.EndOffset - modifiedStartOffset[0];
                }
            }
            fragment.Append(encoder.EncodeText(src.Substring(srcIndex)));
            return fragment.ToString();
        }

        protected virtual string GetFragmentSourceMSO(StringBuilder buffer, int[] index, Field[] values,
            int startOffset, int endOffset, int[] modifiedStartOffset)
        {
            while (buffer.Length < endOffset && index[0] < values.Length)
            {
                buffer.Append(values[index[0]++].StringValue);
                buffer.Append(MultiValuedSeparator);
            }
            int bufferLength = buffer.Length;
            // we added the multi value char to the last buffer, ignore it
            if (values[index[0] - 1].FieldType.Tokenized)
            {
                bufferLength--;
            }
            int eo = bufferLength < endOffset ? bufferLength : boundaryScanner.FindEndOffset(buffer, endOffset);
            modifiedStartOffset[0] = boundaryScanner.FindStartOffset(buffer, startOffset);
            return buffer.ToString(modifiedStartOffset[0], eo - modifiedStartOffset[0]);
        }

        protected virtual string GetFragmentSource(StringBuilder buffer, int[] index, Field[] values,
            int startOffset, int endOffset)
        {
            while (buffer.Length < endOffset && index[0] < values.Length)
            {
                buffer.Append(values[index[0]].StringValue);
                buffer.Append(multiValuedSeparator);
                index[0]++;
            }
            int eo = buffer.Length < endOffset ? buffer.Length : endOffset;
            return buffer.ToString(startOffset, eo - startOffset);
        }

        protected virtual IList<WeightedFragInfo> DiscreteMultiValueHighlighting(IList<WeightedFragInfo> fragInfos, Field[] fields)
        {
            IDictionary<string, List<WeightedFragInfo>> fieldNameToFragInfos = new Dictionary<string, List<WeightedFragInfo>>();
            foreach (Field field in fields)
            {
                fieldNameToFragInfos[field.Name] = new List<WeightedFragInfo>();
            }

            foreach (WeightedFragInfo fragInfo in fragInfos)
            {
                int fieldStart;
                int fieldEnd = 0;
                foreach (Field field in fields)
                {
                    if (field.StringValue.Length == 0)
                    {
                        fieldEnd++;
                        continue;
                    }
                    fieldStart = fieldEnd;
                    fieldEnd += field.StringValue.Length + 1; // + 1 for going to next field with same name.

                    if (fragInfo.StartOffset >= fieldStart && fragInfo.EndOffset >= fieldStart &&
                        fragInfo.StartOffset <= fieldEnd && fragInfo.EndOffset <= fieldEnd)
                    {
                        fieldNameToFragInfos[field.Name].Add(fragInfo);

                        goto fragInfos_continue;
                    }

                    if (!fragInfo.SubInfos.Any())
                    {
                        goto fragInfos_continue;
                    }

                    Toffs firstToffs = fragInfo.SubInfos[0].TermsOffsets[0];
                    if (fragInfo.StartOffset >= fieldEnd || firstToffs.StartOffset >= fieldEnd)
                    {
                        continue;
                    }

                    int fragStart = fieldStart;
                    if (fragInfo.StartOffset > fieldStart && fragInfo.StartOffset < fieldEnd)
                    {
                        fragStart = fragInfo.StartOffset;
                    }

                    int fragEnd = fieldEnd;
                    if (fragInfo.EndOffset > fieldStart && fragInfo.EndOffset < fieldEnd)
                    {
                        fragEnd = fragInfo.EndOffset;
                    }

                    // LUCENENET specific - track the fragInfo.SubInfos items to delete
                    List<SubInfo> fragInfo_SubInfos_ToDelete = new List<SubInfo>();

                    List<SubInfo> subInfos = new List<SubInfo>();
                    IEnumerator<SubInfo> subInfoIterator = fragInfo.SubInfos.GetEnumerator();
                    float boost = 0.0f;  //  The boost of the new info will be the sum of the boosts of its SubInfos
                    while (subInfoIterator.MoveNext())
                    {
                        SubInfo subInfo = subInfoIterator.Current;
                        List<Toffs> toffsList = new List<Toffs>();


                        IEnumerator<Toffs> toffsIterator = subInfo.TermsOffsets.GetEnumerator();
                        while (toffsIterator.MoveNext())
                        {
                            Toffs toffs = toffsIterator.Current;
                            if (toffs.StartOffset >= fieldStart && toffs.EndOffset <= fieldEnd)
                            {

                                toffsList.Add(toffs);
                                //toffsIterator.Remove();
                            }
                        }
                        if (toffsList.Any())
                        {
                            // LUCENENET NOTE: Instead of removing during iteration (which isn't allowed in .NET when using an IEnumerator), 
                            // we just remove the items at this point. We only get here if there are items to remove.
                            subInfo.TermsOffsets.RemoveAll(toffsList);

                            subInfos.Add(new SubInfo(subInfo.Text, toffsList, subInfo.Seqnum, subInfo.Boost));
                            boost += subInfo.Boost;
                        }

                        if (!subInfo.TermsOffsets.Any())
                        {
                            //subInfoIterator.Remove();
                            fragInfo_SubInfos_ToDelete.Add(subInfo);
                        }
                    }

                    // LUCENENET specific - now that we are done iterating the loop, it is safe to delete
                    // the items we earmarked. Note this is just a list of pointers, so it doens't consume
                    // much RAM.
                    fragInfo.SubInfos.RemoveAll(fragInfo_SubInfos_ToDelete);


                    WeightedFragInfo weightedFragInfo = new WeightedFragInfo(fragStart, fragEnd, subInfos, boost);
                    fieldNameToFragInfos[field.Name].Add(weightedFragInfo);
                }
                fragInfos_continue: { }
            }

            List<WeightedFragInfo> result = new List<WeightedFragInfo>();
            foreach (List<WeightedFragInfo> weightedFragInfos in fieldNameToFragInfos.Values)
            {
                result.AddAll(weightedFragInfos);
            }
            CollectionUtil.TimSort(result, new DiscreteMultiValueHighlightingComparerAnonymousHelper());
            //    Collections.sort(result, new Comparator<WeightedFragInfo>() {

            //      @Override
            //      public int compare(FieldFragList.WeightedFragInfo info1, FieldFragList.WeightedFragInfo info2)
            //{
            //    return info1.getStartOffset() - info2.getStartOffset();
            //}

            //    });

            return result;
        }

        internal class DiscreteMultiValueHighlightingComparerAnonymousHelper : IComparer<WeightedFragInfo>
        {
            public int Compare(WeightedFragInfo info1, WeightedFragInfo info2)
            {
                return info1.StartOffset - info2.StartOffset;
            }
        }

        public void SetMultiValuedSeparator(char separator)
        {
            multiValuedSeparator = separator;
        }

        public virtual char MultiValuedSeparator
        {
            get { return multiValuedSeparator; }
        }

        public virtual bool IsDiscreteMultiValueHighlighting
        {
            get { return discreteMultiValueHighlighting; }
            set { this.discreteMultiValueHighlighting = value; }
        }


        protected virtual string GetPreTag(int num)
        {
            return GetPreTag(preTags, num);
        }

        protected virtual string GetPostTag(int num)
        {
            return GetPostTag(postTags, num);
        }

        protected virtual string GetPreTag(string[] preTags, int num)
        {
            int n = num % preTags.Length;
            return preTags[n];
        }

        protected virtual string GetPostTag(string[] postTags, int num)
        {
            int n = num % postTags.Length;
            return postTags[n];
        }
    }
}
