using System;
using System.IO;
using System.Runtime.CompilerServices;
#if FEATURE_CODE_ACCESS_SECURITY
using System.Security.Permissions;
#endif

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

    /// <summary>
    /// Mimics <see cref="System.Console"/>, but allows for swapping
    /// the <see cref="TextWriter"/> of 
    /// <see cref="Out"/> and <see cref="Error"/>, or the <see cref="TextReader"/> of <see cref="In"/>
    /// with user-defined implementations.
    /// </summary>
    public static class SystemConsole
    {
        public static TextWriter Out { get; set; } = Console.Out;
        public static TextWriter Error { get; set; } = Console.Error;
        public static TextReader In { get; set; } = Console.In;

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(bool value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(char value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(char[] buffer)
        {
            Out.Write(buffer);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(decimal value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(double value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(int value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(long value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(object value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(float value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(string value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining), CLSCompliant(false)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(uint value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining), CLSCompliant(false)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(ulong value)
        {
            Out.Write(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(string format, object arg0)
        {
            Out.Write(format, arg0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(string format, params object[] arg)
        {
            if (arg is null)
            {
                Out.Write(format, null, null);
            }
            else
            {
                Out.Write(format, arg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(char[] buffer, int index, int count)
        {
            Out.Write(buffer, index, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(string format, object arg0, object arg1)
        {
            Out.Write(format, arg0, arg1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(string format, object arg0, object arg1, object arg2)
        {
            Out.Write(format, arg0, arg1, arg2);
        }

#if FEATURE_ARGITERATOR
        [MethodImpl(MethodImplOptions.NoInlining), CLSCompliant(false)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void Write(string format, object arg0, object arg1, object arg2, object arg3, __arglist)
        {
            ArgIterator iterator = new ArgIterator(__arglist);
            int num = iterator.GetRemainingCount() + 4;
            object[] arg = new object[num];
            arg[0] = arg0;
            arg[1] = arg1;
            arg[2] = arg2;
            arg[3] = arg3;
            for (int i = 4; i < num; i++)
            {
                arg[i] = TypedReference.ToObject(iterator.GetNextArg());
            }
            Out.Write(format, arg);
        }
#endif

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine()
        {
            Out.WriteLine();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(bool value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(char value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(char[] buffer)
        {
            Out.WriteLine(buffer);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(decimal value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(double value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(int value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(long value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(object value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(float value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(string value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining), CLSCompliant(false)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(uint value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining), CLSCompliant(false)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(ulong value)
        {
            Out.WriteLine(value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(string format, object arg0)
        {
            Out.WriteLine(format, arg0);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(string format, params object[] arg)
        {
            if (arg is null)
            {
                Out.WriteLine(format, null, null);
            }
            else
            {
                Out.WriteLine(format, arg);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(char[] buffer, int index, int count)
        {
            Out.WriteLine(buffer, index, count);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(string format, object arg0, object arg1)
        {
            Out.WriteLine(format, arg0, arg1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(string format, object arg0, object arg1, object arg2)
        {
            Out.WriteLine(format, arg0, arg1, arg2);
        }

#if FEATURE_ARGITERATOR
        [MethodImpl(MethodImplOptions.NoInlining), CLSCompliant(false)]
#if FEATURE_CODE_ACCESS_SECURITY
        [HostProtection(SecurityAction.LinkDemand, UI = true)]
#endif
        public static void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, __arglist)
        {
            ArgIterator iterator = new ArgIterator(__arglist);
            int num = iterator.GetRemainingCount() + 4;
            object[] arg = new object[num];
            arg[0] = arg0;
            arg[1] = arg1;
            arg[2] = arg2;
            arg[3] = arg3;
            for (int i = 4; i < num; i++)
            {
                arg[i] = TypedReference.ToObject(iterator.GetNextArg());
            }
            Out.WriteLine(format, arg);
        }
#endif
    }
}
