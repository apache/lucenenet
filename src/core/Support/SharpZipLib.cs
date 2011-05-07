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

namespace Lucene.Net.Support
{
    public class SharpZipLib
    {
        static System.Reflection.Assembly asm = null;

        static SharpZipLib()
        {
            try
            {
                asm = System.Reflection.Assembly.Load("ICSharpCode.SharpZipLib");
            }
            catch{}
        }

        public static Deflater CreateDeflater()
        {
            if (asm == null) throw new System.IO.FileNotFoundException("Can not load ICSharpCode.SharpZipLib.dll"); 
            return new Deflater(asm.CreateInstance("ICSharpCode.SharpZipLib.Zip.Compression.Deflater"));
        }

        public static Inflater CreateInflater()
        {
            if (asm == null) throw new System.IO.FileNotFoundException("Can not load ICSharpCode.SharpZipLib.dll");
            return new Inflater(asm.CreateInstance("ICSharpCode.SharpZipLib.Zip.Compression.Inflater"));
        }


        public class Inflater
        {
            delegate void SetInputDelegate(byte[] buffer);
            delegate bool GetIsFinishedDelegate();
            delegate int InflateDelegate(byte[] buffer);

            SetInputDelegate setInputMethod;
            GetIsFinishedDelegate getIsFinishedMethod;
            InflateDelegate inflateMethod;

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
            }

            public void SetInput(byte[] buffer)
            {
                setInputMethod(buffer);
            }

            public bool IsFinished
            {
                get { return getIsFinishedMethod(); }
            }

            public int Inflate(byte[] buffer)
            {
                return inflateMethod(buffer);
            }
        }


        public class Deflater 
        {
            delegate void SetLevelDelegate(int level);
            delegate void SetInputDelegate(byte[] input, int offset, int count);
            delegate void FinishDelegate();
            delegate bool GetIsFinishedDelegate();
            delegate int DeflateDelegate(byte[] output);

            SetLevelDelegate setLevelMethod;
            SetInputDelegate setInputMethod;
            FinishDelegate finishMethod;
            GetIsFinishedDelegate getIsFinishedMethod;
            DeflateDelegate deflateMethod;

            public const int BEST_COMPRESSION = 9;

            internal Deflater(object deflaterInstance)
            {
                Type type = deflaterInstance.GetType();

                setLevelMethod = (SetLevelDelegate)Delegate.CreateDelegate(
                    typeof(SetLevelDelegate),
                    deflaterInstance,
                    type.GetMethod("SetLevel", new Type[] { typeof(int) }));

                setInputMethod = (SetInputDelegate)Delegate.CreateDelegate(
                    typeof(SetInputDelegate),
                    deflaterInstance,
                    type.GetMethod("SetInput", new Type[] { typeof(byte[]), typeof(int), typeof(int) }));

                finishMethod = (FinishDelegate)Delegate.CreateDelegate(
                    typeof(FinishDelegate),
                    deflaterInstance,
                    type.GetMethod("Finish", Type.EmptyTypes));

                getIsFinishedMethod = (GetIsFinishedDelegate)Delegate.CreateDelegate(
                    typeof(GetIsFinishedDelegate),
                    deflaterInstance,
                    type.GetMethod("get_IsFinished", Type.EmptyTypes));

                deflateMethod = (DeflateDelegate)Delegate.CreateDelegate(
                    typeof(DeflateDelegate),
                    deflaterInstance,
                    type.GetMethod("Deflate", new Type[] { typeof(byte[]) }));
            }
            
            public void SetLevel(int level)
            {
                setLevelMethod(level);
            }

            public void SetInput(byte[] input, int offset, int count)
            {
                setInputMethod(input, offset, count);
            }

            public void Finish()
            {
                finishMethod();
            }

            public bool IsFinished
            {
                get { return getIsFinishedMethod(); }
            }

            public int Deflate(byte[] output)
            {
                return deflateMethod(output);
            }
        }
    }
}