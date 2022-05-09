using J2N.Text;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using Lucene.Net.Util.Packed;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;
using Int64 = J2N.Numerics.Int64;

namespace Lucene.Net.Analysis.Ja.Util
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

    public class TokenInfoDictionaryBuilder
    {
        /// <summary>Internal word id - incrementally assigned as entries are read and added. This will be byte offset of dictionary file</summary>
        private int offset = 0;

        private readonly string encoding = "euc-jp"; // LUCENENET: marked readonly

        private readonly bool normalizeEntries = false; // LUCENENET: marked readonly
        //private Normalizer2 normalizer;

        private readonly DictionaryBuilder.DictionaryFormat format = DictionaryBuilder.DictionaryFormat.IPADIC; // LUCENENET: marked readonly

        public TokenInfoDictionaryBuilder(DictionaryBuilder.DictionaryFormat format, string encoding, bool normalizeEntries)
        {
            this.format = format;
            this.encoding = encoding;
            this.normalizeEntries = normalizeEntries;
            //this.normalizer = normalizeEntries ? Normalizer2.getInstance(null, "nfkc", Normalizer2.Mode.COMPOSE) : null;
        }

        public virtual TokenInfoDictionaryWriter Build(string dirname)
        {
            JCG.List<string> csvFiles = new JCG.List<string>();
            foreach (FileInfo file in new DirectoryInfo(dirname).EnumerateFiles("*.csv"))
            {
                csvFiles.Add(file.FullName);
            }
            csvFiles.Sort(StringComparer.Ordinal);
            return BuildDictionary(csvFiles);
        }

        public virtual TokenInfoDictionaryWriter BuildDictionary(IList<string> csvFiles)
        {
            TokenInfoDictionaryWriter dictionary = new TokenInfoDictionaryWriter(10 * 1024 * 1024);

            // all lines in the file
            Console.WriteLine("  parse...");
            JCG.List<string[]> lines = new JCG.List<string[]>(400000);
            foreach (string file in csvFiles)
            {
                using Stream inputStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                Encoding decoder = Encoding.GetEncoding(encoding);
                TextReader reader = new StreamReader(inputStream, decoder);

                string line = null;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] entry = CSVUtil.Parse(line);

                    if (entry.Length < 13)
                    {
                        Console.WriteLine("Entry in CSV is not valid: " + line);
                        continue;
                    }

                    string[] formatted = FormatEntry(entry);
                    lines.Add(formatted);

                    // NFKC normalize dictionary entry
                    if (normalizeEntries)
                    {
                        //if (normalizer.isNormalized(entry[0])){
                        if (entry[0].IsNormalized(NormalizationForm.FormKC))
                        {
                            continue;
                        }
                        string[] normalizedEntry = new string[entry.Length];
                        for (int i = 0; i < entry.Length; i++)
                        {
                            //normalizedEntry[i] = normalizer.normalize(entry[i]);
                            normalizedEntry[i] = entry[i].Normalize(NormalizationForm.FormKC);
                        }

                        formatted = FormatEntry(normalizedEntry);
                        lines.Add(formatted);
                    }
                }
            }

            Console.WriteLine("  sort...");

            // sort by term: we sorted the files already and use a stable sort.
            lines.Sort(Comparer<string[]>.Create((left, right) => left[0].CompareToOrdinal(right[0])));

            Console.WriteLine("  encode...");

            PositiveInt32Outputs fstOutput = PositiveInt32Outputs.Singleton;
            Builder<Int64> fstBuilder = new Builder<Int64>(FST.INPUT_TYPE.BYTE2, 0, 0, true, true, int.MaxValue, fstOutput, null, true, PackedInt32s.DEFAULT, true, 15);
            Int32sRef scratch = new Int32sRef();
            long ord = -1; // first ord will be 0
            string lastValue = null;

            // build tokeninfo dictionary
            foreach (string[] entry in lines)
            {
                int next = dictionary.Put(entry);

                if (next == offset)
                {
                    Console.WriteLine("Failed to process line: " + Collections.ToString(entry));
                    continue;
                }

                string token = entry[0];
                if (!token.Equals(lastValue, StringComparison.Ordinal))
                {
                    // new word to add to fst
                    ord++;
                    lastValue = token;
                    scratch.Grow(token.Length);
                    scratch.Length = token.Length;
                    for (int i = 0; i < token.Length; i++)
                    {
                        scratch.Int32s[i] = (int)token[i];
                    }
                    fstBuilder.Add(scratch, ord);
                }
                dictionary.AddMapping((int)ord, offset);
                offset = next;
            }

            FST<Int64> fst = fstBuilder.Finish();

            Console.WriteLine("  " + fst.NodeCount + " nodes, " + fst.ArcCount + " arcs, " + fst.GetSizeInBytes() + " bytes...  ");
            dictionary.SetFST(fst);
            Console.WriteLine(" done");

            return dictionary;
        }
        
        /// <summary>
        /// IPADIC features
        /// 
        /// 0   - surface
        /// 1   - left cost
        /// 2   - right cost
        /// 3   - word cost
        /// 4-9 - pos
        /// 10  - base form
        /// 11  - reading
        /// 12  - pronounciation
        /// 
        /// UniDic features
        /// 
        /// 0   - surface
        /// 1   - left cost
        /// 2   - right cost
        /// 3   - word cost
        /// 4-9 - pos
        /// 10  - base form reading
        /// 11  - base form
        /// 12  - surface form
        /// 13  - surface reading
        /// </summary>
        public virtual string[] FormatEntry(string[] features)
        {
            if (this.format == DictionaryBuilder.DictionaryFormat.IPADIC)
            {
                return features;
            }
            else
            {
                string[] features2 = new string[13];
                features2[0] = features[0];
                features2[1] = features[1];
                features2[2] = features[2];
                features2[3] = features[3];
                features2[4] = features[4];
                features2[5] = features[5];
                features2[6] = features[6];
                features2[7] = features[7];
                features2[8] = features[8];
                features2[9] = features[9];
                features2[10] = features[11];

                // If the surface reading is non-existent, use surface form for reading and pronunciation.
                // This happens with punctuation in UniDic and there are possibly other cases as well
                if (features[13].Length == 0)
                {
                    features2[11] = features[0];
                    features2[12] = features[0];
                }
                else
                {
                    features2[11] = features[13];
                    features2[12] = features[13];
                }
                return features2;
            }
        }
    }
}
