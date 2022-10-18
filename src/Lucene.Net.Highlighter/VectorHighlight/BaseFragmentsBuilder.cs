using J2N.Collections.Generic.Extensions;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Text;
using SubInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo.SubInfo;
using Toffs = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo.Toffs;
using WeightedFragInfo = Lucene.Net.Search.VectorHighlight.FieldFragList.WeightedFragInfo;
using JCG = J2N.Collections.Generic;

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
    /// Uses <see cref="IBoundaryScanner"/> to determine fragments.
    /// </summary>
    public abstract class BaseFragmentsBuilder : IFragmentsBuilder
    {
        protected string[] m_preTags, m_postTags;
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
            this.m_preTags = preTags;
            this.m_postTags = postTags;
            this.boundaryScanner = boundaryScanner;
        }

        internal static object CheckTagsArgument(object tags)
        {
            if (tags is string) return tags;
            else if (tags is string[]) return tags;
            throw new ArgumentException("type of preTags/postTags must be a string or string[]");
        }

        public abstract IList<WeightedFragInfo> GetWeightedFragInfoList(IList<WeightedFragInfo> src);

        private static readonly IEncoder NULL_ENCODER = new DefaultEncoder();

        public virtual string CreateFragment(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList)
        {
            return CreateFragment(reader, docId, fieldName, fieldFragList,
                m_preTags, m_postTags, NULL_ENCODER);
        }

        public virtual string[] CreateFragments(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList, int maxNumFragments)
        {
            return CreateFragments(reader, docId, fieldName, fieldFragList, maxNumFragments,
                m_preTags, m_postTags, NULL_ENCODER);
        }

        public virtual string CreateFragment(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList, string[] preTags, string[] postTags,
            IEncoder encoder)
        {
            string[]
            fragments = CreateFragments(reader, docId, fieldName, fieldFragList, 1,
                preTags, postTags, encoder);
            if (fragments is null || fragments.Length == 0) return null;
            return fragments[0];
        }


        public virtual string[] CreateFragments(IndexReader reader, int docId,
            string fieldName, FieldFragList fieldFragList, int maxNumFragments,
            string[] preTags, string[] postTags, IEncoder encoder)
        {
            // LUCENENET specific - added guard clauses to check for null
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));
            if (fieldFragList is null)
                throw new ArgumentNullException(nameof(fieldFragList));
            if (preTags is null)
                throw new ArgumentNullException(nameof(preTags));
            if (postTags is null)
                throw new ArgumentNullException(nameof(postTags));
            if (encoder is null)
                throw new ArgumentNullException(nameof(encoder));

            if (maxNumFragments < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxNumFragments), "maxNumFragments(" + maxNumFragments + ") must be positive number."); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }

            IList<WeightedFragInfo> fragInfos = fieldFragList.FragInfos;
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
            JCG.List<string> fragments = new JCG.List<string>(limitFragments);

            StringBuilder buffer = new StringBuilder();
            int[] nextValueIndex = { 0 };
            for (int n = 0; n < limitFragments; n++)
            {
                WeightedFragInfo fragInfo = fragInfos[n];
                fragments.Add(MakeFragment(buffer, nextValueIndex, values, fragInfo, preTags, postTags, encoder));
            }
            return fragments.ToArray(/* new String[fragments.size()] */);
        }

        protected virtual Field[] GetFields(IndexReader reader, int docId, string fieldName)
        {
            // according to javadoc, doc.getFields(fieldName) cannot be used with lazy loaded field???
            JCG.List<Field> fields = new JCG.List<Field>();
            reader.Document(docId, new GetFieldsStoredFieldsVisitorAnonymousClass(fields, fieldName));

            return fields.ToArray(/*new Field[fields.size()]*/);
        }

        private sealed class GetFieldsStoredFieldsVisitorAnonymousClass : StoredFieldVisitor
        {
            private readonly IList<Field> fields;
            private readonly string fieldName;
            public GetFieldsStoredFieldsVisitorAnonymousClass(IList<Field> fields, string fieldName)
            {
                this.fields = fields;
                this.fieldName = fieldName;
            }

            public override void StringField(FieldInfo fieldInfo, string value)
            {
                FieldType ft = new FieldType(TextField.TYPE_STORED);
                ft.StoreTermVectors = fieldInfo.HasVectors;
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
                buffer.Append(values[index[0]++].GetStringValue());
                buffer.Append(MultiValuedSeparator);
            }
            int bufferLength = buffer.Length;
            // we added the multi value char to the last buffer, ignore it
            if (values[index[0] - 1].IndexableFieldType.IsTokenized)
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
                buffer.Append(values[index[0]].GetStringValue());
                buffer.Append(multiValuedSeparator);
                index[0]++;
            }
            int eo = buffer.Length < endOffset ? buffer.Length : endOffset;
            return buffer.ToString(startOffset, eo - startOffset);
        }

        protected virtual IList<WeightedFragInfo> DiscreteMultiValueHighlighting(IList<WeightedFragInfo> fragInfos, Field[] fields)
        {
            IDictionary<string, IList<WeightedFragInfo>> fieldNameToFragInfos = new Dictionary<string, IList<WeightedFragInfo>>();
            foreach (Field field in fields)
            {
                fieldNameToFragInfos[field.Name] = new JCG.List<WeightedFragInfo>();
            }

            foreach (WeightedFragInfo fragInfo in fragInfos)
            {
                int fieldStart;
                int fieldEnd = 0;
                foreach (Field field in fields)
                {
                    if (field.GetStringValue().Length == 0)
                    {
                        fieldEnd++;
                        continue;
                    }
                    fieldStart = fieldEnd;
                    fieldEnd += field.GetStringValue().Length + 1; // + 1 for going to next field with same name.

                    if (fragInfo.StartOffset >= fieldStart && fragInfo.EndOffset >= fieldStart &&
                        fragInfo.StartOffset <= fieldEnd && fragInfo.EndOffset <= fieldEnd)
                    {
                        fieldNameToFragInfos[field.Name].Add(fragInfo);

                        goto fragInfos_continue;
                    }

                    if (fragInfo.SubInfos.Count == 0)
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

                    // LUCENENET NOTE: Instead of removing during iteration (which isn't allowed in .NET when using an IEnumerator),
                    // We use the IList<T>.RemoveAll() extension method of J2N. This removal happens in a forward way, but since it
                    // accepts a predicate, we can put in the rest of Lucene's logic without doing something expensive like keeping
                    // track of the items to remove in a separate collection. In a nutshell, any time Lucene calls iterator.remove(),
                    // we return true and any time it is skipped, we return false.

                    IList<SubInfo> subInfos = new JCG.List<SubInfo>();
                    float boost = 0.0f;  //  The boost of the new info will be the sum of the boosts of its SubInfos
                    fragInfo.SubInfos.RemoveAll((subInfo) =>
                    {
                        IList<Toffs> toffsList = new JCG.List<Toffs>();
                        subInfo.TermsOffsets.RemoveAll((toffs) =>
                        {
                            if (toffs.StartOffset >= fieldStart && toffs.EndOffset <= fieldEnd)
                            {

                                toffsList.Add(toffs);
                                return true; // Remove
                            }
                            return false;
                        });
                        if (toffsList.Count > 0)
                        {
                            subInfos.Add(new SubInfo(subInfo.Text, toffsList, subInfo.Seqnum, subInfo.Boost));
                            boost += subInfo.Boost;
                        }

                        if (subInfo.TermsOffsets.Count == 0)
                        {
                            return true; // Remove
                        }
                        return false;
                    });

                    WeightedFragInfo weightedFragInfo = new WeightedFragInfo(fragStart, fragEnd, subInfos, boost);
                    fieldNameToFragInfos[field.Name].Add(weightedFragInfo);
                }
            fragInfos_continue: { }
            }

            JCG.List<WeightedFragInfo> result = new JCG.List<WeightedFragInfo>();
            foreach (IList<WeightedFragInfo> weightedFragInfos in fieldNameToFragInfos.Values)
            {
                result.AddRange(weightedFragInfos);
            }
            CollectionUtil.TimSort(result, Comparer<WeightedFragInfo>.Create((info1, info2) => info1.StartOffset - info2.StartOffset));

            return result;
        }
        
        public virtual char MultiValuedSeparator
        {
            get => multiValuedSeparator;
            set => multiValuedSeparator = value;
        }

        public virtual bool IsDiscreteMultiValueHighlighting
        {
            get => discreteMultiValueHighlighting;
            set => this.discreteMultiValueHighlighting = value;
        }

        protected virtual string GetPreTag(int num)
        {
            return GetPreTag(m_preTags, num);
        }

        protected virtual string GetPostTag(int num)
        {
            return GetPostTag(m_postTags, num);
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
