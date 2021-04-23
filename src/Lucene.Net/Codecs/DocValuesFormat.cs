using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;

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
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Encodes/decodes per-document values.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="Name"/>) may
    /// written into the index in certain configurations. In order for the segment
    /// to be read, the name must resolve to your implementation via <see cref="ForName(string)"/>.
    /// This method uses <see cref="IDocValuesFormatFactory.GetDocValuesFormat(string)"/> to resolve format names.
    /// <para/>
    /// To implement your own format:
    /// <list type="number">
    ///     <item><description>Subclass this class.</description></item>
    ///     <item><description>Subclass <see cref="DefaultDocValuesFormatFactory"/>, override the <see cref="DefaultDocValuesFormatFactory.Initialize()"/> method,
    ///         and add the line <c>base.ScanForDocValuesFormats(typeof(YourDocValuesFormat).Assembly)</c>. 
    ///         If you have any format classes in your assembly 
    ///         that are not meant for reading, you can add the <see cref="ExcludeDocValuesFormatFromScanAttribute"/> 
    ///         to them so they are ignored by the scan.</description></item>
    ///     <item><description>Set the new <see cref="IDocValuesFormatFactory"/> by calling <see cref="SetDocValuesFormatFactory(IDocValuesFormatFactory)"/>
    ///         at application startup.</description></item>
    /// </list>
    /// If your format has dependencies, you may also override <see cref="DefaultDocValuesFormatFactory.GetDocValuesFormat(Type)"/>
    /// to inject them via pure DI or a DI container. See <a href="http://blog.ploeh.dk/2014/05/19/di-friendly-framework/">DI-Friendly Framework</a>
    /// to understand the approach used.
    /// <para/>
    /// <b>DocValuesFormat Names</b>
    /// <para/>
    /// Unlike the Java version, format names are by default convention-based on the class name. 
    /// If you name your custom format class "MyCustomDocValuesFormat", the format name will the same name 
    /// without the "DocValuesFormat" suffix: "MyCustom".
    /// <para/>
    /// You can override this default behavior by using the <see cref="DocValuesFormatNameAttribute"/> to
    /// name the format differently than this convention. Format names must be all ASCII alphanumeric, 
    /// and less than 128 characters in length.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="DefaultDocValuesFormatFactory"/>
    /// <seealso cref="IDocValuesFormatFactory"/>
    /// <seealso cref="DocValuesFormatNameAttribute"/>
    // LUCENENET specific - refactored this class so it depends on IDocValuesFormatFactory rather than
    // the Java-centric NamedSPILoader
    public abstract class DocValuesFormat //: NamedSPILoader.INamedSPI
    {
        private static IDocValuesFormatFactory docValuesFormatFactory = new DefaultDocValuesFormatFactory();

        /// <summary>
        /// Unique name that's used to retrieve this format when
        /// reading the index.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Sets the <see cref="IDocValuesFormatFactory"/> instance used to instantiate
        /// <see cref="DocValuesFormat"/> subclasses.
        /// </summary>
        /// <param name="docValuesFormatFactory">The new <see cref="IDocValuesFormatFactory"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="docValuesFormatFactory"/> parameter is <c>null</c>.</exception>
        public static void SetDocValuesFormatFactory(IDocValuesFormatFactory docValuesFormatFactory)
        {
            DocValuesFormat.docValuesFormatFactory = docValuesFormatFactory ?? throw new ArgumentNullException(nameof(docValuesFormatFactory));
        }

        /// <summary>
        /// Gets the associated <see cref="DocValuesFormat"/> factory.
        /// </summary>
        /// <returns>The <see cref="DocValuesFormat"/> factory.</returns>
        public static IDocValuesFormatFactory GetDocValuesFormatFactory()
        {
            return docValuesFormatFactory;
        }

        /// <summary>
        /// Creates a new docvalues format.
        /// <para/>
        /// The provided name will be written into the index segment in some configurations
        /// (such as when using <see cref="Codecs.PerField.PerFieldDocValuesFormat"/>): in such configurations,
        /// for the segment to be read this class should be registered by subclassing <see cref="DefaultDocValuesFormatFactory"/> and
        /// calling <see cref="DefaultDocValuesFormatFactory.ScanForDocValuesFormats(System.Reflection.Assembly)"/> in the class constructor.
        /// The new <see cref="IDocValuesFormatFactory"/> can be registered by calling <see cref="SetDocValuesFormatFactory(IDocValuesFormatFactory)"/>
        /// at application startup.
        /// </summary>
        protected DocValuesFormat()
        {
            this.name = NamedServiceFactory<DocValuesFormat>.GetServiceName(this.GetType());
        }

        /// <summary>
        /// Returns a <see cref="DocValuesConsumer"/> to write docvalues to the
        /// index.
        /// </summary>
        public abstract DocValuesConsumer FieldsConsumer(SegmentWriteState state);

        /// <summary>
        /// Returns a <see cref="DocValuesProducer"/> to read docvalues from the index.
        /// <para/>
        /// NOTE: by the time this call returns, it must hold open any files it will
        /// need to use; else, those files may be deleted. Additionally, required files
        /// may be deleted during the execution of this call before there is a chance
        /// to open them. Under these circumstances an <see cref="IOException"/> should be thrown by
        /// the implementation. <see cref="IOException"/>s are expected and will automatically cause
        /// a retry of the segment opening logic with the newly revised segments.
        /// </summary>
        public abstract DocValuesProducer FieldsProducer(SegmentReadState state);

        /// <summary>
        /// Unique name that's used to retrieve this format when
        /// reading the index.
        /// </summary>
        public string Name => name;

        public override string ToString()
        {
            return "DocValuesFormat(name=" + name + ")";
        }

        /// <summary>
        /// Looks up a format by name. </summary>
        public static DocValuesFormat ForName(string name)
        {
            return docValuesFormatFactory.GetDocValuesFormat(name);
        }

        /// <summary>
        /// Returns a list of all available format names. </summary>
        public static ICollection<string> AvailableDocValuesFormats
        {
            get
            {
                if (docValuesFormatFactory is IServiceListable serviceListable)
                {
                    return serviceListable.AvailableServices;
                }
                else
                {
                    throw UnsupportedOperationException.Create("The current DocValuesFormatFactory class does not implement IServiceListable.");
                }
            }
        }

        // LUCENENET specific: Removed the ReloadDocValuesFormats() method because
        // this goes against the grain of standard DI practices.
    }
}