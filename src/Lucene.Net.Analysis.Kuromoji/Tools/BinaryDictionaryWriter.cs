using J2N.IO;
using Lucene.Net.Analysis.Ja.Dict;
using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

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

    public abstract class BinaryDictionaryWriter
    {
        protected readonly Type m_implClazz;
        protected ByteBuffer m_buffer;
        private int targetMapEndOffset = 0, lastWordId = -1, lastSourceId = -1;
        private int[] targetMap = new int[8192];
        private int[] targetMapOffsets = new int[8192];
        private readonly List<string> posDict = new List<string>();

        public BinaryDictionaryWriter(Type implClazz, int size)
        {
            this.m_implClazz = implClazz;
            m_buffer = ByteBuffer.Allocate(size);
        }

        /// <summary>
        /// Put the entry in map.
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>Current position of buffer, which will be wordId of next entry.</returns>
        public virtual int Put(string[] entry)
        {
            short leftId = short.Parse(entry[1], CultureInfo.InvariantCulture);
            short rightId = short.Parse(entry[2], CultureInfo.InvariantCulture);
            short wordCost = short.Parse(entry[3], CultureInfo.InvariantCulture);

            StringBuilder sb = new StringBuilder();

            // build up the POS string
            for (int i = 4; i < 8; i++)
            {
                string part = entry[i];
                Debug.Assert(part.Length > 0);
                if (!"*".Equals(part, StringComparison.Ordinal))
                {
                    if (sb.Length > 0)
                    {
                        sb.Append('-');
                    }
                    sb.Append(part);
                }
            }

            string posData = sb.ToString();

            sb.Length = 0;
            sb.Append(CSVUtil.QuoteEscape(posData));
            sb.Append(',');
            if (!"*".Equals(entry[8], StringComparison.Ordinal))
            {
                sb.Append(CSVUtil.QuoteEscape(entry[8]));
            }
            sb.Append(',');
            if (!"*".Equals(entry[9], StringComparison.Ordinal))
            {
                sb.Append(CSVUtil.QuoteEscape(entry[9]));
            }
            string fullPOSData = sb.ToString();

            string baseForm = entry[10];
            string reading = entry[11];
            string pronunciation = entry[12];

            // extend buffer if necessary
            int left = m_buffer.Remaining;
            // worst case: two short, 3 bytes, and features (all as utf-16)
            int worstCase = 4 + 3 + 2 * (baseForm.Length + reading.Length + pronunciation.Length);
            if (worstCase > left)
            {
                ByteBuffer newBuffer = ByteBuffer.Allocate(ArrayUtil.Oversize(m_buffer.Limit + worstCase - left, 1));
                m_buffer.Flip();
                newBuffer.Put(m_buffer);
                m_buffer = newBuffer;
            }

            int flags = 0;
            if (!("*".Equals(baseForm, StringComparison.Ordinal) || baseForm.Equals(entry[0], StringComparison.Ordinal)))
            {
                flags |= BinaryDictionary.HAS_BASEFORM;
            }
            if (!reading.Equals(ToKatakana(entry[0]), StringComparison.Ordinal))
            {
                flags |= BinaryDictionary.HAS_READING;
            }
            if (!pronunciation.Equals(reading, StringComparison.Ordinal))
            {
                flags |= BinaryDictionary.HAS_PRONUNCIATION;
            }

            Debug.Assert(leftId == rightId);
            Debug.Assert(leftId < 4096); // there are still unused bits
                                         // add pos mapping
            int toFill = 1 + leftId - posDict.Count;
            for (int i = 0; i < toFill; i++)
            {
                posDict.Add(null);
            }

            string existing = posDict[leftId];
            Debug.Assert(existing == null || existing.Equals(fullPOSData, StringComparison.Ordinal));
            posDict[leftId] = fullPOSData;

            m_buffer.PutInt16((short)(leftId << 3 | flags));
            m_buffer.PutInt16(wordCost);

            if ((flags & BinaryDictionary.HAS_BASEFORM) != 0)
            {
                Debug.Assert(baseForm.Length < 16);
                int shared = SharedPrefix(entry[0], baseForm);
                int suffix = baseForm.Length - shared;
                m_buffer.Put((byte)(shared << 4 | suffix));
                for (int i = shared; i < baseForm.Length; i++)
                {
                    m_buffer.PutChar(baseForm[i]);
                }
            }

            if ((flags & BinaryDictionary.HAS_READING) != 0)
            {
                if (IsKatakana(reading))
                {
                    m_buffer.Put((byte)(reading.Length << 1 | 1));
                    WriteKatakana(reading);
                }
                else
                {
                    m_buffer.Put((byte)(reading.Length << 1));
                    for (int i = 0; i < reading.Length; i++)
                    {
                        m_buffer.PutChar(reading[i]);
                    }
                }
            }

            if ((flags & BinaryDictionary.HAS_PRONUNCIATION) != 0)
            {
                // we can save 150KB here, but it makes the reader a little complicated.
                // int shared = sharedPrefix(reading, pronunciation);
                // buffer.put((byte) shared);
                // pronunciation = pronunciation.substring(shared);
                if (IsKatakana(pronunciation))
                {
                    m_buffer.Put((byte)(pronunciation.Length << 1 | 1));
                    WriteKatakana(pronunciation);
                }
                else
                {
                    m_buffer.Put((byte)(pronunciation.Length << 1));
                    for (int i = 0; i < pronunciation.Length; i++)
                    {
                        m_buffer.PutChar(pronunciation[i]);
                    }
                }
            }

            return m_buffer.Position;
        }

        private bool IsKatakana(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
                if (ch < 0x30A0 || ch > 0x30FF)
                {
                    return false;
                }
            }
            return true;
        }

        private void WriteKatakana(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                m_buffer.Put((byte)(s[i] - 0x30A0));
            }
        }

        private string ToKatakana(string s)
        {
            char[] text = new char[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                char ch = s[i];
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

        public static int SharedPrefix(string left, string right)
        {
            int len = left.Length < right.Length ? left.Length : right.Length;
            for (int i = 0; i < len; i++)
                if (left[i] != right[i])
                    return i;
            return len;
        }

        public virtual void AddMapping(int sourceId, int wordId)
        {
            Debug.Assert(wordId > lastWordId, "words out of order: " + wordId + " vs lastID: " + lastWordId);

            if (sourceId > lastSourceId)
            {
                Debug.Assert(sourceId > lastSourceId, "source ids out of order: lastSourceId=" + lastSourceId + " vs sourceId=" + sourceId);
                targetMapOffsets = ArrayUtil.Grow(targetMapOffsets, sourceId + 1);
                for (int i = lastSourceId + 1; i <= sourceId; i++)
                {
                    targetMapOffsets[i] = targetMapEndOffset;
                }
            }
            else
            {
                Debug.Assert(sourceId == lastSourceId);
            }

            targetMap = ArrayUtil.Grow(targetMap, targetMapEndOffset + 1);
            targetMap[targetMapEndOffset] = wordId;
            targetMapEndOffset++;

            lastSourceId = sourceId;
            lastWordId = wordId;
        }

        protected string GetBaseFileName(string baseDir)
        {
            // LUCENENET specific: we don't need to do a "classpath" output directory, since we
            // are changing the implementation to read files dynamically instead of making the
            // user recompile with the new files.
            return System.IO.Path.Combine(baseDir, m_implClazz.Name);

            //return baseDir + System.IO.Path.DirectorySeparatorChar + m_implClazz.FullName.Replace('.', System.IO.Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Write dictionary in file
        /// </summary>
        /// <remarks>
        /// Dictionary format is:
        /// [Size of dictionary(int)], [entry:{left id(short)}{right id(short)}{word cost(short)}{length of pos info(short)}{pos info(char)}], [entry...], [entry...].....
        /// </remarks>
        /// <param name="baseDir"></param>
        /// <exception cref="IOException">If an I/O error occurs writing the dictionary files.</exception>
        public virtual void Write(string baseDir)
        {
            string baseName = GetBaseFileName(baseDir);
            WriteDictionary(baseName + BinaryDictionary.DICT_FILENAME_SUFFIX);
            WriteTargetMap(baseName + BinaryDictionary.TARGETMAP_FILENAME_SUFFIX);
            WritePosDict(baseName + BinaryDictionary.POSDICT_FILENAME_SUFFIX);
        }

        // TODO: maybe this int[] should instead be the output to the FST...
        protected virtual void WriteTargetMap(string filename)
        {
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            using (Stream os = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                DataOutput @out = new OutputStreamDataOutput(os);
                CodecUtil.WriteHeader(@out, BinaryDictionary.TARGETMAP_HEADER, BinaryDictionary.VERSION);

                int numSourceIds = lastSourceId + 1;
                @out.WriteVInt32(targetMapEndOffset); // <-- size of main array
                @out.WriteVInt32(numSourceIds + 1); // <-- size of offset array (+ 1 more entry)
                int prev = 0, sourceId = 0;
                for (int ofs = 0; ofs < targetMapEndOffset; ofs++)
                {
                    int val = targetMap[ofs], delta = val - prev;
                    Debug.Assert(delta >= 0);
                    if (ofs == targetMapOffsets[sourceId])
                    {
                        @out.WriteVInt32((delta << 1) | 0x01);
                        sourceId++;
                    }
                    else
                    {
                        @out.WriteVInt32((delta << 1));
                    }
                    prev += delta;
                }
                Debug.Assert(sourceId == numSourceIds, "sourceId:" + sourceId + " != numSourceIds:" + numSourceIds);
            }
        }

        protected virtual void WritePosDict(string filename)
        {
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            using (Stream os = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                DataOutput @out = new OutputStreamDataOutput(os);
                CodecUtil.WriteHeader(@out, BinaryDictionary.POSDICT_HEADER, BinaryDictionary.VERSION);
                @out.WriteVInt32(posDict.Count);
                foreach (string s in posDict)
                {
                    if (s == null)
                    {
                        @out.WriteByte((byte)0);
                        @out.WriteByte((byte)0);
                        @out.WriteByte((byte)0);
                    }
                    else
                    {
                        string[] data = CSVUtil.Parse(s);
                        Debug.Assert(data.Length == 3, "malformed pos/inflection: " + s);
                        @out.WriteString(data[0]);
                        @out.WriteString(data[1]);
                        @out.WriteString(data[2]);
                    }
                }
            }
        }

        protected virtual void WriteDictionary(string filename)
        {
            //new File(filename).getParentFile().mkdirs();
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filename));
            using (Stream os = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                DataOutput @out = new OutputStreamDataOutput(os);
                CodecUtil.WriteHeader(@out, BinaryDictionary.DICT_HEADER, BinaryDictionary.VERSION);
                @out.WriteVInt32(m_buffer.Position);

                //WritableByteChannel channel = Channels.newChannel(os);
                // Write Buffer
                m_buffer.Flip();  // set position to 0, set limit to current position
                //channel.write(buffer);

                while (m_buffer.HasRemaining)
                {
                    @out.WriteByte(m_buffer.Get());
                }

                Debug.Assert(m_buffer.Remaining == 0L);
            }
        }
    }
}
