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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Index;

using WeightedFragInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo;
using SubInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo.SubInfo;
using Toffs = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo.Toffs;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Support;

namespace Lucene.Net.Search.VectorHighlight
{
    public abstract class BaseFragmentsBuilder : IFragmentsBuilder
    {
        protected String[] preTags, postTags;
        public static String[] COLORED_PRE_TAGS = {
            "<b style=\"background:yellow\">", "<b style=\"background:lawngreen\">", "<b style=\"background:aquamarine\">",
            "<b style=\"background:magenta\">", "<b style=\"background:palegreen\">", "<b style=\"background:coral\">",
            "<b style=\"background:wheat\">", "<b style=\"background:khaki\">", "<b style=\"background:lime\">",
            "<b style=\"background:deepskyblue\">", "<b style=\"background:deeppink\">", "<b style=\"background:salmon\">",
            "<b style=\"background:peachpuff\">", "<b style=\"background:violet\">", "<b style=\"background:mediumpurple\">",
            "<b style=\"background:palegoldenrod\">", "<b style=\"background:darkkhaki\">", "<b style=\"background:springgreen\">",
            "<b style=\"background:turquoise\">", "<b style=\"background:powderblue\">"
        };

        public static String[] COLORED_POST_TAGS = { "</b>" };
        private char multiValuedSeparator = ' ';
        private readonly IBoundaryScanner boundaryScanner;
        private bool discreteMultiValueHighlighting = false;

        protected BaseFragmentsBuilder()
            : this(new String[] { "<b>" }, new String[] { "</b>" })
        {

        }

        protected BaseFragmentsBuilder(String[] preTags, String[] postTags)
            : this(preTags, postTags, new SimpleBoundaryScanner())
        {
        }

        protected BaseFragmentsBuilder(IBoundaryScanner boundaryScanner)
            : this(new String[] { "<b>" }, new String[] { "</b>" }, boundaryScanner)
        {
        }

        protected BaseFragmentsBuilder(String[] preTags, String[] postTags, IBoundaryScanner boundaryScanner)
        {
            this.preTags = preTags;
            this.postTags = postTags;
            this.boundaryScanner = boundaryScanner;
        }

        static Object CheckTagsArgument(Object tags)
        {
            if (tags is String) return tags;
            else if (tags is String[]) return tags;
            throw new ArgumentException("type of preTags/postTags must be a String or String[]");
        }

        public abstract IList<WeightedFragInfo> GetWeightedFragInfoList(IList<WeightedFragInfo> src);

        private static readonly IEncoder NULL_ENCODER = new DefaultEncoder();

        public virtual String CreateFragment(IndexReader reader, int docId, String fieldName, FieldFragList fieldFragList)
        {
            return CreateFragment(reader, docId, fieldName, fieldFragList, preTags, postTags, NULL_ENCODER);
        }

        public virtual String[] CreateFragments(IndexReader reader, int docId, String fieldName, FieldFragList fieldFragList, int maxNumFragments)
        {
            return CreateFragments(reader, docId, fieldName, fieldFragList, maxNumFragments, preTags, postTags, NULL_ENCODER);
        }

        public String CreateFragment(IndexReader reader, int docId,
            String fieldName, FieldFragList fieldFragList, String[] preTags, String[] postTags,
            IEncoder encoder)
        {
            String[] fragments = CreateFragments(reader, docId, fieldName, fieldFragList, 1,
                preTags, postTags, encoder);
            if (fragments == null || fragments.Length == 0) return null;
            return fragments[0];
        }

        public String[] CreateFragments(IndexReader reader, int docId,
            String fieldName, FieldFragList fieldFragList, int maxNumFragments,
            String[] preTags, String[] postTags, IEncoder encoder)
        {

            if (maxNumFragments < 0)
            {
                throw new ArgumentException("maxNumFragments(" + maxNumFragments + ") must be positive number.");
            }

            IList<WeightedFragInfo> fragInfos = fieldFragList.FragInfos;
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
            int limitFragments = maxNumFragments < fragInfos.Count ? maxNumFragments : fragInfos.Count;
            List<String> fragments = new List<String>(limitFragments);

            StringBuilder buffer = new StringBuilder();
            int[] nextValueIndex = { 0 };
            for (int n = 0; n < limitFragments; n++)
            {
                WeightedFragInfo fragInfo = fragInfos[n];
                fragments.Add(MakeFragment(buffer, nextValueIndex, values, fragInfo, preTags, postTags, encoder));
            }
            return fragments.ToArray();
        }

