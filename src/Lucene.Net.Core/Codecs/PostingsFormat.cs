using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs
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

    using SegmentReadState = Lucene.Net.Index.SegmentReadState;

    // javadocs
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Encodes/decodes terms, postings, and proximity data.
    /// <p>
    /// Note, when extending this class, the name (<seealso cref="#getName"/>) may
    /// written into the index in certain configurations. In order for the segment
    /// to be read, the name must resolve to your implementation via <seealso cref="#forName(String)"/>.
    /// this method uses Java's
    /// <seealso cref="ServiceLoader Service Provider Interface"/> (SPI) to resolve format names.
    /// <p>
    /// If you implement your own format, make sure that it has a no-arg constructor
    /// so SPI can load it. </summary>
    /// <seealso cref= ServiceLoader
    /// @lucene.experimental  </seealso>
    public abstract class PostingsFormat : NamedSPILoader<PostingsFormat>.NamedSPI
    {
        private static readonly NamedSPILoader<PostingsFormat> Loader = new NamedSPILoader<PostingsFormat>(typeof(PostingsFormat));

        /// <summary>
        /// Zero-length {@code PostingsFormat} array. </summary>
        public static readonly PostingsFormat[] EMPTY = new PostingsFormat[0];

        /// <summary>
        /// Unique name that's used to retrieve this format when
        ///  reading the index.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Creates a new postings format.
        /// <p>
        /// The provided name will be written into the index segment in some configurations
        /// (such as when using <seealso cref="PerFieldPostingsFormat"/>): in such configurations,
        /// for the segment to be read this class should be registered with Java's
        /// SPI mechanism (registered in META-INF/ of your jar file, etc). </summary>
        /// <param name="name"> must be all ascii alphanumeric, and less than 128 characters in length. </param>
        protected PostingsFormat(string name)
        {
            NamedSPILoader<PostingsFormat>.CheckServiceName(name);
            this.name = name;
        }

        /// <summary>
        /// Returns this posting format's name </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Writes a new segment </summary>
        public abstract FieldsConsumer FieldsConsumer(SegmentWriteState state);

        /// <summary>
        /// Reads a segment.  NOTE: by the time this call
        ///  returns, it must hold open any files it will need to
        ///  use; else, those files may be deleted.
        ///  Additionally, required files may be deleted during the execution of
        ///  this call before there is a chance to open them. Under these
        ///  circumstances an IOException should be thrown by the implementation.
        ///  IOExceptions are expected and will automatically cause a retry of the
        ///  segment opening logic with the newly revised segments.
        ///
        /// </summary>
        public abstract FieldsProducer FieldsProducer(SegmentReadState state);

        public override string ToString()
        {
            return "PostingsFormat(name=" + name + ")";
        }

        /// <summary>
        /// looks up a format by name </summary>
        public static PostingsFormat ForName(string name)
        {
            if (Loader == null)
            {
                throw new InvalidOperationException("You called PostingsFormat.forName() before all formats could be initialized. " + "this likely happens if you call it from a PostingsFormat's ctor.");
            }
            return Loader.Lookup(name);
        }

        /// <summary>
        /// returns a list of all available format names </summary>
        public static ISet<string> AvailablePostingsFormats()
        {
            if (Loader == null)
            {
                throw new InvalidOperationException("You called PostingsFormat.availablePostingsFormats() before all formats could be initialized. " + "this likely happens if you call it from a PostingsFormat's ctor.");
            }
            return Loader.AvailableServices();
        }

        /// <summary>
        /// Reloads the postings format list from the given <seealso cref="ClassLoader"/>.
        /// Changes to the postings formats are visible after the method ends, all
        /// iterators (<seealso cref="#availablePostingsFormats()"/>,...) stay consistent.
        ///
        /// <p><b>NOTE:</b> Only new postings formats are added, existing ones are
        /// never removed or replaced.
        ///
        /// <p><em>this method is expensive and should only be called for discovery
        /// of new postings formats on the given classpath/classloader!</em>
        /// </summary>
        public static void ReloadPostingsFormats()
        {
            Loader.Reload();
        }
    }
}