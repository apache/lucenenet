using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
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
    /// LUCENENET specific contract that provides support for <see cref="Codecs.Codec.AvailableCodecs()"/>, 
    /// <see cref="Codecs.DocValuesFormat.AvailableDocValuesFormats()"/>, 
    /// and <see cref="Codecs.PostingsFormat.AvailablePostingsFormats()"/>. Implement this
    /// interface in addition to <see cref="Codecs.ICodecFactory"/>, <see cref="Codecs.IDocValuesFormatFactory"/>,
    /// or <see cref="Codecs.IPostingsFormatFactory"/> to provide optional support for the above
    /// methods when providing a custom implementation. If this interface is not supported by
    /// the corresponding factory, a <see cref="NotSupportedException"/> will be thrown from the above methods.
    /// </summary>
    public interface IServiceListable
    {
        /// <summary>
        /// Lists the available services for the current service type.
        /// </summary>
        /// <returns></returns>
        ICollection<string> AvailableServices { get; }
    }
}
