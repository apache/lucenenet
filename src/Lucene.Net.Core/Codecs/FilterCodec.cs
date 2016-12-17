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
    /// <p>
    /// Extend this class when you need to reuse the functionality of an existing
    /// codec. For example, if you want to build a codec that redefines Lucene46's
    /// <seealso cref="LiveDocsFormat"/>:
    /// <pre class="prettyprint">
    ///   public final class CustomCodec extends FilterCodec {
    ///
    ///     public CustomCodec() {
    ///       super("CustomCodec", new Lucene46Codec());
    ///     }
    ///
    ///     public LiveDocsFormat liveDocsFormat() {
    ///       return new CustomLiveDocsFormat();
    ///     }
    ///
    ///   }
    /// </pre>
    ///
    /// <p><em>Please note:</em> Don't call <seealso cref="Codec#forName"/> from
    /// the no-arg constructor of your own codec. When the SPI framework
    /// loads your own Codec as SPI component, SPI has not yet fully initialized!
    /// If you want to extend another Codec, instantiate it directly by calling
    /// its constructor.
    ///
    /// @lucene.experimental
    /// </summary>
    public abstract class FilterCodec : Codec
    {
        /// <summary>
        /// The codec to filter. </summary>
        protected readonly Codec @delegate;

        /// <summary>
        /// Sole constructor. When subclassing this codec,
        /// create a no-arg ctor and pass the delegate codec
        /// and a unique name to this ctor.
        /// </summary>
        protected internal FilterCodec(string name, Codec @delegate)
            : base(name)
        {
            this.@delegate = @delegate;
        }

        public override DocValuesFormat DocValuesFormat
        {
            get { return @delegate.DocValuesFormat; }
        }

        public override FieldInfosFormat FieldInfosFormat
        {
            get { return @delegate.FieldInfosFormat; }
        }

        public override LiveDocsFormat LiveDocsFormat
        {
            get { return @delegate.LiveDocsFormat; }
        }

        public override NormsFormat NormsFormat
        {
            get { return @delegate.NormsFormat; }
        }

        public override PostingsFormat PostingsFormat
        {
            get { return @delegate.PostingsFormat; }
        }

        public override SegmentInfoFormat SegmentInfoFormat
        {
            get { return @delegate.SegmentInfoFormat; }
        }

        public override StoredFieldsFormat StoredFieldsFormat
        {
            get { return @delegate.StoredFieldsFormat; }
        }

        public override TermVectorsFormat TermVectorsFormat
        {
            get { return @delegate.TermVectorsFormat; }
        }
    }
}