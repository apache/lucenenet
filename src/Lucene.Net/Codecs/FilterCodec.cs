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
    /// A codec that forwards all its method calls to another codec.
    /// <para/>
    /// Extend this class when you need to reuse the functionality of an existing
    /// codec. For example, if you want to build a codec that redefines Lucene46's
    /// <see cref="Codecs.LiveDocsFormat"/>:
    /// <code>
    ///     public sealed class CustomCodec : FilterCodec 
    ///     {
    ///         public CustomCodec()
    ///             : base("CustomCodec", new Lucene46Codec())
    ///         {
    ///         }
    ///
    ///         public override LiveDocsFormat LiveDocsFormat 
    ///         {
    ///             get { return new CustomLiveDocsFormat(); }
    ///         }
    ///     }
    /// </code>
    ///
    /// <para/>
    /// <em>Please note:</em> Don't call <see cref="Codec.ForName(string)"/> from
    /// the no-arg constructor of your own codec. When the <see cref="DefaultCodecFactory"/>
    /// loads your own <see cref="Codec"/>, the <see cref="DefaultCodecFactory"/> has not yet fully initialized!
    /// If you want to extend another <see cref="Codec"/>, instantiate it directly by calling
    /// its constructor.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class FilterCodec : Codec
    {
        /// <summary>
        /// The codec to filter. </summary>
        protected readonly Codec m_delegate;

        /// <summary>
        /// Sole constructor. When subclassing this codec,
        /// create a no-arg ctor and pass the delegate codec
        /// and a unique name to this ctor.
        /// </summary>
        protected FilterCodec(Codec @delegate)
            : base()
        {
            this.m_delegate = @delegate;
        }

        public override DocValuesFormat DocValuesFormat => m_delegate.DocValuesFormat;

        public override FieldInfosFormat FieldInfosFormat => m_delegate.FieldInfosFormat;

        public override LiveDocsFormat LiveDocsFormat => m_delegate.LiveDocsFormat;

        public override NormsFormat NormsFormat => m_delegate.NormsFormat;

        public override PostingsFormat PostingsFormat => m_delegate.PostingsFormat;

        public override SegmentInfoFormat SegmentInfoFormat => m_delegate.SegmentInfoFormat;

        public override StoredFieldsFormat StoredFieldsFormat => m_delegate.StoredFieldsFormat;

        public override TermVectorsFormat TermVectorsFormat => m_delegate.TermVectorsFormat;
    }
}