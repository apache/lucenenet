using Lucene.Net.Support.Threading;
using System;
using System.Threading;

namespace Lucene.Net.Store
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

    /// <summary>
    /// Takes a while to open files: gives TestThreadInterruptDeadlock
    /// a chance to find file leaks if opening an input throws exception
    /// </summary>
    internal class SlowOpeningMockIndexInputWrapper : MockIndexInputWrapper
    {
        public SlowOpeningMockIndexInputWrapper(MockDirectoryWrapper dir, string name, IndexInput @delegate)
            : base(dir, name, @delegate)
        {
            try
            {
                Thread.Sleep(50);
            }
            catch (Exception ie) when (ie.IsInterruptedException())
            {
                try
                {
                    base.Dispose();
                } // we didnt open successfully
                catch (Exception ignore) when (ignore.IsThrowable())
                {
                }
                throw new Util.ThreadInterruptedException(ie);
            }
        }
    }
}