using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Index
{
    using Support;
    using System.Text.RegularExpressions;

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

    // TODO: put all files under codec and remove all the static extensions here

    /// <summary>
    /// this class contains useful constants representing filenames and extensions
    /// used by lucene, as well as convenience methods for querying whether a file
    /// name matches an extension ({@link #matchesExtension(String, String)
    /// matchesExtension}), as well as generating file names from a segment name,
    /// generation and extension (
    /// <seealso cref="#fileNameFromGeneration(String, String, long) fileNameFromGeneration"/>,
    /// <seealso cref="#segmentFileName(String, String, String) segmentFileName"/>).
    ///
    /// <p><b>NOTE</b>: extensions used by codecs are not
    /// listed here.  You must interact with the <seealso cref="Codec"/>
    /// directly.
    ///
    /// @lucene.internal
    /// </summary>

    public sealed class IndexFileNames
    {
        /// <summary>
        /// No instance </summary>
        private IndexFileNames()
        {
        }

        /// <summary>
        /// Name of the index segment file </summary>
        public const string SEGMENTS = "segments";

        /// <summary>
        /// Extension of gen file </summary>
        public const string GEN_EXTENSION = "gen";

        /// <summary>
        /// Name of the generation reference file name </summary>
        public static readonly string SEGMENTS_GEN = "segments." + GEN_EXTENSION;

        /// <summary>
        /// Extension of compound file </summary>
        public const string COMPOUND_FILE_EXTENSION = "cfs";

        /// <summary>
        /// Extension of compound file entries </summary>
        public const string COMPOUND_FILE_ENTRIES_EXTENSION = "cfe";

        /// <summary>
        /// this array contains all filename extensions used by
        /// Lucene's index files, with one exception, namely the
        /// extension made up from  <code>.s</code> + a number.
        /// Also note that Lucene's <code>segments_N</code> files
        /// do not have any filename extension.
        /// </summary>
        public static readonly string[] INDEX_EXTENSIONS = new string[] { COMPOUND_FILE_EXTENSION, COMPOUND_FILE_ENTRIES_EXTENSION, GEN_EXTENSION };

        /// <summary>
        /// Computes the full file name from base, extension and generation. If the
        /// generation is -1, the file name is null. If it's 0, the file name is
        /// &lt;base&gt;.&lt;ext&gt;. If it's > 0, the file name is
        /// &lt;base&gt;_&lt;gen&gt;.&lt;ext&gt;.<br>
        /// <b>NOTE:</b> .&lt;ext&gt; is added to the name only if <code>ext</code> is
        /// not an empty string.
        /// </summary>
        /// <param name="base"> main part of the file name </param>
        /// <param name="ext"> extension of the filename </param>
        /// <param name="gen"> generation </param>
        public static string FileNameFromGeneration(string @base, string ext, long gen)
        {
            if (gen == -1)
            {
                return null;
            }
            else if (gen == 0)
            {
                return SegmentFileName(@base, "", ext);
            }
            else
            {
                Debug.Assert(gen > 0);
                // The '6' part in the length is: 1 for '.', 1 for '_' and 4 as estimate
                // to the gen length as string (hopefully an upper limit so SB won't
                // expand in the middle.
                StringBuilder res = (new StringBuilder(@base.Length + 6 + ext.Length))
                    .Append(@base).Append('_').Append(Number.ToString(gen, Character.MAX_RADIX));
                if (ext.Length > 0)
                {
                    res.Append('.').Append(ext);
                }
                return res.ToString();
            }
        }

        /// <summary>
        /// Returns a file name that includes the given segment name, your own custom
        /// name and extension. The format of the filename is:
        /// &lt;segmentName&gt;(_&lt;name&gt;)(.&lt;ext&gt;).
        /// <p>
        /// <b>NOTE:</b> .&lt;ext&gt; is added to the result file name only if
        /// <code>ext</code> is not empty.
        /// <p>
        /// <b>NOTE:</b> _&lt;segmentSuffix&gt; is added to the result file name only if
        /// it's not the empty string
        /// <p>
        /// <b>NOTE:</b> all custom files should be named using this method, or
        /// otherwise some structures may fail to handle them properly (such as if they
        /// are added to compound files).
        /// </summary>
        public static string SegmentFileName(string segmentName, string segmentSuffix, string ext)
        {
            if (ext.Length > 0 || segmentSuffix.Length > 0)
            {
                Debug.Assert(!ext.StartsWith("."));
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

        /// <summary>
        /// Returns true if the given filename ends with the given extension. One
        /// should provide a <i>pure</i> extension, without '.'.
        /// </summary>
        public static bool MatchesExtension(string filename, string ext)
        {
            // It doesn't make a difference whether we allocate a StringBuilder ourself
            // or not, since there's only 1 '+' operator.
            return filename.EndsWith("." + ext);
        }

        /// <summary>
        /// locates the boundary of the segment name, or -1 </summary>
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

        /// <summary>
        /// Strips the segment name out of the given file name. If you used
        /// <seealso cref="#segmentFileName"/> or <seealso cref="#fileNameFromGeneration"/> to create your
        /// files, then this method simply removes whatever comes before the first '.',
        /// or the second '_' (excluding both).
        /// </summary>
        /// <returns> the filename with the segment name removed, or the given filename
        ///         if it does not contain a '.' and '_'. </returns>
        public static string StripSegmentName(string filename)
        {
            int idx = IndexOfSegmentName(filename);
            if (idx != -1)
            {
                filename = filename.Substring(idx);
            }
            return filename;
        }

        /// <summary>
        /// Parses the segment name out of the given file name.
        /// </summary>
        /// <returns> the segment name only, or filename
        ///         if it does not contain a '.' and '_'. </returns>
        public static string ParseSegmentName(string filename)
        {
            int idx = IndexOfSegmentName(filename);
            if (idx != -1)
            {
                filename = filename.Substring(0, idx);
            }
            return filename;
        }

        /// <summary>
        /// Removes the extension (anything after the first '.'),
        /// otherwise returns the original filename.
        /// </summary>
        public static string StripExtension(string filename)
        {
            int idx = filename.IndexOf('.');
            if (idx != -1)
            {
                filename = filename.Substring(0, idx);
            }
            return filename;
        }

        /// <summary>
        /// Return the extension (anything after the first '.'),
        /// or null if there is no '.' in the file name.
        /// </summary>
        public static string GetExtension(string filename)
        {
            int idx = filename.IndexOf('.');
            if (idx == -1)
            {
                return null;
            }
            else
            {
                return filename.Substring(idx + 1, filename.Length - (idx + 1));
            }
        }

        /// <summary>
        /// All files created by codecs much match this pattern (checked in
        /// SegmentInfo).
        /// </summary>
        public static readonly Regex CODEC_FILE_PATTERN = new Regex("_[a-z0-9]+(_.*)?\\..*", RegexOptions.Compiled);
    }
}