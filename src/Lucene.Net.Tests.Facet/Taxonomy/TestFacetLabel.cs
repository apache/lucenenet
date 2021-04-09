// Lucene version compatibility level 4.8.1
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Facet.Taxonomy
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using SortedSetDocValuesFacetField = Lucene.Net.Facet.SortedSet.SortedSetDocValuesFacetField;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestFacetLabel : FacetTestCase
    {

        [Test]
        public virtual void TestBasic()
        {
            Assert.AreEqual(0, new FacetLabel().Length);
            Assert.AreEqual(1, new FacetLabel("hello").Length);
            Assert.AreEqual(2, new FacetLabel("hello", "world").Length);
        }

        [Test]
        public virtual void TestToString()
        {
            // When the category is empty, we expect an empty string
            Assert.AreEqual("FacetLabel: []", new FacetLabel().ToString());
            // one category
            Assert.AreEqual("FacetLabel: [hello]", new FacetLabel("hello").ToString());
            // more than one category
            Assert.AreEqual("FacetLabel: [hello, world]", new FacetLabel("hello", "world").ToString());
        }

        [Test]
        public virtual void TestGetComponent()
        {
            string[] components = new string[AtLeast(10)];
            for (int i = 0; i < components.Length; i++)
            {
                components[i] = Convert.ToString(i, CultureInfo.InvariantCulture);
            }
            FacetLabel cp = new FacetLabel(components);
            for (int i = 0; i < components.Length; i++)
            {
                Assert.AreEqual(i, Convert.ToInt32(cp.Components[i], CultureInfo.InvariantCulture));
            }
        }

        [Test]
        public virtual void TestDefaultConstructor()
        {
            // test that the default constructor (no parameters) currently
            // defaults to creating an object with a 0 initial capacity.
            // If we change this default later, we also need to change this
            // test.
            FacetLabel p = new FacetLabel();
            Assert.AreEqual(0, p.Length);
            Assert.AreEqual("FacetLabel: []", p.ToString());
        }

        [Test]
        public virtual void TestSubPath()
        {
            FacetLabel p = new FacetLabel("hi", "there", "man");
            Assert.AreEqual(p.Length, 3);

            FacetLabel p1 = p.Subpath(2);
            Assert.AreEqual(2, p1.Length);
            Assert.AreEqual("FacetLabel: [hi, there]", p1.ToString());

            p1 = p.Subpath(1);
            Assert.AreEqual(1, p1.Length);
            Assert.AreEqual("FacetLabel: [hi]", p1.ToString());

            p1 = p.Subpath(0);
            Assert.AreEqual(0, p1.Length);
            Assert.AreEqual("FacetLabel: []", p1.ToString());

            // with all the following lengths, the prefix should be the whole path 
            int[] lengths = new int[] { 3, -1, 4 };
            for (int i = 0; i < lengths.Length; i++)
            {
                p1 = p.Subpath(lengths[i]);
                Assert.AreEqual(3, p1.Length);
                Assert.AreEqual("FacetLabel: [hi, there, man]", p1.ToString());
                Assert.AreEqual(p, p1);
            }
        }

        [Test]
        public virtual void TestEquals()
        {
            Assert.AreEqual(new FacetLabel(), new FacetLabel());
            Assert.IsFalse(new FacetLabel().Equals(new FacetLabel("hi")));
            Assert.IsFalse(new FacetLabel().Equals(Convert.ToInt32(3)));
            Assert.AreEqual(new FacetLabel("hello", "world"), new FacetLabel("hello", "world"));
        }

        [Test]
        public virtual void TestHashCode()
        {
            Assert.AreEqual(new FacetLabel().GetHashCode(), new FacetLabel().GetHashCode());
            Assert.IsFalse(new FacetLabel().GetHashCode() == new FacetLabel("hi").GetHashCode());
            Assert.AreEqual(new FacetLabel("hello", "world").GetHashCode(), new FacetLabel("hello", "world").GetHashCode());
        }

        [Test]
        public virtual void TestLongHashCode()
        {
            Assert.AreEqual(new FacetLabel().Int64HashCode(), new FacetLabel().Int64HashCode());
            Assert.IsFalse(new FacetLabel().Int64HashCode() == new FacetLabel("hi").Int64HashCode());
            Assert.AreEqual(new FacetLabel("hello", "world").Int64HashCode(), new FacetLabel("hello", "world").Int64HashCode());
        }

        [Test]
        public virtual void TestArrayConstructor()
        {
            FacetLabel p = new FacetLabel("hello", "world", "yo");
            Assert.AreEqual(3, p.Length);
            Assert.AreEqual("FacetLabel: [hello, world, yo]", p.ToString());
        }

        [Test]
        public virtual void TestCompareTo()
        {
            FacetLabel p = new FacetLabel("a", "b", "c", "d");
            FacetLabel pother = new FacetLabel("a", "b", "c", "d");
            Assert.AreEqual(0, pother.CompareTo(p));
            Assert.AreEqual(0, p.CompareTo(pother));
            pother = new FacetLabel();
            Assert.IsTrue(pother.CompareTo(p) < 0);
            Assert.IsTrue(p.CompareTo(pother) > 0);
            pother = new FacetLabel("a", "b_", "c", "d");
            Assert.IsTrue(pother.CompareTo(p) > 0);
            Assert.IsTrue(p.CompareTo(pother) < 0);
            pother = new FacetLabel("a", "b", "c");
            Assert.IsTrue(pother.CompareTo(p) < 0);
            Assert.IsTrue(p.CompareTo(pother) > 0);
            pother = new FacetLabel("a", "b", "c", "e");
            Assert.IsTrue(pother.CompareTo(p) > 0);
            Assert.IsTrue(p.CompareTo(pother) < 0);
        }

        [Test]
        public virtual void TestEmptyNullComponents()
        {
            // LUCENE-4724: CategoryPath should not allow empty or null components
            string[][] components_tests = new string[][] {
                new string[] {"", "test"}, // empty in the beginning
                new string[] {"test", ""}, // empty in the end
                new string[] {"test", "", "foo"}, // empty in the middle
                new string[] {null, "test"}, // null at the beginning
                new string[] {"test", null}, // null in the end
                new string[] {"test", null, "foo"} // null in the middle
            };

            foreach (string[] components in components_tests)
            {
                try
                {
                    Assert.IsNotNull(new FacetLabel(components));
                    fail("empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    // expected
                }
                try
                {
                    _ = new FacetField("dim", components);
                    fail("empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    // expected
                }
                try
                {
                    _ = new AssociationFacetField(new BytesRef(), "dim", components);
                    fail("empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    // expected
                }
                try
                {
                    _ = new Int32AssociationFacetField(17, "dim", components);
                    fail("empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    // expected
                }
                try
                {
                    _ = new SingleAssociationFacetField(17.0f, "dim", components);
                    fail("empty or null components should not be allowed: " + Arrays.ToString(components));
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    // expected
                }
            }
            try
            {
                _ = new FacetField(null, new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new FacetField("", new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new Int32AssociationFacetField(17, null, new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new Int32AssociationFacetField(17, "", new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new SingleAssociationFacetField(17.0f, null, new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new SingleAssociationFacetField(17.0f, "", new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new AssociationFacetField(new BytesRef(), null, new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new AssociationFacetField(new BytesRef(), "", new string[] { "abc" });
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new SortedSetDocValuesFacetField(null, "abc");
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new SortedSetDocValuesFacetField("", "abc");
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new SortedSetDocValuesFacetField("dim", null);
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
            try
            {
                _ = new SortedSetDocValuesFacetField("dim", "");
                fail("empty or null components should not be allowed");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
        }

        [Test]
        public virtual void TestLongPath()
        {
            string bigComp = null;
            while (true)
            {
                int len = FacetLabel.MAX_CATEGORY_PATH_LENGTH;
                bigComp = TestUtil.RandomSimpleString(Random, len, len);
                if (bigComp.IndexOf('\u001f') != -1)
                {
                    continue;
                }
                break;
            }

            try
            {
                Assert.IsNotNull(new FacetLabel("dim", bigComp));
                fail("long paths should not be allowed; len=" + bigComp.Length);
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected
            }
        }
    }
}