// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Id
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
    /// Tests <seealso cref="IndonesianStemmer"/>
    /// </summary>
    public class TestIndonesianStemmer : BaseTokenStreamTestCase
    {
        /* full stemming, no stopwords */
        internal static readonly Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new KeywordTokenizer(reader);
            return new TokenStreamComponents(tokenizer, new IndonesianStemFilter(tokenizer));
        });

        /// <summary>
        /// Some examples from the paper </summary>
        [Test]
        public virtual void TestExamples()
        {
            CheckOneTerm(a, "bukukah", "buku");
            CheckOneTerm(a, "adalah", "ada");
            CheckOneTerm(a, "bukupun", "buku");
            CheckOneTerm(a, "bukuku", "buku");
            CheckOneTerm(a, "bukumu", "buku");
            CheckOneTerm(a, "bukunya", "buku");
            CheckOneTerm(a, "mengukur", "ukur");
            CheckOneTerm(a, "menyapu", "sapu");
            CheckOneTerm(a, "menduga", "duga");
            CheckOneTerm(a, "menuduh", "uduh");
            CheckOneTerm(a, "membaca", "baca");
            CheckOneTerm(a, "merusak", "rusak");
            CheckOneTerm(a, "pengukur", "ukur");
            CheckOneTerm(a, "penyapu", "sapu");
            CheckOneTerm(a, "penduga", "duga");
            CheckOneTerm(a, "pembaca", "baca");
            CheckOneTerm(a, "diukur", "ukur");
            CheckOneTerm(a, "tersapu", "sapu");
            CheckOneTerm(a, "kekasih", "kasih");
            CheckOneTerm(a, "berlari", "lari");
            CheckOneTerm(a, "belajar", "ajar");
            CheckOneTerm(a, "bekerja", "kerja");
            CheckOneTerm(a, "perjelas", "jelas");
            CheckOneTerm(a, "pelajar", "ajar");
            CheckOneTerm(a, "pekerja", "kerja");
            CheckOneTerm(a, "tarikkan", "tarik");
            CheckOneTerm(a, "ambilkan", "ambil");
            CheckOneTerm(a, "mengambilkan", "ambil");
            CheckOneTerm(a, "makanan", "makan");
            CheckOneTerm(a, "janjian", "janji");
            CheckOneTerm(a, "perjanjian", "janji");
            CheckOneTerm(a, "tandai", "tanda");
            CheckOneTerm(a, "dapati", "dapat");
            CheckOneTerm(a, "mendapati", "dapat");
            CheckOneTerm(a, "pantai", "panta");
        }

        /// <summary>
        /// Some detailed analysis examples (that might not be the best) </summary>
        [Test]
        public virtual void TestIRExamples()
        {
            CheckOneTerm(a, "penyalahgunaan", "salahguna");
            CheckOneTerm(a, "menyalahgunakan", "salahguna");
            CheckOneTerm(a, "disalahgunakan", "salahguna");

            CheckOneTerm(a, "pertanggungjawaban", "tanggungjawab");
            CheckOneTerm(a, "mempertanggungjawabkan", "tanggungjawab");
            CheckOneTerm(a, "dipertanggungjawabkan", "tanggungjawab");

            CheckOneTerm(a, "pelaksanaan", "laksana");
            CheckOneTerm(a, "pelaksana", "laksana");
            CheckOneTerm(a, "melaksanakan", "laksana");
            CheckOneTerm(a, "dilaksanakan", "laksana");

            CheckOneTerm(a, "melibatkan", "libat");
            CheckOneTerm(a, "terlibat", "libat");

            CheckOneTerm(a, "penculikan", "culik");
            CheckOneTerm(a, "menculik", "culik");
            CheckOneTerm(a, "diculik", "culik");
            CheckOneTerm(a, "penculik", "culik");

            CheckOneTerm(a, "perubahan", "ubah");
            CheckOneTerm(a, "peledakan", "ledak");
            CheckOneTerm(a, "penanganan", "tangan");
            CheckOneTerm(a, "kepolisian", "polisi");
            CheckOneTerm(a, "kenaikan", "naik");
            CheckOneTerm(a, "bersenjata", "senjata");
            CheckOneTerm(a, "penyelewengan", "seleweng");
            CheckOneTerm(a, "kecelakaan", "celaka");
        }

        /* inflectional-only stemming */
        internal static readonly Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new KeywordTokenizer(reader);
            return new TokenStreamComponents(tokenizer, new IndonesianStemFilter(tokenizer, false));
        });

        /// <summary>
        /// Test stemming only inflectional suffixes </summary>
        [Test]
        public virtual void TestInflectionalOnly()
        {
            CheckOneTerm(b, "bukunya", "buku");
            CheckOneTerm(b, "bukukah", "buku");
            CheckOneTerm(b, "bukunyakah", "buku");
            CheckOneTerm(b, "dibukukannya", "dibukukan");
        }

        [Test]
        public virtual void TestShouldntStem()
        {
            CheckOneTerm(a, "bersenjata", "senjata");
            CheckOneTerm(a, "bukukah", "buku");
            CheckOneTerm(a, "gigi", "gigi");
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new IndonesianStemFilter(tokenizer));
            });
            CheckOneTerm(a, "", "");
        }
    }
}