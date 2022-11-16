// lucene version compatibility level: 4.8.1
using J2N;
using J2N.IO;
using Lucene.Net.Support;
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
    /// SmartChineseAnalyzer Word Dictionary
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal class WordDictionary : AbstractDictionary
    {
        private WordDictionary()
        {
        }

        private static WordDictionary singleInstance;

        /// <summary>
        /// Large prime number for hash function
        /// </summary>
        public const int PRIME_INDEX_LENGTH = 12071;

        /// <summary>
        /// wordIndexTable guarantees to hash all Chinese characters in Unicode into 
        /// PRIME_INDEX_LENGTH array. There will be conflict, but in reality this 
        /// program only handles the 6768 characters found in GB2312 plus some 
        /// ASCII characters. Therefore in order to guarantee better precision, it is
        /// necessary to retain the original symbol in the charIndexTable.
        /// </summary>
        private short[] wordIndexTable;

        private char[] charIndexTable;

        /// <summary>
        /// To avoid taking too much space, the data structure needed to store the 
        /// lexicon requires two multidimensional arrays to store word and frequency.
        /// Each word is placed in a char[]. Each char represents a Chinese char or 
        /// other symbol.  Each frequency is put into an int. These two arrays 
        /// correspond to each other one-to-one. Therefore, one can use 
        /// wordItem_charArrayTable[i][j] to look up word from lexicon, and 
        /// wordItem_frequencyTable[i][j] to look up the corresponding frequency. 
        /// </summary>
        private char[][][] wordItem_charArrayTable;

        private int[][] wordItem_frequencyTable;

        // static Logger log = Logger.getLogger(WordDictionary.class);

        private static readonly object syncLock = new object();

        /// <summary>
        /// Get the singleton dictionary instance.
        /// </summary>
        /// <returns>singleton</returns>
        public static WordDictionary GetInstance()
        {
            UninterruptableMonitor.Enter(syncLock);
            try
            {
                if (singleInstance is null)
                {
                    singleInstance = new WordDictionary();

                    // LUCENENET specific
                    // LUCENE-1817: https://issues.apache.org/jira/browse/LUCENE-1817
                    // This issue still existed as of 4.8.0. Here is the fix - we only
                    // load from a directory if the actual directory exists (AnalyzerProfile
                    // ensures it is an empty string if it is not available).
                    string dictRoot = AnalyzerProfile.ANALYSIS_DATA_DIR;
                    if (string.IsNullOrEmpty(dictRoot))
                    {
                        singleInstance.Load(); // LUCENENET: No IOExcpetion can happen here
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

        /// <summary>
        /// Attempt to load dictionary from provided directory, first trying coredict.mem, failing back on coredict.dct
        /// </summary>
        /// <param name="dctFileRoot">path to dictionary directory</param>
        public virtual void Load(string dctFileRoot)
        {
            string dctFilePath = System.IO.Path.Combine(dctFileRoot, "coredict.dct");
            FileInfo serialObj = new FileInfo(System.IO.Path.Combine(dctFileRoot, "coredict.mem"));

            if (serialObj.Exists && LoadFromObj(serialObj))
            {

            }
            else
            {
                try
                {
                    wordIndexTable = new short[PRIME_INDEX_LENGTH];
                    charIndexTable = new char[PRIME_INDEX_LENGTH];
                    for (int i = 0; i < PRIME_INDEX_LENGTH; i++)
                    {
                        charIndexTable[i] = (char)0;
                        wordIndexTable[i] = -1;
                    }
                    wordItem_charArrayTable = new char[GB2312_CHAR_NUM][][];
                    wordItem_frequencyTable = new int[GB2312_CHAR_NUM][];
                    // int total =
                    LoadMainDataFromFile(dctFilePath);
                    ExpandDelimiterData();
                    MergeSameWords();
                    SortEachItems();
                    // log.info("load dictionary: " + dctFilePath + " total:" + total);
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e); // LUCENENET: Passing class so we can wrap it
                }

                SaveToObj(serialObj);
            }

        }

        /// <summary>
        /// Load coredict.mem internally from the jar file.
        /// </summary>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        public virtual void Load()
        {
            using Stream input = this.GetType().FindAndGetManifestResourceStream("coredict.mem");
            LoadFromObjectInputStream(input);
        }

        private bool LoadFromObj(FileInfo serialObj)
        {
            try
            {
                using (Stream input = new FileStream(serialObj.FullName, FileMode.Open, FileAccess.Read))
                    LoadFromObjectInputStream(input);
                return true;
            }
            catch (Exception e)
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
        //    // save bigramHashTable
        //    int bhLen = bigramHashTable.length;
        //    stream.writeInt(bhLen);
        //    for (int i = 0; i<bhLen; i++)
        //    {
        //        stream.writeLong(bigramHashTable[i]);
        //    }

        //    // save frequencyTable
        //    int fLen = frequencyTable.length;
        //    stream.writeInt(fLen);
        //    for (int i = 0; i<fLen; i++)
        //    {
        //        stream.writeInt(frequencyTable[i]);
        //    }
        //}

        private void LoadFromObjectInputStream(Stream serialObjectInputStream)
        {
            using var reader = new BinaryReader(serialObjectInputStream);

            // Read wordIndexTable
            int wiLen = reader.ReadInt32();
            wordIndexTable = new short[wiLen];
            for (int i = 0; i < wiLen; i++)
            {
                wordIndexTable[i] = reader.ReadInt16();
            }

            // Read charIndexTable
            int ciLen = reader.ReadInt32();
            charIndexTable = new char[ciLen];
            for (int i = 0; i < ciLen; i++)
            {
                charIndexTable[i] = reader.ReadChar();
            }

            // Read wordItem_charArrayTable
            int caDim1 = reader.ReadInt32();
            if (caDim1 > -1)
            {
                wordItem_charArrayTable = new char[caDim1][][];
                for (int i = 0; i < caDim1; i++)
                {
                    int caDim2 = reader.ReadInt32();
                    if (caDim2 > -1)
                    {
                        wordItem_charArrayTable[i] = new char[caDim2][];
                        for (int j = 0; j < caDim2; j++)
                        {
                            int caDim3 = reader.ReadInt32();
                            if (caDim3 > -1)
                            {
                                wordItem_charArrayTable[i][j] = new char[caDim3];
                                for (int k = 0; k < caDim3; k++)
                                {
                                    wordItem_charArrayTable[i][j][k] = reader.ReadChar();
                                }
                            }
                        }
                    }
                }
            }

            // Read wordItem_frequencyTable
            int fDim1 = reader.ReadInt32();
            if (fDim1 > -1)
            {
                wordItem_frequencyTable = new int[fDim1][];
                for (int i = 0; i < fDim1; i++)
                {
                    int fDim2 = reader.ReadInt32();
                    if (fDim2 > -1)
                    {
                        wordItem_frequencyTable[i] = new int[fDim2];
                        for (int j = 0; j < fDim2; j++)
                        {
                            wordItem_frequencyTable[i][j] = reader.ReadInt32();
                        }
                    }
                }
            }

            // log.info("load core dict from serialization.");
        }

        private void SaveToObj(FileInfo serialObj)
        {
            try
            {
                using Stream stream = new FileStream(serialObj.FullName, FileMode.Create, FileAccess.Write);
                using var writer = new BinaryWriter(stream);
                // Write wordIndexTable
                int wiLen = wordIndexTable.Length;
                writer.Write(wiLen);
                for (int i = 0; i < wiLen; i++)
                {
                    writer.Write(wordIndexTable[i]);
                }

                // Write charIndexTable
                int ciLen = charIndexTable.Length;
                writer.Write(ciLen);
                for (int i = 0; i < ciLen; i++)
                {
                    writer.Write(charIndexTable[i]);
                }

                // Write wordItem_charArrayTable
                int caDim1 = wordItem_charArrayTable is null ? -1 : wordItem_charArrayTable.Length;
                writer.Write(caDim1);
                for (int i = 0; i < caDim1; i++)
                {
                    int caDim2 = wordItem_charArrayTable[i] is null ? -1 : wordItem_charArrayTable[i].Length;
                    writer.Write(caDim2);
                    for (int j = 0; j < caDim2; j++)
                    {
                        int caDim3 = wordItem_charArrayTable[i][j] is null ? -1 : wordItem_charArrayTable[i][j].Length;
                        writer.Write(caDim3);
                        for (int k = 0; k < caDim3; k++)
                        {
                            writer.Write(wordItem_charArrayTable[i][j][k]);
                        }
                    }
                }

                // Write wordItem_frequencyTable
                int fDim1 = wordItem_frequencyTable is null ? -1 : wordItem_frequencyTable.Length;
                writer.Write(fDim1);
                for (int i = 0; i < fDim1; i++)
                {
                    int fDim2 = wordItem_frequencyTable[i] is null ? -1 : wordItem_frequencyTable[i].Length;
                    writer.Write(fDim2);
                    for (int j = 0; j < fDim2; j++)
                    {
                        writer.Write(wordItem_frequencyTable[i][j]);
                    }
                }

                // log.info("serialize core dict.");
            }
#pragma warning disable 168, IDE0059
            catch (Exception e)
#pragma warning restore 168, IDE0059
            {
                // log.warn(e.getMessage());
            }
        }

        /// <summary>
        /// Load the datafile into this <see cref="WordDictionary"/>
        /// </summary>
        /// <param name="dctFilePath">path to word dictionary (coredict.dct)</param>
        /// <returns>number of words read</returns>
        /// <exception cref="IOException">If there is a low-level I/O error.</exception>
        private int LoadMainDataFromFile(string dctFilePath)
        {
            int i, cnt, length, total = 0;
            // The file only counted 6763 Chinese characters plus 5 reserved slots 3756~3760.
            // The 3756th is used (as a header) to store information.
            int[]
            buffer = new int[3];
            byte[] intBuffer = new byte[4];
            string tmpword;
            using (var dctFile = new FileStream(dctFilePath, FileMode.Open, FileAccess.Read))
            {

                // GB2312 characters 0 - 6768
                for (i = GB2312_FIRST_CHAR; i < GB2312_FIRST_CHAR + CHAR_NUM_IN_FILE; i++)
                {
                    // if (i == 5231)
                    // System.out.println(i);

                    dctFile.Read(intBuffer, 0, intBuffer.Length);
                    // the dictionary was developed for C, and byte order must be converted to work with Java
                    cnt = ByteBuffer.Wrap(intBuffer).SetOrder(ByteOrder.LittleEndian).GetInt32();
                    if (cnt <= 0)
                    {
                        wordItem_charArrayTable[i] = null;
                        wordItem_frequencyTable[i] = null;
                        continue;
                    }
                    wordItem_charArrayTable[i] = new char[cnt][];
                    wordItem_frequencyTable[i] = new int[cnt];
                    total += cnt;
                    int j = 0;
                    while (j < cnt)
                    {
                        // wordItemTable[i][j] = new WordItem();
                        dctFile.Read(intBuffer, 0, intBuffer.Length);
                        buffer[0] = ByteBuffer.Wrap(intBuffer).SetOrder(ByteOrder.LittleEndian)
                            .GetInt32();// frequency
                        dctFile.Read(intBuffer, 0, intBuffer.Length);
                        buffer[1] = ByteBuffer.Wrap(intBuffer).SetOrder(ByteOrder.LittleEndian)
                            .GetInt32();// length
                        dctFile.Read(intBuffer, 0, intBuffer.Length);
                        buffer[2] = ByteBuffer.Wrap(intBuffer).SetOrder(ByteOrder.LittleEndian)
                            .GetInt32();// handle

                        // wordItemTable[i][j].frequency = buffer[0];
                        wordItem_frequencyTable[i][j] = buffer[0];

                        length = buffer[1];
                        if (length > 0)
                        {
                            byte[] lchBuffer = new byte[length];
                            dctFile.Read(lchBuffer, 0, lchBuffer.Length);
                            tmpword = Encoding.GetEncoding("GB2312").GetString(lchBuffer);
                            wordItem_charArrayTable[i][j] = tmpword.ToCharArray();
                        }
                        else
                        {
                            // wordItemTable[i][j].charArray = null;
                            wordItem_charArrayTable[i][j] = null;
                        }
                        // System.out.println(indexTable[i].wordItems[j]);
                        j++;
                    }

                    string str = GetCCByGB2312Id(i);
                    SetTableIndex(str[0], i);
                }
            }
            return total;
        }

        /// <summary>
        /// The original lexicon puts all information with punctuation into a 
        /// chart (from 1 to 3755). Here it then gets expanded, separately being
        /// placed into the chart that has the corresponding symbol.
        /// </summary>
        private void ExpandDelimiterData()
        {
            int i;
            int cnt;
            // Punctuation then treating index 3755 as 1, 
            // distribute the original punctuation corresponding dictionary into 
            int delimiterIndex = 3755 + GB2312_FIRST_CHAR;
            i = 0;
            while (i < wordItem_charArrayTable[delimiterIndex].Length)
            {
                char c = wordItem_charArrayTable[delimiterIndex][i][0];
                int j = GetGB2312Id(c);// the id value of the punctuation
                if (wordItem_charArrayTable[j] is null)
                {

                    int k = i;
                    // Starting from i, count the number of the following worditem symbol from j
                    while (k < wordItem_charArrayTable[delimiterIndex].Length
                        && wordItem_charArrayTable[delimiterIndex][k][0] == c)
                    {
                        k++;
                    }
                    // c is the punctuation character, j is the id value of c
                    // k-1 represents the index of the last punctuation character
                    cnt = k - i;
                    if (cnt != 0)
                    {
                        wordItem_charArrayTable[j] = new char[cnt][];
                        wordItem_frequencyTable[j] = new int[cnt];
                    }

                    // Assign value for each wordItem.
                    for (k = 0; k < cnt; k++, i++)
                    {
                        // wordItemTable[j][k] = new WordItem();
                        wordItem_frequencyTable[j][k] = wordItem_frequencyTable[delimiterIndex][i];
                        wordItem_charArrayTable[j][k] = new char[wordItem_charArrayTable[delimiterIndex][i].Length - 1];
                        Arrays.Copy(wordItem_charArrayTable[delimiterIndex][i], 1,
                            wordItem_charArrayTable[j][k], 0,
                            wordItem_charArrayTable[j][k].Length);
                    }
                    SetTableIndex(c, j);
                }
            }
            // Delete the original corresponding symbol array.
            wordItem_charArrayTable[delimiterIndex] = null;
            wordItem_frequencyTable[delimiterIndex] = null;
        }

        /// <summary>
        /// since we aren't doing POS-tagging, merge the frequencies for entries of the same word (with different POS)
        /// </summary>
        private void MergeSameWords()
        {
            int i;
            for (i = 0; i < GB2312_FIRST_CHAR + CHAR_NUM_IN_FILE; i++)
            {
                if (wordItem_charArrayTable[i] is null)
                    continue;
                int len = 1;
                for (int j = 1; j < wordItem_charArrayTable[i].Length; j++)
                {
                    if (Utility.CompareArray(wordItem_charArrayTable[i][j], 0,
                        wordItem_charArrayTable[i][j - 1], 0) != 0)
                        len++;

                }
                if (len < wordItem_charArrayTable[i].Length)
                {
                    char[][] tempArray = new char[len][];
                    int[] tempFreq = new int[len];
                    int k = 0;
                    tempArray[0] = wordItem_charArrayTable[i][0];
                    tempFreq[0] = wordItem_frequencyTable[i][0];
                    for (int j = 1; j < wordItem_charArrayTable[i].Length; j++)
                    {
                        if (Utility.CompareArray(wordItem_charArrayTable[i][j], 0,
                            tempArray[k], 0) != 0)
                        {
                            k++;
                            // temp[k] = wordItemTable[i][j];
                            tempArray[k] = wordItem_charArrayTable[i][j];
                            tempFreq[k] = wordItem_frequencyTable[i][j];
                        }
                        else
                        {
                            // temp[k].frequency += wordItemTable[i][j].frequency;
                            tempFreq[k] += wordItem_frequencyTable[i][j];
                        }
                    }
                    // wordItemTable[i] = temp;
                    wordItem_charArrayTable[i] = tempArray;
                    wordItem_frequencyTable[i] = tempFreq;
                }
            }
        }

        private void SortEachItems()
        {
            char[] tmpArray;
            int tmpFreq;
            for (int i = 0; i < wordItem_charArrayTable.Length; i++)
            {
                if (wordItem_charArrayTable[i] != null
                    && wordItem_charArrayTable[i].Length > 1)
                {
                    for (int j = 0; j < wordItem_charArrayTable[i].Length - 1; j++)
                    {
                        for (int j2 = j + 1; j2 < wordItem_charArrayTable[i].Length; j2++)
                        {
                            if (Utility.CompareArray(wordItem_charArrayTable[i][j], 0,
                                wordItem_charArrayTable[i][j2], 0) > 0)
                            {
                                tmpArray = wordItem_charArrayTable[i][j];
                                tmpFreq = wordItem_frequencyTable[i][j];
                                wordItem_charArrayTable[i][j] = wordItem_charArrayTable[i][j2];
                                wordItem_frequencyTable[i][j] = wordItem_frequencyTable[i][j2];
                                wordItem_charArrayTable[i][j2] = tmpArray;
                                wordItem_frequencyTable[i][j2] = tmpFreq;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate character <paramref name="c"/>'s position in hash table, 
        /// then initialize the value of that position in the address table.
        /// </summary>
        private bool SetTableIndex(char c, int j)
        {
            int index = GetAvaliableTableIndex(c);
            if (index != -1)
            {
                charIndexTable[index] = c;
                wordIndexTable[index] = (short)j;
                return true;
            }
            else
                return false;
        }

        private short GetAvaliableTableIndex(char c)
        {
            int hash1 = (int)(Hash1(c) % PRIME_INDEX_LENGTH);
            int hash2 = Hash2(c) % PRIME_INDEX_LENGTH;
            if (hash1 < 0)
                hash1 = PRIME_INDEX_LENGTH + hash1;
            if (hash2 < 0)
                hash2 = PRIME_INDEX_LENGTH + hash2;
            int index = hash1;
            int i = 1;
            while (charIndexTable[index] != 0 && charIndexTable[index] != c
                && i < PRIME_INDEX_LENGTH)
            {
                index = (hash1 + i * hash2) % PRIME_INDEX_LENGTH;
                i++;
            }
            // System.out.println(i - 1);

            if (i < PRIME_INDEX_LENGTH
                && (charIndexTable[index] == 0 || charIndexTable[index] == c))
            {
                return (short)index;
            }
            else
            {
                return -1;
            }
        }

        private short GetWordItemTableIndex(char c)
        {
            int hash1 = (int)(Hash1(c) % PRIME_INDEX_LENGTH);
            int hash2 = Hash2(c) % PRIME_INDEX_LENGTH;
            if (hash1 < 0)
                hash1 = PRIME_INDEX_LENGTH + hash1;
            if (hash2 < 0)
                hash2 = PRIME_INDEX_LENGTH + hash2;
            int index = hash1;
            int i = 1;
            while (charIndexTable[index] != 0 && charIndexTable[index] != c
                && i < PRIME_INDEX_LENGTH)
            {
                index = (hash1 + i * hash2) % PRIME_INDEX_LENGTH;
                i++;
            }

            if (i < PRIME_INDEX_LENGTH && charIndexTable[index] == c)
            {
                return (short)index;
            }
            else
                return -1;
        }

        /// <summary>
        /// Look up the text string corresponding with the word char array,
        /// and return the position of the word list.
        /// </summary>
        /// <param name="knownHashIndex">
        /// already figure out position of the first word
        /// symbol charArray[0] in hash table. If not calculated yet, can be
        /// replaced with function int findInTable(char[] charArray).
        /// </param>
        /// <param name="charArray">look up the char array corresponding with the word.</param>
        /// <returns>word location in word array.  If not found, then return -1.</returns>
        private int FindInTable(short knownHashIndex, char[] charArray)
        {
            if (charArray is null || charArray.Length == 0)
                return -1;

            char[][] items = wordItem_charArrayTable[wordIndexTable[knownHashIndex]];
            int start = 0, end = items.Length - 1;
            int mid = (start + end) / 2, cmpResult;

            // Binary search for the index of idArray
            while (start <= end)
            {
                cmpResult = Utility.CompareArray(items[mid], 0, charArray, 1);

                if (cmpResult == 0)
                    return mid;// find it
                else if (cmpResult < 0)
                    start = mid + 1;
                else if (cmpResult > 0)
                    end = mid - 1;

                mid = (start + end) / 2;
            }
            return -1;
        }

        /// <summary>
        /// Find the first word in the dictionary that starts with the supplied prefix
        /// </summary>
        /// <param name="charArray">input prefix</param>
        /// <returns>index of word, or -1 if not found</returns>
        /// <seealso cref="GetPrefixMatch(char[], int)"/>
        public virtual int GetPrefixMatch(char[] charArray)
        {
            return GetPrefixMatch(charArray, 0);
        }

        /// <summary>
        /// Find the nth word in the dictionary that starts with the supplied prefix
        /// </summary>
        /// <param name="charArray">input prefix</param>
        /// <param name="knownStart">relative position in the dictionary to start</param>
        /// <returns>index of word, or -1 if not found</returns>
        /// <seealso cref="GetPrefixMatch(char[])"/>
        public virtual int GetPrefixMatch(char[] charArray, int knownStart)
        {
            short index = GetWordItemTableIndex(charArray[0]);
            if (index == -1)
                return -1;
            char[][] items = wordItem_charArrayTable[wordIndexTable[index]];
            int start = knownStart, end = items.Length - 1;

            int mid = (start + end) / 2, cmpResult;

            // Binary search for the index of idArray
            while (start <= end)
            {
                cmpResult = Utility.CompareArrayByPrefix(charArray, 1, items[mid], 0);
                if (cmpResult == 0)
                {
                    // Get the first item which match the current word
                    while (mid >= 0
                        && Utility.CompareArrayByPrefix(charArray, 1, items[mid], 0) == 0)
                        mid--;
                    mid++;
                    return mid;// Find the first word that uses charArray as prefix.
                }
                else if (cmpResult < 0)
                    end = mid - 1;
                else
                    start = mid + 1;
                mid = (start + end) / 2;
            }
            return -1;
        }

        /// <summary>
        /// Get the frequency of a word from the dictionary
        /// </summary>
        /// <param name="charArray">input word</param>
        /// <returns>word frequency, or zero if the word is not found</returns>
        public virtual int GetFrequency(char[] charArray)
        {
            short hashIndex = GetWordItemTableIndex(charArray[0]);
            if (hashIndex == -1)
            {
                return 0;
            }
            int itemIndex = FindInTable(hashIndex, charArray);
            if (itemIndex != -1)
            {
                return wordItem_frequencyTable[wordIndexTable[hashIndex]][itemIndex];
            }
            return 0;
        }

        /// <summary>
        /// Return <c>true</c> if the dictionary entry at itemIndex for table charArray[0] is charArray
        /// </summary>
        /// <param name="charArray">input word</param>
        /// <param name="itemIndex">item index for table charArray[0]</param>
        /// <returns><c>true</c> if the entry exists</returns>
        public virtual bool IsEqual(char[] charArray, int itemIndex)
        {
            short hashIndex = GetWordItemTableIndex(charArray[0]);
            return Utility.CompareArray(charArray, 1,
                wordItem_charArrayTable[wordIndexTable[hashIndex]][itemIndex], 0) == 0;
        }
    }
}
