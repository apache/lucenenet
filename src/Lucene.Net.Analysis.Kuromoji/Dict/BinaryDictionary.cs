using J2N;
using J2N.IO;
using J2N.Numerics;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.IO;
using System.Security;

namespace Lucene.Net.Analysis.Ja.Dict
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
    /// Base class for a binary-encoded in-memory dictionary.
    /// <para/>
    /// NOTE: To use an alternate dicationary than the built-in one, put the data files in a subdirectory of
    /// your application named "kuromoji-data". This subdirectory
    /// can be placed in any directory up to and including the root directory (if the OS permission allows).
    /// To place the files in an alternate location, set an environment variable named "kuromoji.data.dir"
    /// with the name of the directory the data files can be located within.
    /// </summary>
    public abstract class BinaryDictionary : IDictionary
    {
        public static readonly string DICT_FILENAME_SUFFIX = "$buffer.dat";
        public static readonly string TARGETMAP_FILENAME_SUFFIX = "$targetMap.dat";
        public static readonly string POSDICT_FILENAME_SUFFIX = "$posDict.dat";

        public static readonly string DICT_HEADER = "kuromoji_dict";
        public static readonly string TARGETMAP_HEADER = "kuromoji_dict_map";
        public static readonly string POSDICT_HEADER = "kuromoji_dict_pos";
        public static readonly int VERSION = 1;

        private readonly ByteBuffer buffer;
        private readonly int[] targetMapOffsets, targetMap;
        private readonly string[] posDict;
        private readonly string[] inflTypeDict;
        private readonly string[] inflFormDict;

        // LUCENENET specific - variable to hold the name of the data directory (or empty string to load embedded resources)
        private static readonly string DATA_DIR = LoadDataDir();
        // LUCENENET specific - name of the subdirectory inside of the directory where the Kuromoji dictionary files reside.
        private const string DATA_SUBDIR = "kuromoji-data";

        private static string LoadDataDir()
        {
            // LUCENENET specific - reformatted with :, renamed from "analysis.data.dir"
            string currentPath = SystemProperties.GetProperty("kuromoji:data:dir", AppDomain.CurrentDomain.BaseDirectory);

            // If a matching directory path is found, set our DATA_DIR static
            // variable. If it is null or empty after this process, we need to
            // load the embedded files.
            string candidatePath = System.IO.Path.Combine(currentPath, DATA_SUBDIR);
            if (System.IO.Directory.Exists(candidatePath))
            {
                return candidatePath;
            }

            while (new DirectoryInfo(currentPath).Parent != null)
            {
                try
                {
                    candidatePath = System.IO.Path.Combine(new DirectoryInfo(currentPath).Parent.FullName, DATA_SUBDIR);
                    if (System.IO.Directory.Exists(candidatePath))
                    {
                        return candidatePath;
                    }
                    currentPath = new DirectoryInfo(currentPath).Parent.FullName;
                }
                catch (SecurityException)
                {
                    // ignore security errors
                }
            }

            return null; // This is the signal to load from local resources
        }

        protected BinaryDictionary()
        {
            int[] targetMapOffsets = null, targetMap = null;
            string[] posDict = null;
            string[] inflFormDict = null;
            string[] inflTypeDict = null;
            ByteBuffer buffer; // LUCENENET: IDE0059: Remove unnecessary value assignment

            using (Stream mapIS = GetResource(TARGETMAP_FILENAME_SUFFIX))
            {
                DataInput @in = new InputStreamDataInput(mapIS);
                CodecUtil.CheckHeader(@in, TARGETMAP_HEADER, VERSION, VERSION);
                targetMap = new int[@in.ReadVInt32()];
                targetMapOffsets = new int[@in.ReadVInt32()];
                int accum = 0, sourceId = 0;
                for (int ofs = 0; ofs < targetMap.Length; ofs++)
                {
                    int val = @in.ReadVInt32();
                    if ((val & 0x01) != 0)
                    {
                        targetMapOffsets[sourceId] = ofs;
                        sourceId++;
                    }
                    accum += val.TripleShift(1);
                    targetMap[ofs] = accum;
                }
                if (sourceId + 1 != targetMapOffsets.Length)
                    throw new IOException("targetMap file format broken");
                targetMapOffsets[sourceId] = targetMap.Length;
            }

            using (Stream posIS = GetResource(POSDICT_FILENAME_SUFFIX))
            {
                DataInput @in = new InputStreamDataInput(posIS);
                CodecUtil.CheckHeader(@in, POSDICT_HEADER, VERSION, VERSION);
                int posSize = @in.ReadVInt32();
                posDict = new string[posSize];
                inflTypeDict = new string[posSize];
                inflFormDict = new string[posSize];
                for (int j = 0; j < posSize; j++)
                {
                    posDict[j] = @in.ReadString();
                    inflTypeDict[j] = @in.ReadString();
                    inflFormDict[j] = @in.ReadString();
                    // this is how we encode null inflections
                    if (inflTypeDict[j].Length == 0)
                    {
                        inflTypeDict[j] = null;
                    }
                    if (inflFormDict[j].Length == 0)
                    {
                        inflFormDict[j] = null;
                    }
                }
            }

            ByteBuffer tmpBuffer;

            using (Stream dictIS = GetResource(DICT_FILENAME_SUFFIX))
            {
                // no buffering here, as we load in one large buffer
                DataInput @in = new InputStreamDataInput(dictIS);
                CodecUtil.CheckHeader(@in, DICT_HEADER, VERSION, VERSION);
                int size = @in.ReadVInt32();
                tmpBuffer = ByteBuffer.Allocate(size); // AllocateDirect..?
                int read = dictIS.Read(tmpBuffer.Array, 0, size);
                if (read != size)
                {
                    throw EOFException.Create("Cannot read whole dictionary");
                }
            }
            buffer = tmpBuffer.AsReadOnlyBuffer();

            this.targetMap = targetMap;
            this.targetMapOffsets = targetMapOffsets;
            this.posDict = posDict;
            this.inflTypeDict = inflTypeDict;
            this.inflFormDict = inflFormDict;
            this.buffer = buffer;
        }

        protected Stream GetResource(string suffix)
        {
            return GetTypeResource(GetType(), suffix);
        }

        // util, reused by ConnectionCosts and CharacterDefinition
        public static Stream GetTypeResource(Type clazz, string suffix)
        {
            string fileName = clazz.Name + suffix;

            // LUCENENET specific: Rather than forcing the end user to recompile if they want to use a custom dictionary,
            // we load the data from the kuromoji-data directory (which can be set via the kuromoji.data.dir environment variable).
            if (string.IsNullOrEmpty(DATA_DIR))
            {
                Stream @is = clazz.FindAndGetManifestResourceStream(fileName);
                if (@is is null)
                    throw new FileNotFoundException("Not in assembly: " + clazz.FullName + suffix);
                return @is;
            }

            // We have a data directory, so first check if the file exists
            string path = System.IO.Path.Combine(DATA_DIR, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(string.Format("Expected file '{0}' not found. " +
                    "If the '{1}' directory exists, this file is required. " +
                    "Either remove the '{2}' directory or generate the required dictionary files using the lucene-cli tool.",
                    fileName, DATA_DIR, DATA_SUBDIR));
            }

            // The file exists - open a stream.
            return new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        public virtual void LookupWordIds(int sourceId, Int32sRef @ref)
        {
            @ref.Int32s = targetMap;
            @ref.Offset = targetMapOffsets[sourceId];
            // targetMapOffsets always has one more entry pointing behind last:
            @ref.Length = targetMapOffsets[sourceId + 1] - @ref.Offset;
        }

        public virtual int GetLeftId(int wordId)
        {
            return buffer.GetInt16(wordId).TripleShift(3);
        }

        public virtual int GetRightId(int wordId)
        {
            return buffer.GetInt16(wordId).TripleShift(3);
        }

        public virtual int GetWordCost(int wordId)
        {
            return buffer.GetInt16(wordId + 2);  // Skip id
        }

        public virtual string GetBaseForm(int wordId, char[] surfaceForm, int off, int len)
        {
            if (HasBaseFormData(wordId))
            {
                int offset = BaseFormOffset(wordId);
                int data = buffer.Get(offset++) & 0xff;
                int prefix = data.TripleShift(4);
                int suffix = data & 0xF;
                char[] text = new char[prefix + suffix];
                Arrays.Copy(surfaceForm, off, text, 0, prefix);
                for (int i = 0; i < suffix; i++)
                {
                    text[prefix + i] = buffer.GetChar(offset + (i << 1));
                }
                return new string(text);
            }
            else
            {
                return null;
            }
        }

        public virtual string GetReading(int wordId, char[] surface, int off, int len)
        {
            if (HasReadingData(wordId))
            {
                int offset = ReadingOffset(wordId);
                int readingData = buffer.Get(offset++) & 0xff;
                return ReadString(offset, readingData.TripleShift(1), (readingData & 1) == 1);
            }
            else
            {
                // the reading is the surface form, with hiragana shifted to katakana
                char[] text = new char[len];
                for (int i = 0; i < len; i++)
                {
                    char ch = surface[off + i];
                    if (ch > 0x3040 && ch < 0x3097)
                    {
                        text[i] = (char)(ch + 0x60);
                    }
                    else
                    {
                        text[i] = ch;
                    }
                }
                return new string(text);
            }
        }

        public virtual string GetPartOfSpeech(int wordId)
        {
            return posDict[GetLeftId(wordId)];
        }

        public virtual string GetPronunciation(int wordId, char[] surface, int off, int len)
        {
            if (HasPronunciationData(wordId))
            {
                int offset = PronunciationOffset(wordId);
                int pronunciationData = buffer.Get(offset++) & 0xff;
                return ReadString(offset, pronunciationData.TripleShift(1), (pronunciationData & 1) == 1);
            }
            else
            {
                return GetReading(wordId, surface, off, len); // same as the reading
            }
        }

        public virtual string GetInflectionType(int wordId)
        {
            return inflTypeDict[GetLeftId(wordId)];
        }

        public virtual string GetInflectionForm(int wordId)
        {
            return inflFormDict[GetLeftId(wordId)];
        }

        private static int BaseFormOffset(int wordId)
        {
            return wordId + 4;
        }

        private int ReadingOffset(int wordId)
        {
            int offset = BaseFormOffset(wordId);
            if (HasBaseFormData(wordId))
            {
                int baseFormLength = buffer.Get(offset++) & 0xf;
                return offset + (baseFormLength << 1);
            }
            else
            {
                return offset;
            }
        }

        private int PronunciationOffset(int wordId)
        {
            if (HasReadingData(wordId))
            {
                int offset = ReadingOffset(wordId);
                int readingData = buffer.Get(offset++) & 0xff;
                int readingLength;
                if ((readingData & 1) == 0)
                {
                    readingLength = readingData & 0xfe; // UTF-16: mask off kana bit
                }
                else
                {
                    readingLength = readingData.TripleShift(1);
                }
                return offset + readingLength;
            }
            else
            {
                return ReadingOffset(wordId);
            }
        }

        private bool HasBaseFormData(int wordId)
        {
            return (buffer.GetInt16(wordId) & HAS_BASEFORM) != 0;
        }

        private bool HasReadingData(int wordId)
        {
            return (buffer.GetInt16(wordId) & HAS_READING) != 0;
        }

        private bool HasPronunciationData(int wordId)
        {
            return (buffer.GetInt16(wordId) & HAS_PRONUNCIATION) != 0;
        }

        private string ReadString(int offset, int length, bool kana)
        {
            char[] text = new char[length];
            if (kana)
            {
                for (int i = 0; i < length; i++)
                {
                    text[i] = (char)(0x30A0 + (buffer.Get(offset + i) & 0xff));
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    text[i] = buffer.GetChar(offset + (i << 1));
                }
            }
            return new string(text);
        }

        /// <summary>flag that the entry has baseform data. otherwise its not inflected (same as surface form)</summary>
        public static readonly int HAS_BASEFORM = 1;
        /// <summary>flag that the entry has reading data. otherwise reading is surface form converted to katakana</summary>
        public static readonly int HAS_READING = 2;
        /// <summary>flag that the entry has pronunciation data. otherwise pronunciation is the reading</summary>
        public static readonly int HAS_PRONUNCIATION = 4;
    }
}