        protected virtual Field[] GetFields(IndexReader reader, int docId, String fieldName)
        {
            // according to javadoc, doc.getFields(fieldName) cannot be used with lazy loaded field???
            IList<Field> fields = new List<Field>();
            reader.Document(docId, new AnonymousGetFieldsStoredFieldVisitor(fields, fieldName));
            return fields.ToArray();
        }

        private sealed class AnonymousGetFieldsStoredFieldVisitor : StoredFieldVisitor
        {
            private readonly IList<Field> fields;
            private readonly string fieldName;

            public AnonymousGetFieldsStoredFieldVisitor(IList<Field> fields, string fieldName)
            {
                this.fields = fields;
                this.fieldName = fieldName;
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.StoreTermVectors = fieldInfo.HasVectors;
                fields.Add(new Field(fieldInfo.name, value, ft));
            }

            public override Status NeedsField(FieldInfo fieldInfo)
            {
                return fieldInfo.name.Equals(fieldName) ? Status.YES : Status.NO;
            }
        }

        protected String MakeFragment(StringBuilder buffer, int[] index, Field[] values, WeightedFragInfo fragInfo,
            String[] preTags, String[] postTags, IEncoder encoder)
        {
            StringBuilder fragment = new StringBuilder();
            int s = fragInfo.StartOffset;
            int[] modifiedStartOffset = { s };
            String src = GetFragmentSourceMSO(buffer, index, values, s, fragInfo.EndOffset, modifiedStartOffset);
            int srcIndex = 0;
            foreach (SubInfo subInfo in fragInfo.SubInfos)
            {
                foreach (Toffs to in subInfo.TermsOffsets)
                {
                    fragment
                      .Append(encoder.EncodeText(src.Substring(srcIndex, to.StartOffset - modifiedStartOffset[0] - srcIndex)))
                      .Append(GetPreTag(preTags, subInfo.Seqnum))
                      .Append(encoder.EncodeText(src.Substring(to.StartOffset - modifiedStartOffset[0], to.EndOffset - modifiedStartOffset[0] - to.StartOffset)))
                      .Append(GetPostTag(postTags, subInfo.Seqnum));
                    srcIndex = to.EndOffset - modifiedStartOffset[0];
                }
            }
            fragment.Append(encoder.EncodeText(src.Substring(srcIndex)));
            return fragment.ToString();
        }

        protected string GetFragmentSourceMSO(StringBuilder buffer, int[] index, Field[] values,
            int startOffset, int endOffset, int[] modifiedStartOffset)
        {
            while (buffer.Length < endOffset && index[0] < values.Length)
            {
                buffer.Append(values[index[0]++].StringValue);
                buffer.Append(MultiValuedSeparator);
            }
            int bufferLength = buffer.Length;
            // we added the multi value char to the last buffer, ignore it
            if (values[index[0] - 1].FieldTypeValue.Tokenized)
            {
                bufferLength--;
            }
            int eo = bufferLength < endOffset ? bufferLength : boundaryScanner.FindEndOffset(buffer, endOffset);
            modifiedStartOffset[0] = boundaryScanner.FindStartOffset(buffer, startOffset);
            return buffer.ToString().Substring(modifiedStartOffset[0], eo - modifiedStartOffset[0]);
        }

        protected virtual String GetFragmentSource(StringBuilder buffer, int[] index, Field[] values, int startOffset, int endOffset)
        {
            while (buffer.Length < endOffset && index[0] < values.Length)
            {
                buffer.Append(values[index[0]].StringValue);
                buffer.Append(multiValuedSeparator);
                index[0]++;
            }
            int eo = buffer.Length < endOffset ? buffer.Length : endOffset;
            return buffer.ToString().Substring(startOffset, eo - startOffset);
        }

