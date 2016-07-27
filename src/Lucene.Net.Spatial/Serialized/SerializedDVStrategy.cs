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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lucene.Net.Documents;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Query;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Spatial4n.Core.Context;
using Spatial4n.Core.Shapes;

namespace Lucene.Net.Spatial.Serialized
{
    /// <summary>
    /// A SpatialStrategy based on serializing a Shape stored into BinaryDocValues.
    /// This is not at all fast; it's designed to be used in conjuction with another index based
    /// SpatialStrategy that is approximated(like { @link org.apache.lucene.spatial.prefix.RecursivePrefixTreeStrategy})
    /// to add precision or eventually make more specific / advanced calculations on the per-document
    /// geometry.
    /// The serialization uses Spatial4j's {@link com.spatial4j.core.io.BinaryCodec}.
    ///
    /// @lucene.experimental
    /// </summary>
    public class SerializedDVStrategy : SpatialStrategy
    {
        public SerializedDVStrategy(SpatialContext ctx, string fieldName) : base(ctx, fieldName)
        {
            throw new NotImplementedException();
        }

        public override Field[] CreateIndexableFields(Shape shape)
        {
            throw new NotImplementedException();
        }

        public override ValueSource MakeDistanceValueSource(Point queryPoint, double multiplier)
        {
            throw new NotImplementedException();
        }

        public override Filter MakeFilter(SpatialArgs args)
        {
            throw new NotImplementedException();
        }
    }

}
