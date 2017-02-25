using Lucene.Net.Index;
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
    /// Encodes/decodes terms, postings, and proximity data.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="Name"/>) may
    /// written into the index in certain configurations. In order for the segment
    /// to be read, the name must resolve to your implementation via <see cref="ForName(string)"/>.
    /// this method uses <see cref="IPostingsFormatFactory.GetPostingsFormat(string)"/> to resolve format names.
    /// <para/>
    /// If you implement your own format:
    /// <list type="number">
    ///     <item>Subclass this class.</item>
    ///     <item>Subclass <see cref="DefaultPostingsFormatFactory"/> and add the line 
    ///         <c>base.ScanForPostingsFormats(typeof(YourPostingsFormat).GetTypeInfo().Assembly)</c> 
    ///         to the constructor. If you have any format classes in your assembly 
    ///         that are not meant for reading, you can add the <see cref="IgnorePostingsFormatAttribute"/> 
    ///         to them so they are ignored by the scan.</item>
    ///     <item>Set the new <see cref="IPostingsFormatFactory"/> by calling <see cref="SetPostingsFormatFactory(IPostingsFormatFactory)"/> 
    ///         at application startup.</item>
    /// </list>
    /// If your format has dependencies, you may also override <see cref="DefaultPostingsFormatFactory.GetPostingsFormat(Type)"/> to inject 
    /// them via pure DI or a DI container. See <a href="http://blog.ploeh.dk/2014/05/19/di-friendly-framework/">DI-Friendly Framework</a>
    /// to understand the approach used.
    /// <para/>
    /// <b>PostingsFormat Names</b>
    /// <para/>
    /// Unlike the Java version, format names are by default convention-based on the class name. 
    /// If you name your custom format class "MyCustomPostingsFormat", the codec name will the same name 
    /// without the "PostingsFormat" suffix: "MyCustom".
    /// <para/>
    /// You can override this default behavior by using the <see cref="PostingsFormatNameAttribute"/> to
    /// name the format differently than this convention. Format names must be all ASCII alphanumeric, 
    /// and less than 128 characters in length.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <seealso cref="DefaultPostingsFormatFactory"/>
    /// <seealso cref="IPostingsFormatFactory"/>
    /// <seealso cref="PostingsFormatNameAttribute"/>
    public abstract class PostingsFormat //: NamedSPILoader.INamedSPI
    {
        private static IPostingsFormatFactory postingsFormatFactory = new DefaultPostingsFormatFactory();

        /// <summary>
        /// Zero-length <see cref="PostingsFormat"/> array. </summary>
        public static readonly PostingsFormat[] EMPTY = new PostingsFormat[0];

        /// <summary>
        /// Unique name that's used to retrieve this format when
        /// reading the index.
        /// </summary>
        private readonly string name;

        /// <summary>
        /// Sets the <see cref="IPostingsFormatFactory"/> instance used to instantiate
        /// <see cref="PostingsFormat"/> subclasses.
        /// </summary>
        /// <param name="postingsFormatFactory">The new <see cref="IPostingsFormatFactory"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="postingsFormatFactory"/> parameter is <c>null</c>.</exception>
        public static void SetPostingsFormatFactory(IPostingsFormatFactory postingsFormatFactory)
        {
            if (postingsFormatFactory == null)
                throw new ArgumentNullException("postingsFormatFactory");
            PostingsFormat.postingsFormatFactory = postingsFormatFactory;
        }

        /// <summary>
        /// Gets the associated <see cref="PostingsFormat"/> factory.
        /// </summary>
        /// <returns>The <see cref="PostingsFormat"/> factory.</returns>
        public static IPostingsFormatFactory GetPostingsFormatFactory()
        {
            return postingsFormatFactory;
        }

        /// <summary>
        /// Creates a new postings format.
        /// <para/>
        /// The provided name will be written into the index segment in some configurations
        /// (such as when using <see cref="PerField.PerFieldPostingsFormat"/>): in such configurations,
        /// for the segment to be read this class should be registered by subclassing <see cref="DefaultPostingsFormatFactory"/> and
        /// calling <see cref="DefaultPostingsFormatFactory.ScanForPostingsFormats(System.Reflection.Assembly)"/> in the class constructor. 
        /// The new <see cref="IPostingsFormatFactory"/> can be registered by calling <see cref="SetPostingsFormatFactory"/> at application startup.</summary>
        protected PostingsFormat()
        {
            this.name = NamedServiceFactory<PostingsFormat>.GetServiceName(this.GetType());
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
        /// returns, it must hold open any files it will need to
        /// use; else, those files may be deleted.
        /// Additionally, required files may be deleted during the execution of
        /// this call before there is a chance to open them. Under these
        /// circumstances an IOException should be thrown by the implementation.
        /// IOExceptions are expected and will automatically cause a retry of the
        /// segment opening logic with the newly revised segments.
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
            return postingsFormatFactory.GetPostingsFormat(name);
        }

        /// <summary>
        /// returns a list of all available format names </summary>
        public static ICollection<string> AvailablePostingsFormats()
        {
            if (postingsFormatFactory is IServiceListable)
            {
                return ((IServiceListable)postingsFormatFactory).AvailableServices();
            }
            else
            {
                throw new NotSupportedException("The current PostingsFormat factory class does not implement IServiceListable.");
            }
        }

        // LUCENENET specific: Removed the ReloadPostingsFormats() method because
        // this goes against the grain of standard DI practices.
    }


    ///// <summary>
    ///// Encodes/decodes terms, postings, and proximity data.
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
    //public abstract class PostingsFormat : NamedSPILoader.INamedSPI
    //{
    //    private static readonly NamedSPILoader<PostingsFormat> loader = new NamedSPILoader<PostingsFormat>(typeof(PostingsFormat));

    //    /// <summary>
    //    /// Zero-length {@code PostingsFormat} array. </summary>
    //    public static readonly PostingsFormat[] EMPTY = new PostingsFormat[0];

    //    /// <summary>
    //    /// Unique name that's used to retrieve this format when
    //    ///  reading the index.
    //    /// </summary>
    //    private readonly string name;

    //    /// <summary>
    //    /// Creates a new postings format.
    //    /// <p>
    //    /// The provided name will be written into the index segment in some configurations
    //    /// (such as when using <seealso cref="PerFieldPostingsFormat"/>): in such configurations,
    //    /// for the segment to be read this class should be registered with Java's
    //    /// SPI mechanism (registered in META-INF/ of your jar file, etc). </summary>
    //    /// <param name="name"> must be all ascii alphanumeric, and less than 128 characters in length. </param>
    //    protected PostingsFormat(string name)
    //    {
    //        NamedSPILoader<PostingsFormat>.CheckServiceName(name);
    //        this.name = name;
    //    }

    //    /// <summary>
    //    /// Returns this posting format's name </summary>
    //    public string Name
    //    {
    //        get
    //        {
    //            return name;
    //        }
    //    }

    //    /// <summary>
    //    /// Writes a new segment </summary>
    //    public abstract FieldsConsumer FieldsConsumer(SegmentWriteState state);

    //    /// <summary>
    //    /// Reads a segment.  NOTE: by the time this call
    //    ///  returns, it must hold open any files it will need to
    //    ///  use; else, those files may be deleted.
    //    ///  Additionally, required files may be deleted during the execution of
    //    ///  this call before there is a chance to open them. Under these
    //    ///  circumstances an IOException should be thrown by the implementation.
    //    ///  IOExceptions are expected and will automatically cause a retry of the
    //    ///  segment opening logic with the newly revised segments.
    //    ///
    //    /// </summary>
    //    public abstract FieldsProducer FieldsProducer(SegmentReadState state);

    //    public override string ToString()
    //    {
    //        return "PostingsFormat(name=" + name + ")";
    //    }

    //    /// <summary>
    //    /// looks up a format by name </summary>
    //    public static PostingsFormat ForName(string name)
    //    {
    //        if (loader == null)
    //        {
    //            throw new InvalidOperationException("You called PostingsFormat.forName() before all formats could be initialized. " + "this likely happens if you call it from a PostingsFormat's ctor.");
    //        }
    //        return loader.Lookup(name);
    //    }

    //    /// <summary>
    //    /// returns a list of all available format names </summary>
    //    public static ISet<string> AvailablePostingsFormats()
    //    {
    //        if (loader == null)
    //        {
    //            throw new InvalidOperationException("You called PostingsFormat.availablePostingsFormats() before all formats could be initialized. " + "this likely happens if you call it from a PostingsFormat's ctor.");
    //        }
    //        return loader.AvailableServices();
    //    }

    //    /// <summary>
    //    /// Reloads the postings format list from the given <seealso cref="ClassLoader"/>.
    //    /// Changes to the postings formats are visible after the method ends, all
    //    /// iterators (<seealso cref="#availablePostingsFormats()"/>,...) stay consistent.
    //    ///
    //    /// <p><b>NOTE:</b> Only new postings formats are added, existing ones are
    //    /// never removed or replaced.
    //    ///
    //    /// <p><em>this method is expensive and should only be called for discovery
    //    /// of new postings formats on the given classpath/classloader!</em>
    //    /// </summary>
    //    public static void ReloadPostingsFormats()
    //    {
    //        loader.Reload();
    //    }
    //}
}