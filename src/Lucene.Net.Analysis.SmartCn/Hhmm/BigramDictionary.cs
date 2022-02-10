// lucene version compatibility level: 4.8.1
using J2N;
using J2N.IO;
using Lucene.Net.Support.Threading;
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Cn.Smart.Hhmm
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
    /// SmartChineseAnalyzer Bigram dictionary.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal class BigramDictionary : AbstractDictionary
    {
        private BigramDictionary()
        {
        }

        public const char WORD_SEGMENT_CHAR = '@';

        private static BigramDictionary singleInstance;

        public const int PRIME_BIGRAM_LENGTH = 402137;

        /// <summary>
        /// The word associations are stored as FNV1 hashcodes, which have a small probability of collision, but save memory.  
        /// </summary>
        private long[] bigramHashTable;

        private int[] frequencyTable;

        private int max = 0;

        //private int repeat = 0; // LUCENENET: Never read

        // static Logger log = Logger.getLogger(BigramDictionary.class);

        private static readonly object syncLock = new object();

        public static BigramDictionary GetInstance()
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                if (singleInstance is null)
                {
                    singleInstance = new BigramDictionary();

                    // LUCENENET specific
                    // LUCENE-1817: https://issues.apache.org/jira/browse/LUCENE-1817
                    // This issue still existed as of 4.8.0. Here is the fix - we only
                    // load from a directory if the actual directory exists (AnalyzerProfile
                    // ensures it is an empty string if it is not available).
                    string dictRoot = AnalyzerProfile.ANALYSIS_DATA_DIR;
                    if (string.IsNullOrEmpty(dictRoot))
                    {
                        singleInstance.Load(); // LUCENENET: No IOException can happen here
                    }
                    else
                    {
                        singleInstance.Load(dictRoot);
                    }
                }
                return singleInstance;
            }
            finally
            {
                UninterruptableMonitor.Exit(syncLock);
            }
        }

        private bool LoadFromObj(FileInfo serialObj)
        {
            try
            {
                using (Stream input = new FileStream(serialObj.FullName, FileMode.Open, FileAccess.Read))
                    LoadFromInputStream(input);
                return true;
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create(e);
            }
        }

        // LUCENENET conversion note:
        // The data in Lucene is stored in a proprietary binary format (similar to
        // .NET's BinarySerializer) that cannot be read back in .NET. Therefore, the
        // data was extracted using Java's DataOutputStream using the following Java code.
        // It can then be read in using the LoadFromInputStream method below 
        // (using a DataInputStream instead of a BinaryReader), and saved
        // in the correct (BinaryWriter) format by calling the SaveToObj method.
        // Alternatively, the data can be loaded from disk using the files
        // here(https://issues.apache.org/jira/browse/LUCENE-1629) in the analysis.data.zip file, 
        // which will automatically produce the .mem files.

        //public void saveToOutputStream(java.io.DataOutputStream stream) throws IOException
        //{
        //    // save wordIndexTable
        //    int wiLen = wordIndexTable.length;
        //    stream.writeInt(wiLen);
        //    for (int i = 0; i<wiLen; i++)
        //    {
        //        stream.writeShort(wordIndexTable[i]);
        //    }

        //    // save charIndexTable
        //    int ciLen = charIndexTable.length;
        //    stream.writeInt(ciLen);
        //    for (int i = 0; i<ciLen; i++)
        //    {
        //        stream.writeChar(charIndexTable[i]);
        //    }

        //    int caDim1 = wordItem_charArrayTable == null ? -1 : wordItem_charArrayTable.length;
        //    stream.writeInt(caDim1);
        //    for (int i = 0; i<caDim1; i++)
        //    {
        //        int caDim2 = wordItem_charArrayTable[i] == null ? -1 : wordItem_charArrayTable[i].length;
        //        stream.writeInt(caDim2);
        //        for (int j = 0; j<caDim2; j++)
        //        {
        //            int caDim3 = wordItem_charArrayTable[i][j] == null ? -1 : wordItem_charArrayTable[i][j].length;
        //            stream.writeInt(caDim3);
        //            for (int k = 0; k<caDim3; k++)
        //            {
        //                stream.writeChar(wordItem_charArrayTable[i][j][k]);
        //            }
        //        }
        //    }

        //    int fDim1 = wordItem_frequencyTable == null ? -1 : wordItem_frequencyTable.length;
        //    stream.writeInt(fDim1);
        //    for (int i = 0; i<fDim1; i++)
        //    {
        //        int fDim2 = wordItem_frequencyTable[i] == null ? -1 : wordItem_frequencyTable[i].length;
        //        stream.writeInt(fDim2);
        //        for (int j = 0; j<fDim2; j++)
        //        {
        //            stream.writeInt(wordItem_frequencyTable[i][j]);
        //        }
        //    }
        //}

        private void LoadFromInputStream(Stream serialObjectInputStream)
        {
            using var reader = new BinaryReader(serialObjectInputStream);
            // Read bigramHashTable
            int bhLen = reader.ReadInt32();
            bigramHashTable = new long[bhLen];
            for (int i = 0; i < bhLen; i++)
            {
                bigramHashTable[i] = reader.ReadInt64();
            }

            // Read frequencyTable
            int fLen = reader.ReadInt32();
            frequencyTable = new int[fLen];
            for (int i = 0; i < fLen; i++)
            {
                frequencyTable[i] = reader.ReadInt32();
            }

            // log.info("load bigram dict from serialization.");
        }

        private void SaveToObj(FileInfo serialObj)
        {
            try
            {
                using Stream output = new FileStream(serialObj.FullName, FileMode.Create, FileAccess.Write);
                using BinaryWriter writer = new BinaryWriter(output);
                int bhLen = bigramHashTable.Length;
                writer.Write(bhLen);
                for (int i = 0; i < bhLen; i++)
                {
                    writer.Write(bigramHashTable[i]);
                }

                int fLen = frequencyTable.Length;
                writer.Write(fLen);
                for (int i = 0; i < fLen; i++)
                {
                    writer.Write(frequencyTable[i]);
                }
                // log.info("serialize bigram dict.");
            }
#pragma warning disable 168, IDE0059
            catch (Exception e) when (e.IsException())
#pragma warning restore 168, IDE0059
            {
                // log.warn(e.getMessage());
            }
        }

        private void Load()
        {
            using Stream input = this.GetType().FindAndGetManifestResourceStream("bigramdict.mem");
            LoadFromInputStream(input);
        }

        private void Load(string dictRoot)
        {
            string bigramDictPath = System.IO.Path.Combine(dictRoot, "bigramdict.dct");

            FileInfo serialObj = new FileInfo(System.IO.Path.Combine(dictRoot, "bigramdict.mem"));

            if (serialObj.Exists && LoadFromObj(serialObj))
            {

            }
            else
            {
                try
                {
                    bigramHashTable = new long[PRIME_BIGRAM_LENGTH];
                    frequencyTable = new int[PRIME_BIGRAM_LENGTH];
                    for (int i = 0; i < PRIME_BIGRAM_LENGTH; i++)
                    {
                        // it is possible for a value to hash to 0, but the probability is extremely low
                        bigramHashTable[i] = 0;
                        frequencyTable[i] = 0;
                    }
                    LoadFromFile(bigramDictPath);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
                SaveToObj(serialObj);
            }
        }

        /// <summary>
        /// Load the datafile into this <see cref="BigramDictionary"/>
        /// </summary>
        /// <param name="dctFilePath">dctFilePath path to the Bigramdictionary (bigramdict.dct)</param>
        /// <exception cref="IOException">If there is a low-level I/O error</exception>
        public virtual void LoadFromFile(string dctFilePath)
        {
            int i, cnt, length, total = 0;
            // The file only counted 6763 Chinese characters plus 5 reserved slots 3756~3760.  
            // The 3756th is used (as a header) to store information.
            int[]
            buffer = new int[3];
            byte[] intBuffer = new byte[4];
            string tmpword;
            //using (RandomAccessFile dctFile = new RandomAccessFile(dctFilePath, "r"))
            using var dctFile = new FileStream(dctFilePath, FileMode.Open, FileAccess.Read);

            // GB2312 characters 0 - 6768
            for (i = GB2312_FIRST_CHAR; i < GB2312_FIRST_CHAR + CHAR_NUM_IN_FILE; i++)
            {
                string currentStr = GetCCByGB2312Id(i);
                // if (i == 5231)
                // System.out.println(i);

                dctFile.Read(intBuffer, 0, intBuffer.Length);
                // the dictionary was developed for C, and byte order must be converted to work with Java
                cnt = ByteBuffer.Wrap(intBuffer).SetOrder(ByteOrder.LittleEndian).GetInt32();
                if (cnt <= 0)
                {
                    continue;
                }
                total += cnt;
                int j = 0;
                while (j < cnt)
                {
                    dctFile.Read(intBuffer, 0, intBuffer.Length);
                    buffer[0] = ByteBuffer.Wrap(intBuffer).SetOrder(ByteOrder.LittleEndian)
                        .GetInt32();// frequency
                    dctFile.Read(intBuffer, 0, intBuffer.Length);
                    buffer[1] = ByteBuffer.Wrap(intBuffer).SetOrder(ByteOrder.LittleEndian)
                        .GetInt32();// length
                    dctFile.Read(intBuffer, 0, intBuffer.Length);
                    // buffer[2] = ByteBuffer.wrap(intBuffer).order(
                    // ByteOrder.LITTLE_ENDIAN).getInt();// handle

                    length = buffer[1];
                    if (length > 0)
                    {
                        byte[] lchBuffer = new byte[length];
                        dctFile.Read(lchBuffer, 0, lchBuffer.Length);
                        //tmpword = new String(lchBuffer, "GB2312");
                        tmpword = Encoding.GetEncoding("GB2312").GetString(lchBuffer);
                        //tmpword = Encoding.GetEncoding("hz-gb-2312").GetString(lchBuffer);
                        if (i != 3755 + GB2312_FIRST_CHAR)
                        {
                            tmpword = currentStr + tmpword;
                        }
                        char[] carray = tmpword.ToCharArray();
                        long hashId = Hash1(carray);
                        int index = GetAvaliableIndex(hashId, carray);
                        if (index != -1)
                        {
                            if (bigramHashTable[index] == 0)
                            {
                                bigramHashTable[index] = hashId;
                                // bigramStringTable[index] = tmpword;
                            }
                            frequencyTable[index] += buffer[0];
                        }
                    }
                    j++;
                }
            }
            // log.info("load dictionary done! " + dctFilePath + " total:" + total);
        }

        private int GetAvaliableIndex(long hashId, char[] carray)
        {
            int hash1 = (int)(hashId % PRIME_BIGRAM_LENGTH);
            int hash2 = Hash2(carray) % PRIME_BIGRAM_LENGTH;
            if (hash1 < 0)
                hash1 = PRIME_BIGRAM_LENGTH + hash1;
            if (hash2 < 0)
                hash2 = PRIME_BIGRAM_LENGTH + hash2;
            int index = hash1;
            int i = 1;
            while (bigramHashTable[index] != 0 && bigramHashTable[index] != hashId
                && i < PRIME_BIGRAM_LENGTH)
            {
                index = (hash1 + i * hash2) % PRIME_BIGRAM_LENGTH;
                i++;
            }
            // System.out.println(i - 1);

            if (i < PRIME_BIGRAM_LENGTH
                && (bigramHashTable[index] == 0 || bigramHashTable[index] == hashId))
            {
                return index;
            }
            else
                return -1;
        }

        /// <summary>
        /// lookup the index into the frequency array.
        /// </summary>
        private int GetBigramItemIndex(char[] carray)
        {
            long hashId = Hash1(carray);
            int hash1 = (int)(hashId % PRIME_BIGRAM_LENGTH);
            int hash2 = Hash2(carray) % PRIME_BIGRAM_LENGTH;
            if (hash1 < 0)
                hash1 = PRIME_BIGRAM_LENGTH + hash1;
            if (hash2 < 0)
                hash2 = PRIME_BIGRAM_LENGTH + hash2;
            int index = hash1;
            int i = 1;
            //repeat++; // LUCENENET: Never read
            while (bigramHashTable[index] != 0 && bigramHashTable[index] != hashId
                && i < PRIME_BIGRAM_LENGTH)
            {
                index = (hash1 + i * hash2) % PRIME_BIGRAM_LENGTH;
                i++;
                //repeat++; // LUCENENET: Never read
                if (i > max)
                    max = i;
            }
            // System.out.println(i - 1);

            if (i < PRIME_BIGRAM_LENGTH && bigramHashTable[index] == hashId)
            {
                return index;
            }
            else
                return -1;
        }

        public int GetFrequency(char[] carray)
        {
            int index = GetBigramItemIndex(carray);
            if (index != -1)
                return frequencyTable[index];
            return 0;
        }
    }
}