        protected virtual List<WeightedFragInfo> DiscreteMultiValueHighlighting(IList<WeightedFragInfo> fragInfos, Field[] fields)
        {
            IDictionary<String, List<WeightedFragInfo>> fieldNameToFragInfos = new HashMap<String, List<WeightedFragInfo>>();
            foreach (Field field in fields)
            {
                fieldNameToFragInfos[field.Name] = new List<WeightedFragInfo>();
            }

            foreach (WeightedFragInfo fragInfo in fragInfos)
            {
                int fieldStart;
                int fieldEnd = 0;
                bool shouldContinueOuter = false; // .NET port: using in place of continue-to-label

                foreach (Field field in fields)
                {
                    if (string.IsNullOrEmpty(field.StringValue))
                    {
                        fieldEnd++;
                        continue;
                    }

                    fieldStart = fieldEnd;
                    fieldEnd += field.StringValue.Length + 1;
                    if (fragInfo.StartOffset >= fieldStart && fragInfo.EndOffset >= fieldStart && fragInfo.StartOffset <= fieldEnd && fragInfo.EndOffset <= fieldEnd)
                    {
                        fieldNameToFragInfos[field.Name].Add(fragInfo);
                        shouldContinueOuter = true;
                        //continue;
                        break;
                    }

                    if (fragInfo.SubInfos.Count == 0)
                    {
                        shouldContinueOuter = true;
                        //continue;
                        break;
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

                    List<SubInfo> subInfos = new List<SubInfo>();
                    WeightedFragInfo weightedFragInfo = new WeightedFragInfo(fragStart, fragEnd, subInfos, fragInfo.TotalBoost);
                    //IEnumerator<SubInfo> subInfoIterator = fragInfo.SubInfos.GetEnumerator();

                    for (int i = 0; i < fragInfo.SubInfos.Count; i++)
                    //while (subInfoIterator.MoveNext())
                    {
                        //SubInfo subInfo = subInfoIterator.Current;
                        SubInfo subInfo = fragInfo.SubInfos[i];
                        List<Toffs> toffsList = new List<Toffs>();
                        //IEnumerator<Toffs> toffsIterator = subInfo.TermsOffsets.GetEnumerator();

                        for (int j = 0; j < subInfo.TermsOffsets.Count; j++)
                        //while (toffsIterator.MoveNext())
                        {
                            //Toffs toffs = toffsIterator.Current;
                            Toffs toffs = subInfo.TermsOffsets[j];

                            if (toffs.StartOffset >= fieldStart && toffs.EndOffset <= fieldEnd)
                            {
                                toffsList.Add(toffs);
                                //toffsIterator.Remove();
                                subInfo.TermsOffsets.RemoveAt(j--);
                            }
                        }

                        if (toffsList.Count > 0)
                        {
                            subInfos.Add(new SubInfo(subInfo.Text, toffsList, subInfo.Seqnum));
                        }

                        if (subInfo.TermsOffsets.Count == 0)
                        {
                            fragInfo.SubInfos.RemoveAt(i--);
                        }
                    }

                    fieldNameToFragInfos[field.Name].Add(weightedFragInfo);
                }

                // not really needed right now, but can't hurt
                if (shouldContinueOuter)
                    continue;
            }

            List<WeightedFragInfo> result = new List<WeightedFragInfo>();
            foreach (List<WeightedFragInfo> weightedFragInfos in fieldNameToFragInfos.Values)
            {
                result.AddRange(weightedFragInfos);
            }

            result.Sort(new AnonymousComparator());
            return result;
        }

        private sealed class AnonymousComparator : IComparer<WeightedFragInfo>
        {
            public int Compare(FieldFragList.WeightedFragInfo info1, FieldFragList.WeightedFragInfo info2)
            {
                return info1.StartOffset - info2.StartOffset;
            }
        }

        public char MultiValuedSeparator
        {
            get { return multiValuedSeparator; }
            set { multiValuedSeparator = value; }
        }

        public bool IsDiscreteMultiValueHighlighting
        {
            get { return discreteMultiValueHighlighting; }
            set { discreteMultiValueHighlighting = value; }
        }

        protected virtual String GetPreTag(int num)
        {
            return GetPreTag(preTags, num);
        }

        protected virtual String GetPostTag(int num)
        {
            return GetPostTag(postTags, num);
        }

        protected virtual String GetPreTag(String[] preTags, int num)
        {
            int n = num % preTags.Length;
            return preTags[n];
        }

        protected virtual String GetPostTag(String[] postTags, int num)
        {
            int n = num % postTags.Length;
            return postTags[n];
        }
    }
}
