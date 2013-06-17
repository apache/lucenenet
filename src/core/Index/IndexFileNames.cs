/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Lucene.Net.Support;
using System.Text;
using System.Text.RegularExpressions;

namespace Lucene.Net.Index
{

    /// <summary>Useful constants representing filenames and extensions used by lucene</summary>
    public static class IndexFileNames
    {

        /// <summary>Name of the index segment file </summary>
        public const String SEGMENTS = "segments";

        /// <summary>Extension of gen file </summary>
        public const String GEN_EXTENSION = "gen";

        /// <summary>Name of the generation reference file name </summary>
        public const String SEGMENTS_GEN = "segments." + GEN_EXTENSION;

        /// <summary>Extension of compound file </summary>
        public const String COMPOUND_FILE_EXTENSION = "cfs";

        /// <summary>
        /// Extension of compound file entries.
        /// </summary>
        public const String COMPOUND_FILE_ENTRIES_EXTENSION = "cfe";

        /// <summary> This array contains all filename extensions used by
        /// Lucene's index files, with two exceptions, namely the
        /// extension made up from <c>.f</c> + a number and
        /// from <c>.s</c> + a number.  Also note that
        /// Lucene's <c>segments_N</c> files do not have any
        /// filename extension.
        /// </summary>
        public static readonly String[] INDEX_EXTENSIONS = new String[] { 
            COMPOUND_FILE_EXTENSION, 
            COMPOUND_FILE_ENTRIES_EXTENSION,
            GEN_EXTENSION
        };

        /// <summary> Computes the full file name from base, extension and
        /// generation.  If the generation is -1, the file name is
        /// null.  If it's 0, the file name is 
        /// If it's > 0, the file name is 
        /// 
        /// </summary>
        /// <param name="basepart">-- main part of the file name
        /// </param>
        /// <param name="extension">-- extension of the filename (including .)
        /// </param>
        /// <param name="gen">-- generation
        /// </param>
        public static String FileNameFromGeneration(String basepart, String ext, long gen)
        {
            if (gen == -1)
            {
                return null;
            }
            else if (gen == 0)
            {
                return SegmentFileName(basepart, "", ext);
            }
            else
            {
                //assert gen > 0;
                // The '6' part in the length is: 1 for '.', 1 for '_' and 4 as estimate
                // to the gen length as string (hopefully an upper limit so SB won't
                // expand in the middle.
                StringBuilder res = new StringBuilder(basepart.Length + 6 + ext.Length)
                    .Append(basepart).Append('_').Append(Number.ToString(gen));
                if (ext.Length > 0)
                {
                    res.Append('.').Append(ext);
                }
                return res.ToString();
            }
        }

        public static String SegmentFileName(String segmentName, String segmentSuffix, String ext)
        {
            if (ext.Length > 0 || segmentSuffix.Length > 0)
            {
                //assert !ext.startsWith(".");
                StringBuilder sb = new StringBuilder(segmentName.Length + 2 + segmentSuffix.Length + ext.Length);
                sb.Append(segmentName);
                if (segmentSuffix.Length > 0)
                {
                    sb.Append('_').Append(segmentSuffix);
                }
                if (ext.Length > 0)
                {
                    sb.Append('.').Append(ext);
                }
                return sb.ToString();
            }
            else
            {
                return segmentName;
            }
        }

        public static bool MatchesExtension(string filename, string ext)
        {
            // It doesn't make a difference whether we allocate a StringBuilder ourself
            // or not, since there's only 1 '+' operator.
            return filename.EndsWith("." + ext);
        }

        private static int IndexOfSegmentName(string filename)
        {
            // If it is a .del file, there's an '_' after the first character
            int idx = filename.IndexOf('_', 1);
            if (idx == -1)
            {
                // If it's not, strip everything that's before the '.'
                idx = filename.IndexOf('.');
            }
            return idx;
        }

        public static String StripSegmentName(String filename)
        {
            int idx = IndexOfSegmentName(filename);
            if (idx != -1)
            {
                filename = filename.Substring(idx);
            }
            return filename;
        }

        public static String ParseSegmentName(String filename)
        {
            int idx = IndexOfSegmentName(filename);
            if (idx != -1)
            {
                filename = filename.Substring(0, idx);
            }
            return filename;
        }

        public static String StripExtension(String filename)
        {
            int idx = filename.IndexOf('.');
            if (idx != -1)
            {
                filename = filename.Substring(0, idx);
            }
            return filename;
        } 

        // All files created by codecs much match this pattern (we
        // check this in SegmentInfo.java):
        internal static readonly Regex CODEC_FILE_PATTERN = new Regex("_[a-z0-9]+(_.*)?\\..*", RegexOptions.Compiled);
    }
}