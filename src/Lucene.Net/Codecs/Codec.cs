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

    /// <summary>
    /// Encodes/decodes an inverted index segment.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="Name"/>) is
    /// written into the index. In order for the segment to be read, the
    /// name must resolve to your implementation via <see cref="ForName(string)"/>.
    /// This method uses <see cref="ICodecFactory.GetCodec(string)"/> to resolve codec names.
    /// <para/>
    /// To implement your own codec:
    /// <list type="number">
    ///     <item><description>Subclass this class.</description></item>
    ///     <item><description>Subclass <see cref="DefaultCodecFactory"/>, override the <see cref="DefaultCodecFactory.Initialize()"/> method,
    ///         and add the line <c>base.ScanForCodecs(typeof(YourCodec).Assembly)</c>. 
    ///         If you have any codec classes in your assembly 
    ///         that are not meant for reading, you can add the <see cref="ExcludeCodecFromScanAttribute"/> 
    ///         to them so they are ignored by the scan.</description></item>
    ///     <item><description>set the new <see cref="ICodecFactory"/> by calling <see cref="SetCodecFactory"/> at application startup.</description></item>
    /// </list>
    /// If your codec has dependencies, you may also override <see cref="DefaultCodecFactory.GetCodec(Type)"/> to inject 
    /// them via pure DI or a DI container. See <a href="http://blog.ploeh.dk/2014/05/19/di-friendly-framework/">DI-Friendly Framework</a>
    /// to understand the approach used.
    /// <para/>
    /// <b>Codec Names</b>
    /// <para/>
    /// Unlike the Java version, codec names are by default convention-based on the class name. 
    /// If you name your custom codec class "MyCustomCodec", the codec name will the same name 
    /// without the "Codec" suffix: "MyCustom".
    /// <para/>
    /// You can override this default behavior by using the <see cref="CodecNameAttribute"/> to
    /// name the codec differently than this convention. Codec names must be all ASCII alphanumeric, 
    /// and less than 128 characters in length.
    /// </summary>
    /// <seealso cref="DefaultCodecFactory"/>
    /// <seealso cref="ICodecFactory"/>
    /// <seealso cref="CodecNameAttribute"/>
    // LUCENENET specific - refactored this class so it depends on ICodecFactory rather than
    // the Java-centric NamedSPILoader
    public abstract class Codec //: NamedSPILoader.INamedSPI
    {
        private static ICodecFactory codecFactory = new DefaultCodecFactory();
        private readonly string name;

        /// <summary>
        /// Sets the <see cref="ICodecFactory"/> instance used to instantiate
        /// <see cref="Codec"/> subclasses.
        /// </summary>
        /// <param name="codecFactory">The new <see cref="ICodecFactory"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="codecFactory"/> parameter is <c>null</c>.</exception>
        public static void SetCodecFactory(ICodecFactory codecFactory)
        {
            Codec.codecFactory = codecFactory ?? throw new ArgumentNullException(nameof(codecFactory));
        }

        /// <summary>
        /// Gets the associated codec factory.
        /// </summary>
        /// <returns>The codec factory.</returns>
        public static ICodecFactory GetCodecFactory()
        {
            return codecFactory;
        }

        /// <summary>
        /// Creates a new codec.
        /// <para/>
        /// The <see cref="Codec.Name"/> will be written into the index segment: in order for
        /// the segment to be read this class should be registered by subclassing <see cref="DefaultCodecFactory"/> and
        /// calling <see cref="DefaultCodecFactory.ScanForCodecs(System.Reflection.Assembly)"/> in the class constructor. 
        /// The new <see cref="ICodecFactory"/> can be registered by calling <see cref="SetCodecFactory"/> at application startup.</summary>
        protected Codec()
        {
            name = NamedServiceFactory<Codec>.GetServiceName(this.GetType());
        }

        /// <summary>
        /// Returns this codec's name. </summary>
        public string Name => name;

        /// <summary>
        /// Encodes/decodes postings. </summary>
        public abstract PostingsFormat PostingsFormat { get; }

        /// <summary>
        /// Encodes/decodes docvalues. </summary>
        public abstract DocValuesFormat DocValuesFormat { get; }

        /// <summary>
        /// Encodes/decodes stored fields. </summary>
        public abstract StoredFieldsFormat StoredFieldsFormat { get; }

        /// <summary>
        /// Encodes/decodes term vectors. </summary>
        public abstract TermVectorsFormat TermVectorsFormat { get; }

        /// <summary>
        /// Encodes/decodes field infos file. </summary>
        public abstract FieldInfosFormat FieldInfosFormat { get; }

        /// <summary>
        /// Encodes/decodes segment info file. </summary>
        public abstract SegmentInfoFormat SegmentInfoFormat { get; }

        /// <summary>
        /// Encodes/decodes document normalization values. </summary>
        public abstract NormsFormat NormsFormat { get; }

        /// <summary>
        /// Encodes/decodes live docs. </summary>
        public abstract LiveDocsFormat LiveDocsFormat { get; }

        /// <summary>
        /// Looks up a codec by name. </summary>
        public static Codec ForName(string name)
        {
            return codecFactory.GetCodec(name);
        }

        /// <summary>
        /// Returns a list of all available codec names. </summary>
        public static ICollection<string> AvailableCodecs
        {
            get
            {
                if (codecFactory is IServiceListable serviceListable)
                {
                    return serviceListable.AvailableServices;
                }
                else
                {
                    throw UnsupportedOperationException.Create("The current CodecFactory class does not implement IServiceListable.");
                }
            }
        }

        // LUCENENET specific: Removed the ReloadCodecs() method because
        // this goes against the grain of standard DI practices.

        private static Codec defaultCodec;

        /// <summary>
        /// Expert: returns the default codec used for newly created
        /// <seealso cref="Index.IndexWriterConfig"/>s.
        /// </summary>
        // TODO: should we use this, or maybe a system property is better?
        public static Codec Default
        {
            get
            {
                // Lazy load the default codec if not already supplied
                if (defaultCodec is null)
                {
                    defaultCodec = Codec.ForName("Lucene46");
                }

                return defaultCodec;
            }
            set => defaultCodec = value;
        }

        /// <summary>
        /// Returns the codec's name. Subclasses can override to provide
        /// more detail (such as parameters).
        /// </summary>
        public override string ToString()
        {
            return name;
        }
    }
}