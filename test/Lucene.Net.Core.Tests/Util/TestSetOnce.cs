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

namespace Lucene.Net.Util
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TestSetOnce : LuceneTestCase
    {
        [Test(JavaMethodName = "TestEmptyCtor")]
        public void Constructor()
        {
            var set = new SetOnce<int?>();
            Equal(null, set.Value);
        }

        [Test(JavaMethodName = "TestSettingCtor")]
        public void ConstructorWithValue()
        {
            var set = new SetOnce<int>(5);
            Equal(5, set.Value);

            Throws<SetOnce<int>.AlreadySetException>(() =>
            {
                set.Value = 7;
            });
        }

        [Test]
        public void TestSet()
        {
            var set = new SetOnce<int>();
            Equal(0, set.Value);
            
            set.Value = 5;
            Equal(5, set.Value);

            Throws<SetOnce<int>.AlreadySetException>(() =>
            {
                set.Value = 7;
            });
        }
       
        [Test]
        public void TestSetMultiThreaded()
        {
            // This test will always differ from the Java version 
            // because the Lucene.Net.Core project is now a PCL project.
            

            var one = new SetOnce<int>();
            Task<Result>[] tasks = new Task<Result>[10];
            Result[] results = new Result[10];
            tasks.Length.Times((i) =>
            {
                var result = new Result() {
                      Name = "Thread " + i.ToString(),
                      Value = i
                };
                results[i] = result;

                tasks[i] = new Task<Result>(() => {
                     

                    try {
                        one.Value = i;
                        result.Success = true;
                        result.Set = one;
                    } catch {
                        result.Set = one;
                        result.Success = false;
                    }
                    
                    return result;
                });
            });

            foreach (var task in tasks)
                task.Start();

            var final = Task.WhenAll(tasks).Result; 

            var successes = final.Where(o => o.Success).Count();

            Ok(final.Count() == 10, "There should be 10 results.");
            Ok(successes == 1, "There should be only one result that is a success.");

            foreach(var task in tasks)
            {
                if(task.Result != null && task.Result.Success)
                {
                    var expected = results[task.Result.Value];
                    Equal(expected.Value, task.Result.Set.Value);
                }
            }
        }

        public class Result
        {
            public string Name { get; set; }
            public int Value { get; set; }

            public bool Success { get; set; }

            public SetOnce<int> Set { get; set; }
        }
    }
}
