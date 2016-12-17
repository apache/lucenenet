namespace Lucene.Net.Codecs
{
    using Lucene.Net.Util;
    using System;
    using System.Collections.Generic;

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

    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig; // javadocs

    /// <summary>
    /// Encodes/decodes an inverted index segment.
    /// <p>
    /// Note, when extending this class, the name (<seealso cref="#getName"/>) is
    /// written into the index. In order for the segment to be read, the
    /// name must resolve to your implementation via <seealso cref="#forName(String)"/>.
    /// this method uses Java's
    /// <seealso cref="ServiceLoader Service Provider Interface"/> (SPI) to resolve codec names.
    /// <p>
    /// If you implement your own codec, make sure that it has a no-arg constructor
    /// so SPI can load it. </summary>
    /// <seealso cref= ServiceLoader </seealso>
    public abstract class Codec : NamedSPILoader<Codec>.NamedSPI
    {
        private static readonly NamedSPILoader<Codec> Loader;

        private readonly string name;

        static Codec()
        {
            Loader = new NamedSPILoader<Codec>(typeof(Codec));
            DefaultCodec = Codec.ForName("Lucene46");
        }

        /// <summary>
        /// Creates a new codec.
        /// <p>
        /// The provided name will be written into the index segment: in order to
        /// for the segment to be read this class should be registered with Java's
        /// SPI mechanism (registered in META-INF/ of your jar file, etc). </summary>
        /// <param name="name"> must be all ascii alphanumeric, and less than 128 characters in length. </param>
        protected internal Codec(string name)
        {
            NamedSPILoader<Codec>.CheckServiceName(name);
            this.name = name;
        }

        /// <summary>
        /// Returns this codec's name </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        /// <summary>
        /// Encodes/decodes postings </summary>
        public abstract PostingsFormat PostingsFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// Encodes/decodes docvalues </summary>
        public abstract DocValuesFormat DocValuesFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// Encodes/decodes stored fields </summary>
        public abstract StoredFieldsFormat StoredFieldsFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// Encodes/decodes term vectors </summary>
        public abstract TermVectorsFormat TermVectorsFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// Encodes/decodes field infos file </summary>
        public abstract FieldInfosFormat FieldInfosFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// Encodes/decodes segment info file </summary>
        public abstract SegmentInfoFormat SegmentInfoFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// Encodes/decodes document normalization values </summary>
        public abstract NormsFormat NormsFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// Encodes/decodes live docs </summary>
        public abstract LiveDocsFormat LiveDocsFormat(); // LUCENENET TODO: make property

        /// <summary>
        /// looks up a codec by name </summary>
        public static Codec ForName(string name)
        {
            if (Loader == null)
            {
                throw new InvalidOperationException("You called Codec.forName() before all Codecs could be initialized. " + "this likely happens if you call it from a Codec's ctor.");
            }
            return Loader.Lookup(name);
        }

        /// <summary>
        /// returns a list of all available codec names </summary>
        public static ISet<string> AvailableCodecs()
        {
            if (Loader == null)
            {
                throw new InvalidOperationException("You called Codec.AvailableCodecs() before all Codecs could be initialized. " + 
                    "this likely happens if you call it from a Codec's ctor.");
            }
            return Loader.AvailableServices();
        }

        /// <summary>
        /// Reloads the codec list from the given <seealso cref="ClassLoader"/>.
        /// Changes to the codecs are visible after the method ends, all
        /// iterators (<seealso cref="#availableCodecs()"/>,...) stay consistent.
        ///
        /// <p><b>NOTE:</b> Only new codecs are added, existing ones are
        /// never removed or replaced.
        ///
        /// <p><em>this method is expensive and should only be called for discovery
        /// of new codecs on the given classpath/classloader!</em>
        /// </summary>
        public static void ReloadCodecs()
        {
            Loader.Reload();
        }

        private static Codec DefaultCodec;

        /// <summary>
        /// expert: returns the default codec used for newly created
        ///  <seealso cref="IndexWriterConfig"/>s.
        /// </summary>
        // TODO: should we use this, or maybe a system property is better?
        public static Codec Default
        {
            get
            {
                return DefaultCodec;
            }
            set
            {
                DefaultCodec = value;
            }
        }

        /// <summary>
        /// returns the codec's name. Subclasses can override to provide
        /// more detail (such as parameters).
        /// </summary>
        public override string ToString()
        {
            return name;
        }
    }
}