// Some tests from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/ArraysTest.java

using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
#nullable enable

namespace Lucene.Net.Support
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
    public class TestArrays : LuceneTestCase
    {
        /// <summary>
        /// Adapted from test_fill$BB() in Harmony
        /// </summary>
        [Test]
        public void TestFill_ByteArray_Byte()
        {
            // Test for method void Arrays.Fill(byte[], byte)
            byte[] d = new byte[1000];
            Arrays.Fill(d, byte.MaxValue);
            for (int i = 0; i < d.Length; i++)
            {
                assertTrue("Failed to fill byte array correctly", d[i] == byte.MaxValue);
            }
        }

        /// <summary>
        /// Adapted from test_fill$BIIB() in Harmony
        /// </summary>
        [Test]
        public void TestFill_ByteArray_Int32_Int32_Byte()
        {
            // Test for method void Arrays.Fill(byte[], int, int, byte)
            const byte val = byte.MaxValue;
            byte[] d = new byte[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                assertTrue("Failed to fill byte array correctly", d[i] == val);
            }

            int result;
            try
            {
                Arrays.Fill(new byte[2], 2, 1, (byte)27);
                result = 0;
            }
            catch (Exception e) when (e.IsArrayIndexOutOfBoundsException())
            {
                result = 1;
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                result = 2;
            }

            assertEquals("Wrong exception1", 2, result);
            try
            {
                Arrays.Fill(new byte[2], -1, 1, (byte)27);
                result = 0;
            }
            catch (Exception e) when (e.IsArrayIndexOutOfBoundsException())
            {
                result = 1;
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                result = 2;
            }

            assertEquals("Wrong exception2", 1, result);
            try
            {
                Arrays.Fill(new byte[2], 1, 4, (byte)27);
                result = 0;
            }
            catch (Exception e) when (e.IsArrayIndexOutOfBoundsException())
            {
                result = 1;
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                result = 2;
            }

            assertEquals("Wrong exception", 1, result);
        }

        /// <summary>
        /// Adapted from test_fill$SS() in Harmony
        /// </summary>
        [Test]
        public void TestFill_Int16Array_Int16()
        {
            // Test for method void Arrays.Fill(short[], short)
            short[] d = new short[1000];
            Arrays.Fill(d, short.MaxValue);
            for (int i = 0; i < d.Length; i++)
            {
                assertTrue("Failed to fill short array correctly", d[i] == short.MaxValue);
            }
        }

        /// <summary>
        /// Adapted from test_fill$SIIS() in Harmony
        /// </summary>
        [Test]
        public void TestFill_Int16Array_Int32_Int32_Int16()
        {
            // Test for method void Arrays.Fill(short[], int, int, short)
            const short val = short.MaxValue;
            short[] d = new short[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                assertTrue("Failed to fill short array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$CC() in Harmony
        /// </summary>
        [Test]
        public void TestFill_CharArray_Char()
        {
            // Test for method void Arrays.Fill(char[], char)
            char[] d = new char[1000];
            Arrays.Fill(d, 'V');
            for (int i = 0; i < d.Length; i++)
            {
                assertEquals("Failed to fill char array correctly", 'V', d[i]);
            }
        }

        /// <summary>
        /// Adapted from test_fill$CIIC() in Harmony
        /// </summary>
        [Test]
        public void TestFill_CharArray_Int32_Int32_Char()
        {
            // Test for method void Arrays.Fill(char[], int, int, char)
            const char val = 'T';
            char[] d = new char[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                assertTrue("Failed to fill char array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$II() in Harmony
        /// </summary>
        [Test]
        public void TestFill_Int32Array_Int32()
        {
            // Test for method void Arrays.Fill(int[], int)
            int[] d = new int[1000];
            Arrays.Fill(d, int.MaxValue);
            for (int i = 0; i < d.Length; i++)
            {
                assertTrue("Failed to fill int array correctly", d[i] == int.MaxValue);
            }
        }

        /// <summary>
        /// Adapted from test_fill$IIII() in Harmony
        /// </summary>
        [Test]
        public void TestFill_Int32Array_Int32_Int32_Int32()
        {
            // Test for method void Arrays.Fill(int[], int, int, int)
            const int val = int.MaxValue;
            int[] d = new int[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                assertTrue("Failed to fill int array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$JJ() in Harmony
        /// </summary>
        [Test]
        public void TestFill_Int64Array_Int64()
        {
            // Test for method void Arrays.Fill(long[], long)
            long[] d = new long[1000];
            Arrays.Fill(d, long.MaxValue);
            for (int i = 0; i < d.Length; i++)
            {
                assertTrue("Failed to fill long array correctly", d[i] == long.MaxValue);
            }
        }

        /// <summary>
        /// Adapted from test_fill$JIIJ() in Harmony
        /// </summary>
        [Test]
        public void TestFill_Int64Array_Int32_Int32_Int64()
        {
            // Test for method void Arrays.Fill(long[], int, int, long)
            const long val = long.MaxValue;
            long[] d = new long[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                assertTrue("Failed to fill long array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$FF() in Harmony
        /// </summary>
        [Test]
        public void TestFill_SingleArray_Single()
        {
            // Test for method void Arrays.Fill(float[], float)
            float[] d = new float[1000];
            Arrays.Fill(d, float.MaxValue);
            for (int i = 0; i < d.Length; i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator - we're looking for exactly this value
                assertTrue("Failed to fill float array correctly", d[i] == float.MaxValue);
            }
        }

        /// <summary>
        /// Adapted from test_fill$FIIF() in Harmony
        /// </summary>
        [Test]
        public void TestFill_SingleArray_Int32_Int32_Single()
        {
            // Test for method void Arrays.Fill(float[], int, int, float)
            const float val = float.MaxValue;
            float[] d = new float[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator - we're looking for exactly not this value
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator - we're looking for exactly this value
                assertTrue("Failed to fill float array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$DD() in Harmony
        /// </summary>
        [Test]
        public void TestFill_DoubleArray_Double()
        {
            // Test for method void Arrays.Fill(double[], double)
            double[] d = new double[1000];
            Arrays.Fill(d, double.MaxValue);
            for (int i = 0; i < d.Length; i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator - we're looking for exactly this value
                assertTrue("Failed to fill double array correctly", d[i] == double.MaxValue);
            }
        }

        /// <summary>
        /// Adapted from test_fill$DIID() in Harmony
        /// </summary>
        [Test]
        public void TestFill_DoubleArray_Int32_Int32_Double()
        {
            // Test for method void Arrays.Fill(double[], int, int, double)
            const double val = double.MaxValue;
            double[] d = new double[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator - we're looking for exactly not this value
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator - we're looking for exactly this value
                assertTrue("Failed to fill double array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$ZZ() in Harmony
        /// </summary>
        [Test]
        public void TestFill_BoolArray_Bool()
        {
            // Test for method void Arrays.Fill(bool[], bool)
            bool[] d = new bool[1000];
            Arrays.Fill(d, true);
            for (int i = 0; i < d.Length; i++)
            {
                assertTrue("Failed to fill boolean array correctly", d[i]);
            }
        }

        /// <summary>
        /// Adapted from test_fill$ZIIZ() in Harmony
        /// </summary>
        [Test]
        public void TestFill_BoolArray_Int32_Int32_Bool()
        {
            // Test for method void Arrays.Fill(bool[], int, int, bool)
            const bool val = true;
            bool[] d = new bool[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                assertTrue("Failed to fill boolean array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$Ljava_lang_ObjectLjava_lang_Object() in Harmony
        /// </summary>
        [Test]
        public void TestFill_ObjectArray_Object()
        {
            // Test for method void Arrays.Fill(object[], object)
            object val = new object();
            object[] d = new object[1000];
            Arrays.Fill(d,
                val); // LUCENENET NOTE: the Harmony test seems to be wrong and uses the 4-parameter overload here
            for (int i = 0; i < d.Length; i++)
            {
                assertTrue("Failed to fill Object array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$Ljava_lang_ObjectIILjava_lang_Object() in Harmony
        /// </summary>
        [Test]
        public void TestFill_ObjectArray_Int32_Int32_Object()
        {
            // Test for method void Arrays.Fill(object[], int, int, object)
            object val = new object();
            object[] d = new object[1000];
            Arrays.Fill(d, 400, d.Length, val);

            for (int i = 0; i < 400; i++)
            {
                assertTrue("Filled elements not in range", d[i] != val);
            }

            for (int i = 400; i < d.Length; i++)
            {
                assertTrue("Failed to fill Object array correctly", d[i] == val);
            }

            Arrays.Fill(d, 400, d.Length, null);
            for (int i = 400; i < d.Length; i++)
            {
                assertNull("Failed to fill Object array correctly with nulls", d[i]);
            }
        }

        /// <summary>
        /// Adapted from test_equals$B$B() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_ByteArrays()
        {
            // Test for method bool Arrays.Equals(byte[], byte[])
            byte[] d = new byte[1000];
            byte[] x = new byte[1000];
            Arrays.Fill(d, byte.MaxValue);
            Arrays.Fill(x, byte.MinValue);
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, byte.MaxValue);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));
        }

        /// <summary>
        /// Adapted from test_equals$S$S() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_Int16Arrays()
        {
            // Test for method bool Arrays.Equals(short[], short[])
            short[] d = new short[1000];
            short[] x = new short[1000];
            Arrays.Fill(d, short.MaxValue);
            Arrays.Fill(x, short.MinValue);
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, short.MaxValue);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));
        }

        /// <summary>
        /// Adapted from test_equals$C$C() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_CharArrays()
        {
            // Test for method bool Arrays.Equals(char[], char[])
            char[] d = new char[1000];
            char[] x = new char[1000];
            const char c = 'T';
            Arrays.Fill(d, c);
            Arrays.Fill(x, 'L');
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, c);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));
        }

        /// <summary>
        /// Adapted from test_equals$I$I() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_Int32Arrays()
        {
            // Test for method bool Arrays.Equals(int[], int[])
            int[] d = new int[1000];
            int[] x = new int[1000];
            Arrays.Fill(d, int.MaxValue);
            Arrays.Fill(x, int.MinValue);
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, int.MaxValue);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));

            assertTrue("wrong result for null array1", !Arrays.Equals(new int[2], null));
            assertTrue("wrong result for null array2", !Arrays.Equals(null, new int[2]));
        }

        /// <summary>
        /// Adapted from test_equals$J$J() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_Int64Arrays()
        {
            // Test for method bool Arrays.Equals(long[], long[])
            long[] d = new long[1000];
            long[] x = new long[1000];
            Arrays.Fill(d, long.MaxValue);
            Arrays.Fill(x, long.MinValue);
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, long.MaxValue);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));

            assertTrue("should be false", !Arrays.Equals(new[] { 0x100000000L }, new[] { 0x200000000L }));
        }

        /// <summary>
        /// Adapted from test_equals$F$F() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_SingleArrays()
        {
            // Test for method bool Arrays.Equals(float[], float[])
            float[] d = new float[1000];
            float[] x = new float[1000];
            Arrays.Fill(d, float.MaxValue);
            Arrays.Fill(x, float.MinValue);
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, float.MaxValue);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));

            assertTrue("NaN not equals", Arrays.Equals(new[] { float.NaN }, new[] { float.NaN }));
            assertTrue("0f equals -0f", !Arrays.Equals(new[] { 0f }, new[] { -0f }));
        }

        /// <summary>
        /// Adapted from test_equals$D$D() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_DoubleArrays()
        {
            // Test for method bool Arrays.Equals(double[], double[])
            double[] d = new double[1000];
            double[] x = new double[1000];
            Arrays.Fill(d, double.MaxValue);
            Arrays.Fill(x, double.MinValue);
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, double.MaxValue);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));

            assertTrue("NaN not equals", Arrays.Equals(new[] { double.NaN }, new[] { double.NaN }));
            assertTrue("0f equals -0f", !Arrays.Equals(new[] { 0d }, new[] { -0d }));
        }

        /// <summary>
        /// Adapted from test_equals$Z$Z() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_BoolArrays()
        {
            // Test for method bool Arrays.Equals(bool[], bool[])
            bool[] d = new bool[1000];
            bool[] x = new bool[1000];
            Arrays.Fill(d, true);
            Arrays.Fill(x, false);
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, true);
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));
        }

        /// <summary>
        /// Adapted from test_equals$Ljava_lang_Object$Ljava_lang_Object() in Harmony
        /// </summary>
        [Test]
        public void TestEquals_ObjectArrays()
        {
            // Test for method bool Arrays.Equals(object[], object[])
            object?[] d = new object?[1000];
            object?[] x = new object?[1000];
            object o = new object();
            Arrays.Fill(d, o);
            Arrays.Fill(x, new object());
            assertTrue("Inequal arrays returned true", !Arrays.Equals(d, x));
            Arrays.Fill(x, o);
            d[50] = null;
            x[50] = null;
            assertTrue("equal arrays returned false", Arrays.Equals(d, x));
        }

        // LUCENENET - sort, deepEquals, deepHashCode tests omitted

        /// <summary>
        /// Adapted from test_hashCode$LZ() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_BoolArray()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            bool[] boolArr1 = { true, false, false, true, false };
            bool[] boolArr1Same = { true, false, false, true, false };
            bool[] boolArr2 = { true, false, false, true, true };
            int hashCode1 = Arrays.GetHashCode(boolArr1);
            int hashCode1Same = Arrays.GetHashCode(boolArr1Same);
            int hashCode2 = Arrays.GetHashCode(boolArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LI() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_Int32Array()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            int[] intArr1 = { 10, 5, 134, 7, 19 };
            int[] intArr1Same = { 10, 5, 134, 7, 19 };
            int[] intArr2 = { 10, 5, 134, 7, 20 };
            int hashCode1 = Arrays.GetHashCode(intArr1);
            int hashCode1Same = Arrays.GetHashCode(intArr1Same);
            int hashCode2 = Arrays.GetHashCode(intArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LC() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_CharArray()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            char[] charArr1 = { 'a', 'g', 'x', 'c', 'm' };
            char[] charArr1Same = { 'a', 'g', 'x', 'c', 'm' };
            char[] charArr2 = { 'a', 'g', 'x', 'c', 'n' };
            int hashCode1 = Arrays.GetHashCode(charArr1);
            int hashCode1Same = Arrays.GetHashCode(charArr1Same);
            int hashCode2 = Arrays.GetHashCode(charArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LB() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_ByteArray()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            byte[] byteArr1 = { 5, 9, 7, 6, 17 };
            byte[] byteArr1Same = { 5, 9, 7, 6, 17 };
            byte[] byteArr2 = { 5, 9, 7, 6, 18 };
            int hashCode1 = Arrays.GetHashCode(byteArr1);
            int hashCode1Same = Arrays.GetHashCode(byteArr1Same);
            int hashCode2 = Arrays.GetHashCode(byteArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LJ() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_Int64Array()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            long[] longArr1 = { 67890234512L, 97587236923425L, 257421912912L, 6754268100L, 5 };
            long[] longArr1Same = { 67890234512L, 97587236923425L, 257421912912L, 6754268100L, 5 };
            long[] longArr2 = { 67890234512L, 97587236923425L, 257421912912L, 6754268100L, 6 };
            int hashCode1 = Arrays.GetHashCode(longArr1);
            int hashCode1Same = Arrays.GetHashCode(longArr1Same);
            int hashCode2 = Arrays.GetHashCode(longArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LF() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_SingleArray()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            float[] floatArr1 = { 0.13497f, 0.268934f, 12e-5f, -3e+2f, 10e-4f };
            float[] floatArr1Same = { 0.13497f, 0.268934f, 12e-5f, -3e+2f, 10e-4f };
            float[] floatArr2 = { 0.13497f, 0.268934f, 12e-5f, -3e+2f, 10e-5f };
            int hashCode1 = Arrays.GetHashCode(floatArr1);
            int hashCode1Same = Arrays.GetHashCode(floatArr1Same);
            int hashCode2 = Arrays.GetHashCode(floatArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LD() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_DoubleArray()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            double[] doubleArr1 = { 0.134945657, 0.0038754, 11e-150, -30e-300, 10e-4 };
            double[] doubleArr1Same = { 0.134945657, 0.0038754, 11e-150, -30e-300, 10e-4 };
            double[] doubleArr2 = { 0.134945657, 0.0038754, 11e-150, -30e-300, 10e-5 };
            int hashCode1 = Arrays.GetHashCode(doubleArr1);
            int hashCode1Same = Arrays.GetHashCode(doubleArr1Same);
            int hashCode2 = Arrays.GetHashCode(doubleArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LS() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_Int16Array()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            short[] shortArr1 = { 35, 13, 45, 2, 91 };
            short[] shortArr1Same = { 35, 13, 45, 2, 91 };
            short[] shortArr2 = { 35, 13, 45, 2, 92 };
            int hashCode1 = Arrays.GetHashCode(shortArr1);
            int hashCode1Same = Arrays.GetHashCode(shortArr1Same);
            int hashCode2 = Arrays.GetHashCode(shortArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        /// <summary>
        /// Adapted from test_hashCode$LS() in Harmony
        /// </summary>
        [Test]
        public void TestGetHashCode_ObjectArray()
        {
            // LUCENENET NOTE - in Harmony, they are testing to make sure
            // that Arrays.hashCode returns the same as LinkedList.hashCode
            // on the same data. We can't do that here, so we are just testing
            // that the hash codes are consistent.
            object?[] objectArr1 = { 1, 10e-12f, null };
            object?[] objectArr1Same = { 1, 10e-12f, null };
            object?[] objectArr2 = { 1, 10e-12f, new object() };
            int hashCode1 = Arrays.GetHashCode(objectArr1);
            int hashCode1Same = Arrays.GetHashCode(objectArr1Same);
            int hashCode2 = Arrays.GetHashCode(objectArr2);
            Assert.AreEqual(hashCode1, hashCode1Same);
            Assert.AreNotEqual(hashCode1, hashCode2);
        }

        [Test, LuceneNetSpecific]
        public void Copy_Int32Array()
        {
            int[] source = { 1, 2, 3, 4, 5 };
            int[] dest = new int[5];
            Arrays.Copy(source, dest, source.Length);
            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2, dest[1]);
            Assert.AreEqual(3, dest[2]);
            Assert.AreEqual(4, dest[3]);
            Assert.AreEqual(5, dest[4]);
        }

        [Test, LuceneNetSpecific]
        public void Copy_ObjectArray()
        {
            object[] source = { 1, 2f, 3d, 4L, new object() };
            object[] dest = new object[5];
            Arrays.Copy(source, dest, source.Length);
            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2f, dest[1]);
            Assert.AreEqual(3d, dest[2]);
            Assert.AreEqual(4L, dest[3]);
            Assert.IsNotNull(dest[4]);
        }

        [Test, LuceneNetSpecific]
        public void Copy_Int32Array_Partial()
        {
            int[] source = { 1, 2, 3, 4, 5 };
            int[] dest = new int[3];
            Arrays.Copy(source, dest, dest.Length);
            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2, dest[1]);
            Assert.AreEqual(3, dest[2]);
        }

        [Test, LuceneNetSpecific]
        public void Copy_ObjectArray_Partial()
        {
            object[] source = { 1, 2f, 3d, 4L, new object() };
            object[] dest = new object[3];
            Arrays.Copy(source, dest, dest.Length);
            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2f, dest[1]);
            Assert.AreEqual(3d, dest[2]);
        }

        [Test, LuceneNetSpecific]
        public void Copy_Int32Array_WithIndices()
        {
            int[] source = { 1, 2, 3, 4, 5 };
            int[] dest = new int[5];
            Arrays.Copy(source, 1, dest, 2, 3);
            Assert.AreEqual(2, dest[2]);
            Assert.AreEqual(3, dest[3]);
            Assert.AreEqual(4, dest[4]);
        }

        [Test, LuceneNetSpecific]
        public void Copy_ObjectArray_WithIndices()
        {
            object[] source = { 1, 2f, 3d, 4L, new object() };
            object[] dest = new object[5];
            Arrays.Copy(source, 1, dest, 2, 3);
            Assert.AreEqual(2f, dest[2]);
            Assert.AreEqual(3d, dest[3]);
            Assert.AreEqual(4L, dest[4]);
        }

        [Test, LuceneNetSpecific]
        public void CopyOf_Int32Array()
        {
            int[] source = { 1, 2, 3, 4, 5 };

            int[] dest = Arrays.CopyOf(source, 5);

            Assert.AreNotSame(source, dest);
            Assert.AreEqual(5, dest.Length);
            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2, dest[1]);
            Assert.AreEqual(3, dest[2]);
            Assert.AreEqual(4, dest[3]);
            Assert.AreEqual(5, dest[4]);
        }

        [Test, LuceneNetSpecific]
        public void CopyOf_ObjectArray()
        {
            object[] source = { 1, 2f, 3d, 4L, new object() };

            object[] dest = Arrays.CopyOf(source, 5);

            Assert.AreNotSame(source, dest);
            Assert.AreEqual(5, dest.Length);
            Assert.AreEqual(1, dest[0]);
            Assert.AreEqual(2f, dest[1]);
            Assert.AreEqual(3d, dest[2]);
            Assert.AreEqual(4L, dest[3]);
            Assert.IsNotNull(dest[4]);
        }

        [Test, LuceneNetSpecific]
        public void CopyOfRange_Int32Array()
        {
            int[] source = { 1, 2, 3, 4, 5 };

            int[] dest = Arrays.CopyOfRange(source, 1, 4);

            Assert.AreNotSame(source, dest);
            Assert.AreEqual(3, dest.Length);
            Assert.AreEqual(2, dest[0]);
            Assert.AreEqual(3, dest[1]);
            Assert.AreEqual(4, dest[2]);
        }

        [Test, LuceneNetSpecific]
        public void CopyOfRange_ObjectArray()
        {
            object[] source = { 1, 2f, 3d, 4L, new object() };

            object[] dest = Arrays.CopyOfRange(source, 1, 4);

            Assert.AreNotSame(source, dest);
            Assert.AreEqual(3, dest.Length);
            Assert.AreEqual(2f, dest[0]);
            Assert.AreEqual(3d, dest[1]);
            Assert.AreEqual(4L, dest[2]);
        }

        [Test, LuceneNetSpecific]
        public void ToString_Int32Array()
        {
            int[] source = { 1, 2, 3, 4, 5 };
            string result = Arrays.ToString(source);
            Assert.IsTrue("[1, 2, 3, 4, 5]".Equals(result, StringComparison.Ordinal), "Strings are not equal");
        }

        [Test, LuceneNetSpecific]
        public void ToString_ObjectArray()
        {
            object?[] source = { 1, 2f, 3d, 4L, null };
            string result = Arrays.ToString(source);
            Assert.IsTrue("[1, 2.0, 3.0, 4, null]".Equals(result, StringComparison.Ordinal), "Strings are not equal");
        }
    }
}
