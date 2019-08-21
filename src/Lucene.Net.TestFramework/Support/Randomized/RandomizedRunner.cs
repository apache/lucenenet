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

using Lucene.Net.Randomized.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Lucene.Net.Randomized
{
    public class RandomizedRunner
    {
        public Randomness Randomness { get; protected set; }

        private Type suiteClass;

        private static int sequence = new int();

        public RandomizedRunner(Type testClass)
        {
            this.suiteClass = testClass;

            var list = new List<ISeedDecorator>();
            var attrs = this.suiteClass.GetTypeInfo().GetCustomAttributes<SeedDecoratorAttribute>(true);
            foreach (var attr in attrs)
            {
                foreach (var decoratorType in attr.Decorators)
                {
                    var decorator = (ISeedDecorator)Activator.CreateInstance(decoratorType);
                    decorator.Initialize(testClass);
                    list.Add(decorator);
                }
            }

            int ticks = (int)System.DateTime.Now.Ticks;
            int randomSeed = MurmurHash3.Hash(NextSequence() + ticks);

            int initialSeed = randomSeed;

            this.Randomness = new Randomness(initialSeed, list.ToArray());
        }

        private static int NextSequence()
        {
            return Interlocked.Increment(ref sequence);
        }
    }
}