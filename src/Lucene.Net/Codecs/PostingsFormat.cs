using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Encodes/decodes terms, postings, and proximity data.
    /// <para/>
    /// Note, when extending this class, the name (<see cref="Name"/>) may
    /// written into the index in certain configurations. In order for the segment
    /// to be read, the name must resolve to your implementation via <see cref="ForName(string)"/>.
    /// This method uses <see cref="IPostingsFormatFactory.GetPostingsFormat(string)"/> to resolve format names.
    /// <para/>
    /// If you implement your own format:
    /// <list type="number">
    ///     <item><description>Subclass this class.</description></item>
    ///     <item><description>Subclass <see cref="DefaultPostingsFormatFactory"/>, override <see cref="DefaultPostingsFormatFactory.Initialize()"/>,
    ///         and add the line <c>base.ScanForPostingsFormats(typeof(YourPostingsFormat).Assembly)</c>. 
    ///         If you have any format classes in your assembly 
    ///         that are not meant for reading, you can add the <see cref="ExcludePostingsFormatFromScanAttribute"/> 
    ///         to them so they are ignored by the scan.</description></item>
    ///     <item><description>Set the new <see cref="IPostingsFormatFactory"/> by calling <see cref="SetPostingsFormatFactory(IPostingsFormatFactory)"/> 
    ///         at application startup.</description></item>
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
    // LUCENENET specific - refactored this class so it depends on IPostingsFormatFactory rather than
    // the Java-centric NamedSPILoader
    public abstract class PostingsFormat //: NamedSPILoader.INamedSPI
    {
        private static IPostingsFormatFactory postingsFormatFactory = new DefaultPostingsFormatFactory();

        /// <summary>
        /// Zero-length <see cref="PostingsFormat"/> array. </summary>
        [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "This is a SonarCloud issue")]
        [SuppressMessage("Performance", "S3887:Use an immutable collection or reduce the accessibility of the non-private readonly field", Justification = "Collection is immutable")]
        [SuppressMessage("Performance", "S2386:Use an immutable collection or reduce the accessibility of the public static field", Justification = "Collection is immutable")]
        public static readonly PostingsFormat[] EMPTY = Arrays.Empty<PostingsFormat>();

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
            PostingsFormat.postingsFormatFactory = postingsFormatFactory ?? throw new ArgumentNullException(nameof(postingsFormatFactory));
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
        /// The new <see cref="IPostingsFormatFactory"/> can be registered by calling <see cref="SetPostingsFormatFactory(IPostingsFormatFactory)"/> at application startup.</summary>
        protected PostingsFormat()
        {
            this.name = NamedServiceFactory<PostingsFormat>.GetServiceName(this.GetType());
        }

        /// <summary>
        /// Returns this posting format's name. </summary>
        public string Name => name;

        /// <summary>
        /// Writes a new segment. </summary>
        public abstract FieldsConsumer FieldsConsumer(SegmentWriteState state);

        /// <summary>
        /// Reads a segment.  NOTE: by the time this call
        /// returns, it must hold open any files it will need to
        /// use; else, those files may be deleted.
        /// Additionally, required files may be deleted during the execution of
        /// this call before there is a chance to open them. Under these
        /// circumstances an <see cref="IOException"/> should be thrown by the implementation.
        /// <see cref="IOException"/>s are expected and will automatically cause a retry of the
        /// segment opening logic with the newly revised segments.
        /// </summary>
        public abstract FieldsProducer FieldsProducer(SegmentReadState state);

        public override string ToString()
        {
            return "PostingsFormat(name=" + name + ")";
        }

        /// <summary>
        /// Looks up a format by name. </summary>
        public static PostingsFormat ForName(string name)
        {
            return postingsFormatFactory.GetPostingsFormat(name);
        }

        /// <summary>
        /// Returns a list of all available format names. </summary>
        public static ICollection<string> AvailablePostingsFormats
        {
            get
            {
                if (postingsFormatFactory is IServiceListable serviceListable)
                {
                    return serviceListable.AvailableServices;
                }
                else
                {
                    throw UnsupportedOperationException.Create("The current PostingsFormat factory class does not implement IServiceListable.");
                }
            }
        }

        // LUCENENET specific: Removed the ReloadPostingsFormats() method because
        // this goes against the grain of standard DI practices.
    }
}