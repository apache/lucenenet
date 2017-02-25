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
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;

    /// <summary>
    /// Encodes/decodes per-document values.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="Name"/>) may
    /// written into the index in certain configurations. In order for the segment
    /// to be read, the name must resolve to your implementation via <see cref="ForName(string)"/>.
    /// this method uses <see cref="IDocValuesFormatFactory.GetDocValuesFormat(string)"/> to resolve format names.
    /// <para/>
    /// To implement your own format:
    /// <list type="number">
    ///     <item>Subclass this class.</item>
    ///     <item>Subclass <see cref="DefaultDocValuesFormatFactory"/> and add the line
    ///         <c>base.ScanForDocValuesFormats(typeof(YourDocValuesFormat).GetTypeInfo().Assembly)</c>
    ///         to the constructor. If you have any format classes in your assembly 
    ///         that are not meant for reading, you can add the <see cref="IgnoreDocValuesFormatAttribute"/> 
    ///         to them so they are ignored by the scan.</item>
    ///     <item>Set the new <see cref="IDocValuesFormatFactory"/> by calling <see cref="SetDocValuesFormatFactory(IDocValuesFormatFactory)"/>
    ///         at application startup.</item>
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
            if (docValuesFormatFactory == null)
                throw new ArgumentNullException("docValuesFormatFactory");
            DocValuesFormat.docValuesFormatFactory = docValuesFormatFactory;
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
        /// (such as when using <see cref="Codecs.Perfield.PerFieldDocValuesFormat"/>): in such configurations,
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
        /// to open them. Under these circumstances an IOException should be thrown by
        /// the implementation. IOExceptions are expected and will automatically cause
        /// a retry of the segment opening logic with the newly revised segments.
        /// </summary>
        public abstract DocValuesProducer FieldsProducer(SegmentReadState state);

        /// <summary>
        /// Unique name that's used to retrieve this format when
        /// reading the index.
        /// </summary>
        public string Name
        {
            get
            {
                return name;
            }
        }

        public override string ToString()
        {
            return "DocValuesFormat(name=" + name + ")";
        }

        /// <summary>
        /// looks up a format by name </summary>
        public static DocValuesFormat ForName(string name)
        {
            return docValuesFormatFactory.GetDocValuesFormat(name);
        }

        /// <summary>
        /// returns a list of all available format names </summary>
        public static ICollection<string> AvailableDocValuesFormats()
        {
            if (docValuesFormatFactory is IServiceListable)
            {
                return ((IServiceListable)docValuesFormatFactory).AvailableServices();
            }
            else
            {
                throw new NotSupportedException("The current DocValuesFormatFactory class does not implement IServiceListable.");
            }
        }

        // LUCENENET specific: Removed the ReloadDocValuesFormats() method because
        // this goes against the grain of standar DI practices.
    }


    ///// <summary>
    ///// Encodes/decodes per-document values.
    ///// <p>
    ///// Note, when extending this class, the name (<seealso cref="#getName"/>) may
    ///// written into the index in certain configurations. In order for the segment
    ///// to be read, the name must resolve to your implementation via <seealso cref="#forName(String)"/>.
    ///// this method uses Java's
    ///// <seealso cref="ServiceLoader Service Provider Interface"/> (SPI) to resolve format names.
    ///// <p>
    ///// If you implement your own format, make sure that it has a no-arg constructor
    ///// so SPI can load it. </summary>
    ///// <seealso cref= ServiceLoader
    ///// @lucene.experimental  </seealso>
    //public abstract class DocValuesFormat : NamedSPILoader.INamedSPI
    //{
    //    private static readonly NamedSPILoader<DocValuesFormat> loader = new NamedSPILoader<DocValuesFormat>(typeof(DocValuesFormat));

    //    /// <summary>
    //    /// Unique name that's used to retrieve this format when
    //    ///  reading the index.
    //    /// </summary>
    //    private readonly string name;

    //    /// <summary>
    //    /// Creates a new docvalues format.
    //    /// <p>
    //    /// The provided name will be written into the index segment in some configurations
    //    /// (such as when using {@code PerFieldDocValuesFormat}): in such configurations,
    //    /// for the segment to be read this class should be registered with Java's
    //    /// SPI mechanism (registered in META-INF/ of your jar file, etc). </summary>
    //    /// <param name="name"> must be all ascii alphanumeric, and less than 128 characters in length. </param>
    //    protected internal DocValuesFormat(string name)
    //    {
    //        NamedSPILoader<DocValuesFormat>.CheckServiceName(name);
    //        this.name = name;
    //    }

    //    /// <summary>
    //    /// Returns a <seealso cref="DocValuesConsumer"/> to write docvalues to the
    //    ///  index.
    //    /// </summary>
    //    public abstract DocValuesConsumer FieldsConsumer(SegmentWriteState state);

    //    /// <summary>
    //    /// Returns a <seealso cref="DocValuesProducer"/> to read docvalues from the index.
    //    /// <p>
    //    /// NOTE: by the time this call returns, it must hold open any files it will
    //    /// need to use; else, those files may be deleted. Additionally, required files
    //    /// may be deleted during the execution of this call before there is a chance
    //    /// to open them. Under these circumstances an IOException should be thrown by
    //    /// the implementation. IOExceptions are expected and will automatically cause
    //    /// a retry of the segment opening logic with the newly revised segments.
    //    /// </summary>
    //    public abstract DocValuesProducer FieldsProducer(SegmentReadState state);

    //    public string Name
    //    {
    //        get
    //        {
    //            return name;
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        return "DocValuesFormat(name=" + name + ")";
    //    }

    //    /// <summary>
    //    /// looks up a format by name </summary>
    //    public static DocValuesFormat ForName(string name)
    //    {
    //        if (loader == null)
    //        {
    //            throw new InvalidOperationException("You called DocValuesFormat.forName() before all formats could be initialized. " + "this likely happens if you call it from a DocValuesFormat's ctor.");
    //        }
    //        return loader.Lookup(name);
    //    }

    //    /// <summary>
    //    /// returns a list of all available format names </summary>
    //    public static ISet<string> AvailableDocValuesFormats()
    //    {
    //        if (loader == null)
    //        {
    //            throw new InvalidOperationException("You called DocValuesFormat.availableDocValuesFormats() before all formats could be initialized. " + "this likely happens if you call it from a DocValuesFormat's ctor.");
    //        }
    //        return loader.AvailableServices();
    //    }

    //    /// <summary>
    //    /// Reloads the DocValues format list from the given <seealso cref="ClassLoader"/>.
    //    /// Changes to the docvalues formats are visible after the method ends, all
    //    /// iterators (<seealso cref="#availableDocValuesFormats()"/>,...) stay consistent.
    //    ///
    //    /// <p><b>NOTE:</b> Only new docvalues formats are added, existing ones are
    //    /// never removed or replaced.
    //    ///
    //    /// <p><em>this method is expensive and should only be called for discovery
    //    /// of new docvalues formats on the given classpath/classloader!</em>
    //    /// </summary>
    //    public static void ReloadDocValuesFormats()
    //    {
    //        loader.Reload();
    //    }
    //}
}