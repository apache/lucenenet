// Lucene version compatibility level 7.1.0
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Facet.Taxonomy.WriterCache
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

    [TestFixture]
    public class TestLruTaxonomyWriterCache : FacetTestCase
    {
        [Test]
        public virtual void TestDefaultLRUTypeIsCollisionSafe()
        {
            // These labels are clearly different, but have identical longHashCodes.
            // Note that these labels are clearly contrived. We did encounter
            // collisions in actual production data, but we aren't allowed to publish
            // those.
            // LUCENENET specific: The upstream labels only collide using Java's
            // String.hashCode(), and .NET's string.GetHashCode() is randomized per
            // process, so we use a subclass of FacetLabel that produces collisions
            // (as the comment below suggests) instead.
            //FacetLabel a = new FacetLabel("\0", "\u0003￢");
            //FacetLabel b = new FacetLabel("\u0001", "\0");
            FacetLabel a = new CollidingFacetLabel("\0", "\u0003￢");
            FacetLabel b = new CollidingFacetLabel("\u0001", "\0");
            // If this fails, then the longHashCode implementation has changed. This
            // cannot prevent collisions. (All hashes must allow for collisions.) It
            // will however stop the rest of this test from making sense. To fix, find
            // new colliding labels, or make a subclass of FacetLabel that produces
            // collisions.
            Assert.AreEqual(a.Int64HashCode(), b.Int64HashCode());
            // Make a cache with capacity > 2 so both our labels will fit. Don't
            // specify an LRUType, since we want to check if the default is
            // collision-safe.
            LruTaxonomyWriterCache cache = new LruTaxonomyWriterCache(10);
            cache.Put(a, 0);
            cache.Put(b, 1);
            Assert.AreEqual(0, cache.Get(a));
            Assert.AreEqual(1, cache.Get(b));
        }

        // LUCENENET specific - FacetLabel whose Int64HashCode() always collides
        private sealed class CollidingFacetLabel : FacetLabel
        {
            public CollidingFacetLabel(params string[] components)
                : base(components)
            {
            }

            public override long Int64HashCode() => 0;
        }
    }
}
