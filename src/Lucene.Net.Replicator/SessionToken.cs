using J2N.IO;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Replicator
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
    /// Token for a replication session, for guaranteeing that source replicated
    /// files will be kept safe until the replication completes.
    /// </summary>
    /// <remarks>
    /// @lucene.experimental
    /// </remarks>
    /// <seealso cref="IReplicator.CheckForUpdate"/>
    /// <seealso cref="IReplicator.Release"/>
    /// <seealso cref="LocalReplicator.DEFAULT_SESSION_EXPIRATION_THRESHOLD"/>
    public sealed class SessionToken
    {
        /// <summary>
        /// Id of this session.
        /// Should be passed when releasing the session, thereby acknowledging the 
        /// <see cref="IReplicator"/> that this session is no longer in use.
        /// </summary>
        /// <seealso cref="IReplicator.Release"/>
        public string Id { get; private set; }

        /// <summary>
        /// <see cref="IRevision.Version"/>
        /// </summary>
        public string Version { get; private set; }

        /// <summary>
        /// <see cref="IRevision.SourceFiles"/>
        /// </summary>
        public IDictionary<string, IList<RevisionFile>> SourceFiles { get; private set; }

        /// <summary>
        /// Constructor which deserializes from the given <see cref="IDataInput"/>.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public SessionToken(IDataInput reader)
        {
            Id = reader.ReadUTF();
            Version = reader.ReadUTF();

            Dictionary<string, IList<RevisionFile>> sourceFiles = new Dictionary<string, IList<RevisionFile>>();
            int numSources = reader.ReadInt32();
            while (numSources > 0)
            {
                string source = reader.ReadUTF();
                int numFiles = reader.ReadInt32();

                IList<RevisionFile> files = new JCG.List<RevisionFile>(numFiles);
                for (int i = 0; i < numFiles; i++)
                {
                    files.Add(new RevisionFile(reader.ReadUTF(), reader.ReadInt64()));
                }
                sourceFiles.Add(source, files);
                --numSources;
            }
            SourceFiles = sourceFiles;
        }

        /// <summary>
        /// Constructor with the given <paramref name="id"/> and <paramref name="revision"/>.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public SessionToken(string id, IRevision revision)
        {
            Id = id;
            Version = revision.Version;
            SourceFiles = revision.SourceFiles;
        }

        /// <summary>
        /// Serialize the token data for communication between server and client.
        /// </summary>
        /// <exception cref="IOException"></exception>
        public void Serialize(DataOutputStream writer)
        {
            writer.WriteUTF(Id);
            writer.WriteUTF(Version);
            writer.WriteInt32(SourceFiles.Count);

            foreach (KeyValuePair<string, IList<RevisionFile>> pair in SourceFiles)
            {
                writer.WriteUTF(pair.Key);
                writer.WriteInt32(pair.Value.Count);
                foreach (RevisionFile file in pair.Value)
                {
                    writer.WriteUTF(file.FileName);
                    writer.WriteInt64(file.Length);
                }
            }
        }

        public override string ToString()
        {
            return string.Format("id={0} version={1} files={2}", Id, Version, SourceFiles);
        }
    }
}