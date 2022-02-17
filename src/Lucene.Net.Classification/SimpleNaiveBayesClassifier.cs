using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Classification
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
    /// A simplistic Lucene based NaiveBayes classifier, see
    /// <a href="http://en.wikipedia.org/wiki/Naive_Bayes_classifier">http://en.wikipedia.org/wiki/Naive_Bayes_classifier</a>
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class SimpleNaiveBayesClassifier : IClassifier<BytesRef>
    {
        private AtomicReader atomicReader;
        private string[] textFieldNames;
        private string classFieldName;
        private int docsWithClassSize;
        private Analyzer analyzer;
        private IndexSearcher indexSearcher;
        private Query query;

        /// <summary>
        /// Creates a new NaiveBayes classifier.
        /// Note that you must call <see cref="Train(AtomicReader, string, string, Analyzer)"/> before you can
        /// classify any documents.
        /// </summary>
        public SimpleNaiveBayesClassifier()
        {      
        }

        /// <summary>
        /// Train the classifier using the underlying Lucene index
        /// </summary>
        /// <param name="analyzer"> the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        public virtual void Train(AtomicReader atomicReader, string textFieldName, string classFieldName, Analyzer analyzer) 
        {
            Train(atomicReader, textFieldName, classFieldName, analyzer, null);
        }

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldName">the name of the field used to compare documents</param>
        public virtual void Train(AtomicReader atomicReader, string textFieldName, string classFieldName, Analyzer analyzer, Query query)
        {
            Train(atomicReader, new string[]{textFieldName}, classFieldName, analyzer, query);
        }

        /// <summary>Train the classifier using the underlying Lucene index</summary>
        /// <param name="analyzer">the analyzer used to tokenize / filter the unseen text</param>
        /// <param name="atomicReader">the reader to use to access the Lucene index</param>
        /// <param name="classFieldName">the name of the field containing the class assigned to documents</param>
        /// <param name="query">the query to filter which documents use for training</param>
        /// <param name="textFieldNames">the names of the fields to be used to compare documents</param>
        public virtual void Train(AtomicReader atomicReader, string[] textFieldNames, string classFieldName, Analyzer analyzer, Query query)
        {
            this.atomicReader = atomicReader;
            indexSearcher = new IndexSearcher(this.atomicReader);
            this.textFieldNames = textFieldNames;
            this.classFieldName = classFieldName;
            this.analyzer = analyzer;
            this.query = query;
            docsWithClassSize = CountDocsWithClass();
        }

        private int CountDocsWithClass() 
        {
            int docCount = MultiFields.GetTerms(atomicReader, classFieldName).DocCount;
            if (docCount == -1) 
            { // in case codec doesn't support getDocCount
                TotalHitCountCollector totalHitCountCollector = new TotalHitCountCollector();
                BooleanQuery q = new BooleanQuery
                {
                    new BooleanClause(new WildcardQuery(new Term(classFieldName, WildcardQuery.WILDCARD_STRING.ToString())), Occur.MUST)
                };
                if (query != null) 
                {
                    q.Add(query, Occur.MUST);
                }
                indexSearcher.Search(q, totalHitCountCollector);
                docCount = totalHitCountCollector.TotalHits;
            }
            return docCount;
        }

        private string[] TokenizeDoc(string doc)
        {
            ICollection<string> result = new LinkedList<string>();
            foreach (string textFieldName in textFieldNames) {
                TokenStream tokenStream = analyzer.GetTokenStream(textFieldName, new StringReader(doc));
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
                    IOUtils.DisposeWhileHandlingException(tokenStream);
                }
            }
            var ret = new string[result.Count];
            result.CopyTo(ret, 0);
            return ret;
        }

        /// <summary>
        /// Assign a class (with score) to the given text string
        /// </summary>
        /// <param name="inputDocument">a string containing text to be classified</param>
        /// <returns>a <see cref="ClassificationResult{BytesRef}"/> holding assigned class of type <see cref="BytesRef"/> and score</returns>
        public virtual ClassificationResult<BytesRef> AssignClass(string inputDocument) 
        {
            if (atomicReader is null) 
            {
                throw new IOException("You must first call Classifier#train");
            }
            double max = - double.MaxValue;
            BytesRef foundClass = new BytesRef();

            Terms terms = MultiFields.GetTerms(atomicReader, classFieldName);
            TermsEnum termsEnum = terms.GetEnumerator();
            BytesRef next;
            string[] tokenizedDoc = TokenizeDoc(inputDocument);
            while (termsEnum.MoveNext()) 
            {
                next = termsEnum.Term;
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


        private double CalculateLogLikelihood(string[] tokenizedDoc, BytesRef c)
        {
            // for each word
            double result = 0d;
            foreach (string word in tokenizedDoc) 
            {
                // search with text:word AND class:c
                int hits = GetWordFreqForClass(word, c);

                // num : count the no of times the word appears in documents of class c (+1)
                double num = hits + 1; // +1 is added because of add 1 smoothing

                // den : for the whole dictionary, count the no of times a word appears in documents of class c (+|V|)
                double den = GetTextTermFreqForClass(c) + docsWithClassSize;

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
            foreach (string textFieldName in textFieldNames) 
            {
                Terms terms = MultiFields.GetTerms(atomicReader, textFieldName);
                long numPostings = terms.SumDocFreq; // number of term/doc pairs
                avgNumberOfUniqueTerms += numPostings / (double) terms.DocCount; // avg # of unique terms per doc
            }
            int docsWithC = atomicReader.DocFreq(new Term(classFieldName, c));
            return avgNumberOfUniqueTerms * docsWithC; // avg # of unique terms in text fields per doc * # docs with c
        }

        private int GetWordFreqForClass(string word, BytesRef c)
        {
            BooleanQuery booleanQuery = new BooleanQuery();
            BooleanQuery subQuery = new BooleanQuery();
            foreach (string textFieldName in textFieldNames) 
            {
                subQuery.Add(new BooleanClause(new TermQuery(new Term(textFieldName, word)), Occur.SHOULD));
            }
            booleanQuery.Add(new BooleanClause(subQuery, Occur.MUST));
            booleanQuery.Add(new BooleanClause(new TermQuery(new Term(classFieldName, c)), Occur.MUST));
            if (query != null) 
            {
                booleanQuery.Add(query, Occur.MUST);
            }
            TotalHitCountCollector totalHitCountCollector = new TotalHitCountCollector();
            indexSearcher.Search(booleanQuery, totalHitCountCollector);
            return totalHitCountCollector.TotalHits;
        }

        private double CalculateLogPrior(BytesRef currentClass)
        {
            return Math.Log((double) DocCount(currentClass)) - Math.Log(docsWithClassSize);
        }

        private int DocCount(BytesRef countedClass) 
        {
            return atomicReader.DocFreq(new Term(classFieldName, countedClass));
        }
    }   
}