using System.Reflection;

namespace Lucene.Net.Index
{
    using NUnit.Framework;

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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    public class TestNoMergePolicy : LuceneTestCase
    {
        [Test]
        public virtual void TestNoMergePolicy_Mem()
        {
            MergePolicy mp = NoMergePolicy.NO_COMPOUND_FILES;
            Assert.IsNull(mp.FindMerges(null, (SegmentInfos)null));
            Assert.IsNull(mp.FindForcedMerges(null, 0, null));
            Assert.IsNull(mp.FindForcedDeletesMerges(null));
            Assert.IsFalse(mp.UseCompoundFile(null, null));
            mp.Dispose();
        }

        [Test]
        public virtual void TestCompoundFiles()
        {
            Assert.IsFalse(NoMergePolicy.NO_COMPOUND_FILES.UseCompoundFile(null, null));
            Assert.IsTrue(NoMergePolicy.COMPOUND_FILES.UseCompoundFile(null, null));
        }

        //LUCENE TODO: Compilation problems
        /*[Test]
        public virtual void TestFinalSingleton()
	    {
		    Assert.IsTrue(Modifier.isFinal(typeof(NoMergePolicy).Modifiers));
		    Constructor<?>[] ctors = typeof(NoMergePolicy).DeclaredConstructors;
		    Assert.AreEqual("expected 1 private ctor only: " + Arrays.ToString(ctors), 1, ctors.Length);
		    Assert.IsTrue("that 1 should be private: " + ctors[0], Modifier.isPrivate(ctors[0].Modifiers));
	    }*/

        [Test]
        public virtual void TestMethodsOverridden()
        {
            // Ensures that all methods of MergePolicy are overridden. That's important
            // to ensure that NoMergePolicy overrides everything, so that no unexpected
            // behavior/error occurs
            foreach (MethodInfo m in typeof(NoMergePolicy).GetMethods())
            {
                // getDeclaredMethods() returns just those methods that are declared on
                // NoMergePolicy. getMethods() returns those that are visible in that
                // context, including ones from Object. So just filter out Object. If in
                // the future MergePolicy will extend a different class than Object, this
                // will need to change.
                if (m.Name.Equals("Clone"))
                {
                    continue;
                }
                if (m.DeclaringType != typeof(object) && !m.IsFinal)
                {
                    Assert.IsTrue(m.DeclaringType == typeof(NoMergePolicy), m + " is not overridden ! Declaring Type: " + m.DeclaringType);
                }
            }
        }
    }
}