// Some tests from Apache Harmony:
// https://github.com/apache/harmony/blob/02970cb7227a335edd2c8457ebdde0195a735733/classlib/modules/luni/src/test/api/common/org/apache/harmony/luni/tests/java/util/ArraysTest.java

using Lucene.Net.Util;
using NUnit.Framework;

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
        public void TestFill_ByteArray_Int_Int_Byte()
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
            catch (ArrayIndexOutOfBoundsException)
            {
                result = 1;
            }
            catch (IllegalArgumentException)
            {
                result = 2;
            }

            assertEquals("Wrong exception1", 2, result);
            try
            {
                Arrays.Fill(new byte[2], -1, 1, (byte)27);
                result = 0;
            }
            catch (ArrayIndexOutOfBoundsException)
            {
                result = 1;
            }
            catch (IllegalArgumentException)
            {
                result = 2;
            }

            assertEquals("Wrong exception2", 1, result);
            try
            {
                Arrays.Fill(new byte[2], 1, 4, (byte)27);
                result = 0;
            }
            catch (ArrayIndexOutOfBoundsException e)
            {
                result = 1;
            }
            catch (IllegalArgumentException e)
            {
                result = 2;
            }

            assertEquals("Wrong exception", 1, result);
        }

        /// <summary>
        /// Adapted from test_fill$SS() in Harmony
        /// </summary>
        [Test]
        public void TestFill_ShortArray_Short()
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
        public void TestFill_ShortArray_Int_Int_Short()
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
        public void TestFill_CharArray_Int_Int_Char()
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
        public void TestFill_IntArray_Int()
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
        public void TestFill_IntArray_Int_Int_Int()
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
        public void TestFill_LongArray_Long()
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
        public void TestFill_LongArray_Int_Int_Long()
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
        public void TestFill_FloatArray_Float()
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
        public void TestFill_FloatArray_Int_Int_Float()
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
        public void TestFill_DoubleArray_Int_Int_Double()
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
        public void TestFill_BoolArray_Int_Int_Bool()
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
            Arrays.Fill(d, val); // LUCENENET NOTE: the Harmony test seems to be wrong and uses the 4-parameter overload here
            for (int i = 0; i < d.Length; i++)
            {
                assertTrue("Failed to fill Object array correctly", d[i] == val);
            }
        }

        /// <summary>
        /// Adapted from test_fill$Ljava_lang_ObjectIILjava_lang_Object() in Harmony
        /// </summary>
        [Test]
        public void TestFill_ObjectArray_Int_Int_Object()
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
    }
}
