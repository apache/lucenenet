using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

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

    [TestFixture]
    public class TestSloppyMath : LuceneTestCase
    {
        // accuracy for cos()
        internal static double COS_DELTA = 1E-15;

        // accuracy for asin()
        internal static double ASIN_DELTA = 1E-7;

        [Test]
        public virtual void TestCos()
        {
            Assert.IsTrue(double.IsNaN(Math.Cos(double.NaN)));
            Assert.IsTrue(double.IsNaN(Math.Cos(double.NegativeInfinity)));
            Assert.IsTrue(double.IsNaN(Math.Cos(double.PositiveInfinity)));
            Assert.AreEqual(Math.Cos(1), Math.Cos(1), COS_DELTA);
            Assert.AreEqual(Math.Cos(0), Math.Cos(0), COS_DELTA);
            Assert.AreEqual(Math.Cos(Math.PI / 2), Math.Cos(Math.PI / 2), COS_DELTA);
            Assert.AreEqual(Math.Cos(-Math.PI / 2), Math.Cos(-Math.PI / 2), COS_DELTA);
            Assert.AreEqual(Math.Cos(Math.PI / 4), Math.Cos(Math.PI / 4), COS_DELTA);
            Assert.AreEqual(Math.Cos(-Math.PI / 4), Math.Cos(-Math.PI / 4), COS_DELTA);
            Assert.AreEqual(Math.Cos(Math.PI * 2 / 3), Math.Cos(Math.PI * 2 / 3), COS_DELTA);
            Assert.AreEqual(Math.Cos(-Math.PI * 2 / 3), Math.Cos(-Math.PI * 2 / 3), COS_DELTA);
            Assert.AreEqual(Math.Cos(Math.PI / 6), Math.Cos(Math.PI / 6), COS_DELTA);
            Assert.AreEqual(Math.Cos(-Math.PI / 6), Math.Cos(-Math.PI / 6), COS_DELTA);

            // testing purely random longs is inefficent, as for stupid parameters we just
            // pass thru to Math.cos() instead of doing some huperduper arg reduction
            for (int i = 0; i < 10000; i++)
            {
                double d = Random.NextDouble() * SloppyMath.SIN_COS_MAX_VALUE_FOR_INT_MODULO;
                if (Random.NextBoolean())
                {
                    d = -d;
                }
                Assert.AreEqual(Math.Cos(d), Math.Cos(d), COS_DELTA);
            }
        }

        [Test]
        public virtual void TestAsin()
        {
            Assert.IsTrue(double.IsNaN(Math.Asin(double.NaN)));
            Assert.IsTrue(double.IsNaN(Math.Asin(2)));
            Assert.IsTrue(double.IsNaN(Math.Asin(-2)));
            Assert.AreEqual(-Math.PI / 2, Math.Asin(-1), ASIN_DELTA);
            Assert.AreEqual(-Math.PI / 3, Math.Asin(-0.8660254), ASIN_DELTA);
            Assert.AreEqual(-Math.PI / 4, Math.Asin(-0.7071068), ASIN_DELTA);
            Assert.AreEqual(-Math.PI / 6, Math.Asin(-0.5), ASIN_DELTA);
            Assert.AreEqual(0, Math.Asin(0), ASIN_DELTA);
            Assert.AreEqual(Math.PI / 6, Math.Asin(0.5), ASIN_DELTA);
            Assert.AreEqual(Math.PI / 4, Math.Asin(0.7071068), ASIN_DELTA);
            Assert.AreEqual(Math.PI / 3, Math.Asin(0.8660254), ASIN_DELTA);
            Assert.AreEqual(Math.PI / 2, Math.Asin(1), ASIN_DELTA);
            // only values -1..1 are useful
            for (int i = 0; i < 10000; i++)
            {
                double d = Random.NextDouble();
                if (Random.NextBoolean())
                {
                    d = -d;
                }
                Assert.AreEqual(Math.Asin(d), Math.Asin(d), ASIN_DELTA);
                Assert.IsTrue(Math.Asin(d) >= -Math.PI / 2);
                Assert.IsTrue(Math.Asin(d) <= Math.PI / 2);
            }
        }

        [Test]
        public virtual void TestHaversin()
        {
            Assert.IsTrue(double.IsNaN(SloppyMath.Haversin(1, 1, 1, double.NaN)));
            Assert.IsTrue(double.IsNaN(SloppyMath.Haversin(1, 1, double.NaN, 1)));
            Assert.IsTrue(double.IsNaN(SloppyMath.Haversin(1, double.NaN, 1, 1)));
            Assert.IsTrue(double.IsNaN(SloppyMath.Haversin(double.NaN, 1, 1, 1)));

            Assert.AreEqual(0, SloppyMath.Haversin(0, 0, 0, 0), 0D);
            Assert.AreEqual(0, SloppyMath.Haversin(0, -180, 0, -180), 0D);
            Assert.AreEqual(0, SloppyMath.Haversin(0, -180, 0, 180), 0D);
            Assert.AreEqual(0, SloppyMath.Haversin(0, 180, 0, 180), 0D);
            Assert.AreEqual(0, SloppyMath.Haversin(90, 0, 90, 0), 0D);
            Assert.AreEqual(0, SloppyMath.Haversin(90, -180, 90, -180), 0D);
            Assert.AreEqual(0, SloppyMath.Haversin(90, -180, 90, 180), 0D);
            Assert.AreEqual(0, SloppyMath.Haversin(90, 180, 90, 180), 0D);

            // Test half a circle on the equator, using WGS84 earth radius
            double earthRadiusKMs = 6378.137;
            double halfCircle = earthRadiusKMs * Math.PI;
            Assert.AreEqual(halfCircle, SloppyMath.Haversin(0, 0, 0, 180), 0D);

            Random r = Random;
            double randomLat1 = 40.7143528 + (r.Next(10) - 5) * 360;
            double randomLon1 = -74.0059731 + (r.Next(10) - 5) * 360;

            double randomLat2 = 40.65 + (r.Next(10) - 5) * 360;
            double randomLon2 = -73.95 + (r.Next(10) - 5) * 360;

            Assert.AreEqual(8.572, SloppyMath.Haversin(randomLat1, randomLon1, randomLat2, randomLon2), 0.01D);

            // from solr and ES tests (with their respective epsilons)
            Assert.AreEqual(0, SloppyMath.Haversin(40.7143528, -74.0059731, 40.7143528, -74.0059731), 0D);
            Assert.AreEqual(5.286, SloppyMath.Haversin(40.7143528, -74.0059731, 40.759011, -73.9844722), 0.01D);
            Assert.AreEqual(0.4621, SloppyMath.Haversin(40.7143528, -74.0059731, 40.718266, -74.007819), 0.01D);
            Assert.AreEqual(1.055, SloppyMath.Haversin(40.7143528, -74.0059731, 40.7051157, -74.0088305), 0.01D);
            Assert.AreEqual(1.258, SloppyMath.Haversin(40.7143528, -74.0059731, 40.7247222, -74), 0.01D);
            Assert.AreEqual(2.029, SloppyMath.Haversin(40.7143528, -74.0059731, 40.731033, -73.9962255), 0.01D);
            Assert.AreEqual(8.572, SloppyMath.Haversin(40.7143528, -74.0059731, 40.65, -73.95), 0.01D);
        }
    }
}