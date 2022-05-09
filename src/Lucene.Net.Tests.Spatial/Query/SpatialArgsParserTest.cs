using Lucene.Net.Util;
using NUnit.Framework;
using Spatial4n.Context;
using Spatial4n.Shapes;
using System;

namespace Lucene.Net.Spatial.Queries
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

    public class SpatialArgsParserTest : LuceneTestCase
    {
        private SpatialContext ctx = SpatialContext.Geo;

        //The args parser is only dependent on the ctx for IO so I don't care to test
        // with other implementations.

        [Test]
        public virtual void TestArgsParser()
        {
            SpatialArgsParser parser = new SpatialArgsParser();

            String arg = SpatialOperation.IsWithin + "(Envelope(-10, 10, 20, -20))";
            SpatialArgs @out = parser.Parse(arg, ctx);
            assertEquals(SpatialOperation.IsWithin, @out.Operation);
            IRectangle bounds = (IRectangle)@out.Shape;
            assertEquals(-10.0, bounds.MinX, 0D);
            assertEquals(10.0, bounds.MaxX, 0D);

            // Disjoint should not be scored
            arg = SpatialOperation.IsDisjointTo + " (Envelope(-10,-20,20,10))";
            @out = parser.Parse(arg, ctx);
            assertEquals(SpatialOperation.IsDisjointTo, @out.Operation);

            try
            {
                parser.Parse(SpatialOperation.IsDisjointTo + "[ ]", ctx);
                fail("spatial operations need args");
            }
            catch (Exception ex) when (ex.IsException())
            {
                //expected
            }

            try
            {
                parser.Parse("XXXX(Envelope(-10, 10, 20, -20))", ctx);
                fail("unknown operation!");
            }
            catch (Exception ex) when (ex.IsException())
            {
                //expected
            }
        }
    }
}
