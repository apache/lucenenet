/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Linq;

namespace Lucene.Net.Support
{
    public class Inflater
    {
        private delegate void SetInputDelegate(byte[] buffer);

        private delegate bool GetIsFinishedDelegate();

        private delegate int InflateDelegate(byte[] buffer);

        private delegate void ResetDelegate();

        private delegate void SetInputDelegate3(byte[] buffer, int index, int count);

        private delegate int InflateDelegate3(byte[] buffer, int offset, int count);

        private SetInputDelegate setInputMethod;
        private GetIsFinishedDelegate getIsFinishedMethod;
        private InflateDelegate inflateMethod;
        private ResetDelegate resetMethod;
        private SetInputDelegate3 setInput3Method;
        private InflateDelegate3 inflate3Method;

        internal Inflater(object inflaterInstance)
        {
            Type type = inflaterInstance.GetType();

            setInputMethod = (SetInputDelegate)Delegate.CreateDelegate(
                typeof(SetInputDelegate),
                inflaterInstance,
                type.GetMethod("SetInput", new Type[] { typeof(byte[]) }));

            getIsFinishedMethod = (GetIsFinishedDelegate)Delegate.CreateDelegate(
                typeof(GetIsFinishedDelegate),
                inflaterInstance,
                type.GetMethod("get_IsFinished", Type.EmptyTypes));

            inflateMethod = (InflateDelegate)Delegate.CreateDelegate(
                typeof(InflateDelegate),
                inflaterInstance,
                type.GetMethod("Inflate", new Type[] { typeof(byte[]) }));

            resetMethod = (ResetDelegate)Delegate.CreateDelegate(
                typeof(ResetDelegate),
                inflaterInstance,
                type.GetMethod("Reset", Type.EmptyTypes));

            setInput3Method = (SetInputDelegate3)Delegate.CreateDelegate(
                typeof(SetInputDelegate3),
                inflaterInstance,
                type.GetMethod("SetInput", new Type[] { typeof(byte[]), typeof(int), typeof(int) }));

            inflate3Method = (InflateDelegate3)Delegate.CreateDelegate(
                typeof(InflateDelegate3),
                inflaterInstance,
                type.GetMethod("Inflate", new Type[] { typeof(byte[]), typeof(int), typeof(int) }));
        }

        public void SetInput(byte[] buffer)
        {
            setInputMethod(buffer);
        }

        public void SetInput(byte[] buffer, int index, int count)
        {
            setInput3Method(buffer, index, count);
        }

        public bool IsFinished
        {
            get { return getIsFinishedMethod(); }
        }

        public int Inflate(byte[] buffer)
        {
            return inflateMethod(buffer);
        }

        public int Inflate(byte[] buffer, int offset, int count)
        {
            return inflate3Method(buffer, offset, count);
        }

        public void Reset()
        {
            resetMethod();
        }
    }
}