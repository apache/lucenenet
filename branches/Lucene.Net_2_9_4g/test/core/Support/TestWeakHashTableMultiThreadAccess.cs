/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Threading;
using NUnit.Framework;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestWeakHashTableMultiThreadAccess
    {
        Support.WeakHashTable wht = new Support.WeakHashTable();
        Exception AnyException = null;
        bool EndOfTest = false;

        [Test]
        public void Test()
        {
            CreateThread(Add);
            CreateThread(Enum);
            
            int count = 200;
            while (count-- > 0)
            {
                Thread.Sleep(50);
                if (AnyException != null)
                {
                    EndOfTest = true;
                    Thread.Sleep(50);
                    Assert.Fail(AnyException.Message);
                }
            }
        }

        void CreateThread(ThreadStart fxn)
        {
            Thread t = new Thread(fxn);
            t.IsBackground = true;
            t.Start();
        }
        

        void Add()
        {
            try
            {
                long count = 0;
                while (EndOfTest==false)
                {
                    wht.Add(count.ToString(), count.ToString());
                    Thread.Sleep(1);

                    string toReplace = (count - 10).ToString();
                    if (wht.Contains(toReplace))
                    {
                        wht[toReplace] = "aa";
                    }

                    count++;
                }
            }
            catch (Exception ex)
            {
                AnyException = ex;
            }
        }

        void Enum()
        {
            try
            {
                while (EndOfTest==false)
                {
                    System.Collections.IEnumerator e = wht.Keys.GetEnumerator();
                    while (e.MoveNext())
                    {
                        string s = (string)e.Current;
                    }
                    Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                AnyException = ex;
            }
        }
    }
}