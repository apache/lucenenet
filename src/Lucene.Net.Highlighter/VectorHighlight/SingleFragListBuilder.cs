using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WeightedPhraseInfo = Lucene.Net.Search.VectorHighlight.FieldPhraseList.WeightedPhraseInfo;

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
    /// An implementation class of {@link FragListBuilder} that generates one {@link WeightedFragInfo} object.
    /// Typical use case of this class is that you can get an entire field contents
    /// by using both of this class and {@link SimpleFragmentsBuilder}.
    /// <para/>
    /// <code>
    /// FastVectorHighlighter h = new FastVectorHighlighter( true, true,
    ///     new SingleFragListBuilder(), new SimpleFragmentsBuilder() );
    /// </code>
    /// </summary>
    public class SingleFragListBuilder : IFragListBuilder
    {
        public virtual FieldFragList CreateFieldFragList(FieldPhraseList fieldPhraseList,
            int fragCharSize)
        {
            FieldFragList ffl = new SimpleFieldFragList(fragCharSize);

            List<WeightedPhraseInfo> wpil = new List<WeightedPhraseInfo>();
            IEnumerator<WeightedPhraseInfo> ite = fieldPhraseList.PhraseList.GetEnumerator();
            WeightedPhraseInfo phraseInfo = null;
            while (true)
            {
                if (!ite.MoveNext()) break;
                phraseInfo = ite.Current;
                if (phraseInfo == null) break;

                wpil.Add(phraseInfo);
            }
            if (wpil.Count > 0)
                ffl.Add(0, int.MaxValue, wpil);
            return ffl;
        }
    }
}
