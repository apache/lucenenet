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
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Lucene.Net.Codecs;
using Lucene.Net.Support;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
using IndexOutput = Lucene.Net.Store.IndexOutput;

namespace Lucene.Net.Index
{

    /// <summary> Information about a segment such as it's name, directory, and files related
    /// to the segment.
    /// 
    /// * <p/><b>NOTE:</b> This API is new and still experimental
    /// (subject to change suddenly in the next release)<p/>
    /// </summary>
    public sealed class SegmentInfo : ICloneable
    {

        public const int NO = -1;            // e.g. no norms; no deletes;
        public const int YES = 1;             // e.g. have norms; have deletes;

        public readonly String name;              // unique name in dir
        private int docCount;                    // number of docs in seg
        public readonly Directory dir;                   // where segment resides

        private bool isCompoundFile;           // NO if it is not; YES if it is; CHECK_DIR if it's
        // pre-2.1 (ie, must check file system to see
        // if <name>.cfs and <name>.nrm exist)         

        private Codec codec;

        private IDictionary<string, string> diagnostics;

        private IDictionary<string, string> attributes;

        // Tracks the Lucene version this segment was created with, since 3.1. Null
        // indicates an older than 3.0 index, and it's used to detect a too old index.
        // The format expected is "x.y" - "2.x" for pre-3.0 indexes (or null), and
        // specific versions afterwards ("3.0", "3.1" etc.).
        // see Constants.LUCENE_MAIN_VERSION.
        private String version;

        public IDictionary<string, string> Diagnostics
        {
            get { return diagnostics; }
            set { diagnostics = value; }
        }

        public SegmentInfo(Directory dir, String version, String name, int docCount,
                     bool isCompoundFile, Codec codec, IDictionary<String, String> diagnostics, IDictionary<String, String> attributes)
        {
            //assert !(dir instanceof TrackingDirectoryWrapper);
            this.dir = dir;
            this.version = version;
            this.name = name;
            this.docCount = docCount;
            this.isCompoundFile = isCompoundFile;
            this.codec = codec;
            this.diagnostics = diagnostics;
            this.attributes = attributes;
        }

        [Obsolete]
        internal bool HasSeparateNorms
        {
            get { return GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_KEY) != null; }
        }

        public bool UseCompoundFile
        {
            get { return isCompoundFile; }
            set { isCompoundFile = value; }
        }

        public Codec Codec
        {
            get { return codec; }
            set
            {
                //assert this.codec == null;
                if (value == null)
                {
                    throw new ArgumentException("segmentCodecs must be non-null");
                }
                this.codec = value;
            }
        }

        public int DocCount
        {
            get
            {
                if (this.docCount == -1)
                {
                    throw new InvalidOperationException("docCount isn't set yet");
                }
                return docCount;
            }
            set
            {
                if (this.docCount != -1)
                {
                    throw new InvalidOperationException("docCount was already set");
                }
                this.docCount = value;
            }
        }

        public ISet<string> Files
        {
            get
            {
                if (setFiles == null)
                {
                    throw new InvalidOperationException("files were not computed yet");
                }

                return setFiles;
            }
            set
            {
                CheckFileNames(value);
                setFiles = value;
            }
        }

        public override string ToString()
        {
            return ToString(dir, 0);
        }

        public String ToString(Directory dir, int delCount)
        {
            StringBuilder s = new StringBuilder();
            s.Append(name).Append('(').Append(version == null ? "?" : version).Append(')').Append(':');
            char cfs = UseCompoundFile ? 'c' : 'C';
            s.Append(cfs);

            if (this.dir != dir)
            {
                s.Append('x');
            }
            s.Append(docCount);

            if (delCount != 0)
            {
                s.Append('/').Append(delCount);
            }

            // TODO: we could append toString of attributes() here?

            return s.ToString();
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj is SegmentInfo)
            {
                SegmentInfo other = (SegmentInfo)obj;
                return other.dir == dir && other.name.Equals(name);
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return dir.GetHashCode() + name.GetHashCode();
        }

        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        private ISet<String> setFiles;

        public void AddFiles(ICollection<String> files)
        {
            CheckFileNames(files);
            setFiles.UnionWith(files);
        }

        public void AddFile(String file)
        {
            CheckFileNames(new[] { file });
            setFiles.Add(file);
        }

        private void CheckFileNames(ICollection<String> files)
        {
            Regex r = IndexFileNames.CODEC_FILE_PATTERN;
            foreach (String file in files)
            {
                if (!r.IsMatch(file))
                {
                    throw new ArgumentException("invalid codec filename '" + file + "', must match: " + IndexFileNames.CODEC_FILE_PATTERN.ToString());
                }
            }
        }

        public string GetAttribute(string key)
        {
            if (attributes == null)
            {
                return null;
            }
            else
            {
                return attributes[key];
            }
        }

        public String PutAttribute(String key, String value)
        {
            if (attributes == null)
            {
                attributes = new HashMap<String, String>();
            }
            return attributes[key] = value;
        }

        public IDictionary<String, String> Attributes
        {
            get { return attributes; }
        }
    }
}