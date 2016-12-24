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

namespace Lucene.Net.Classification
{
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.TokenAttributes;
    using Lucene.Net.Index;
    using Lucene.Net.Search;
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// A simplistic Lucene based NaiveBayes classifier, see <code>http://en.wikipedia.org/wiki/Naive_Bayes_classifier</code>
    ///
    /// @lucene.experimental
    /// </summary>
    public class SimpleNaiveBayesClassifier : IClassifier<BytesRef>
    {
        private AtomicReader _atomicReader;
        private String[] _textFieldNames;
        private String _classFieldName;
        private int _docsWithClassSize;
        private Analyzer _analyzer;
        private IndexSearcher _indexSearcher;
        private Query _query;

        public SimpleNaiveBayesClassifier()
        {      
        }

        public void Train(AtomicReader atomicReader, String textFieldName, String classFieldName, Analyzer analyzer) 
        {
            Train(atomicReader, textFieldName, classFieldName, analyzer, null);
        }

        public void Train(AtomicReader atomicReader, String textFieldName, String classFieldName, Analyzer analyzer, Query query)
        {
            Train(atomicReader, new String[]{textFieldName}, classFieldName, analyzer, query);
        }

        public void Train(AtomicReader atomicReader, String[] textFieldNames, String classFieldName, Analyzer analyzer, Query query)
        {
            _atomicReader = atomicReader;
            _indexSearcher = new IndexSearcher(_atomicReader);
            _textFieldNames = textFieldNames;
            _classFieldName = classFieldName;
            _analyzer = analyzer;
            _query = query;
            _docsWithClassSize = CountDocsWithClass();
        }

        private int CountDocsWithClass() 
        {
            int docCount = MultiFields.GetTerms(_atomicReader, _classFieldName).DocCount;
            if (docCount == -1) 
            { // in case codec doesn't support getDocCount
                TotalHitCountCollector totalHitCountCollector = new TotalHitCountCollector();
                BooleanQuery q = new BooleanQuery();
                q.Add(new BooleanClause(new WildcardQuery(new Term(_classFieldName, WildcardQuery.WILDCARD_STRING.ToString())), Occur.MUST));
                if (_query != null) 
                {
                    q.Add(_query, Occur.MUST);
                }
                _indexSearcher.Search(q, totalHitCountCollector);
                docCount = totalHitCountCollector.TotalHits;
            }
            return docCount;
        }

        private String[] TokenizeDoc(String doc)
        {
            ICollection<String> result = new LinkedList<string>();
            foreach (String textFieldName in _textFieldNames) {
                TokenStream tokenStream = _analyzer.TokenStream(textFieldName, new StringReader(doc));
                try 
                {
                    ICharTermAttribute charTermAttribute = tokenStream.AddAttribute<ICharTermAttribute>();
                    tokenStream.Reset();
                    while (tokenStream.IncrementToken()) 
                    {
                        result.Add(charTermAttribute.ToString());
                    }
                    tokenStream.End();
                } 
                finally 
                {
                    IOUtils.CloseWhileHandlingException(tokenStream);
                }
            }
            var ret = new string[result.Count];
            result.CopyTo(ret, 0);
            return ret;
        }

        public ClassificationResult<BytesRef> AssignClass(String inputDocument) 
        {
            if (_atomicReader == null) 
            {
                throw new IOException("You must first call Classifier#train");
            }
            double max = - Double.MaxValue;
            BytesRef foundClass = new BytesRef();

            Terms terms = MultiFields.GetTerms(_atomicReader, _classFieldName);
            TermsEnum termsEnum = terms.Iterator(null);
            BytesRef next;
            String[] tokenizedDoc = TokenizeDoc(inputDocument);
            while ((next = termsEnum.Next()) != null) 
            {
                double clVal = CalculateLogPrior(next) + CalculateLogLikelihood(tokenizedDoc, next);
                if (clVal > max) 
                {
                    max = clVal;
                    foundClass = BytesRef.DeepCopyOf(next);
                }
            }
            double score = 10 / Math.Abs(max);
            return new ClassificationResult<BytesRef>(foundClass, score);
        }


        private double CalculateLogLikelihood(String[] tokenizedDoc, BytesRef c)
        {
            // for each word
            double result = 0d;
            foreach (String word in tokenizedDoc) 
            {
                // search with text:word AND class:c
                int hits = GetWordFreqForClass(word, c);

                // num : count the no of times the word appears in documents of class c (+1)
                double num = hits + 1; // +1 is added because of add 1 smoothing

                // den : for the whole dictionary, count the no of times a word appears in documents of class c (+|V|)
                double den = GetTextTermFreqForClass(c) + _docsWithClassSize;

                // P(w|c) = num/den
                double wordProbability = num / den;
                result += Math.Log(wordProbability);
            }

            // log(P(d|c)) = log(P(w1|c))+...+log(P(wn|c))
            return result;
        }

        private double GetTextTermFreqForClass(BytesRef c)
        {
            double avgNumberOfUniqueTerms = 0;
            foreach (String textFieldName in _textFieldNames) 
            {
                Terms terms = MultiFields.GetTerms(_atomicReader, textFieldName);
                long numPostings = terms.SumDocFreq; // number of term/doc pairs
                avgNumberOfUniqueTerms += numPostings / (double) terms.DocCount; // avg # of unique terms per doc
            }
            int docsWithC = _atomicReader.DocFreq(new Term(_classFieldName, c));
            return avgNumberOfUniqueTerms * docsWithC; // avg # of unique terms in text fields per doc * # docs with c
        }

        private int GetWordFreqForClass(String word, BytesRef c)
        {
            BooleanQuery booleanQuery = new BooleanQuery();
            BooleanQuery subQuery = new BooleanQuery();
            foreach (String textFieldName in _textFieldNames) 
            {
                subQuery.Add(new BooleanClause(new TermQuery(new Term(textFieldName, word)), Occur.SHOULD));
            }
            booleanQuery.Add(new BooleanClause(subQuery, Occur.MUST));
            booleanQuery.Add(new BooleanClause(new TermQuery(new Term(_classFieldName, c)), Occur.MUST));
            if (_query != null) 
            {
                booleanQuery.Add(_query, Occur.MUST);
            }
            TotalHitCountCollector totalHitCountCollector = new TotalHitCountCollector();
            _indexSearcher.Search(booleanQuery, totalHitCountCollector);
            return totalHitCountCollector.TotalHits;
        }

        private double CalculateLogPrior(BytesRef currentClass)
        {
            return Math.Log((double) DocCount(currentClass)) - Math.Log(_docsWithClassSize);
        }

        private int DocCount(BytesRef countedClass) 
        {
            return _atomicReader.DocFreq(new Term(_classFieldName, countedClass));
        }
    }   
}