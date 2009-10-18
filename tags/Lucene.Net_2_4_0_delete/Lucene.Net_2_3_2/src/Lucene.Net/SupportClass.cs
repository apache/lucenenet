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
using System.Collections;

/// <summary>
/// This interface should be implemented by any class whose instances are intended 
/// to be executed by a thread.
/// </summary>
public interface IThreadRunnable
{
	/// <summary>
	/// This method has to be implemented in order that starting of the thread causes the object's 
	/// run method to be called in that separately executing thread.
	/// </summary>
	void Run();
}

/// <summary>
/// Contains conversion support elements such as classes, interfaces and static methods.
/// </summary>
public class SupportClass
{
    /// <summary>
    /// Support class used to handle threads
    /// </summary>
    public class ThreadClass : IThreadRunnable
    {
        /// <summary>
        /// The instance of System.Threading.Thread
        /// </summary>
        private System.Threading.Thread threadField;


        /// <summary>
        /// Initializes a new instance of the ThreadClass class
        /// </summary>
        public ThreadClass()
        {
            threadField = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
            This = this;
        }

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="Name">The name of the thread</param>
        public ThreadClass(System.String Name)
        {
            threadField = new System.Threading.Thread(new System.Threading.ThreadStart(Run));
            this.Name = Name;
            This = this;
        }

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="Start">A ThreadStart delegate that references the methods to be invoked when this thread begins executing</param>
        public ThreadClass(System.Threading.ThreadStart Start)
        {
            threadField = new System.Threading.Thread(Start);
            This = this;
        }

        /// <summary>
        /// Initializes a new instance of the Thread class.
        /// </summary>
        /// <param name="Start">A ThreadStart delegate that references the methods to be invoked when this thread begins executing</param>
        /// <param name="Name">The name of the thread</param>
        public ThreadClass(System.Threading.ThreadStart Start, System.String Name)
        {
            threadField = new System.Threading.Thread(Start);
            this.Name = Name;
            This = this;
        }

        /// <summary>
        /// This method has no functionality unless the method is overridden
        /// </summary>
        public virtual void Run()
        {
        }

        /// <summary>
        /// Causes the operating system to change the state of the current thread instance to ThreadState.Running
        /// </summary>
        public virtual void Start()
        {
            threadField.Start();
            if (This == null)
            {
                This = this;
                This.Instance = threadField;
            }
        }

        /// <summary>
        /// Interrupts a thread that is in the WaitSleepJoin thread state
        /// </summary>
        public virtual void Interrupt()
        {
            threadField.Interrupt();
        }

        /// <summary>
        /// Gets the current thread instance
        /// </summary>
        public System.Threading.Thread Instance
        {
            get
            {
                return threadField;
            }
            set
            {
                threadField = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the thread
        /// </summary>
        public System.String Name
        {
            get
            {
                return threadField.Name;
            }
            set
            {
                if (threadField.Name == null)
                    threadField.Name = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the scheduling priority of a thread
        /// </summary>
        public System.Threading.ThreadPriority Priority
        {
            get
            {
                try
                {
                    return threadField.Priority;
                }
                catch
                {
                    return System.Threading.ThreadPriority.Normal;
                }
            }
            set
            {
                try
                {
                    threadField.Priority = value;
                }
                catch{}
                
            }
        }

        /// <summary>
        /// Gets a value indicating the execution status of the current thread
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return threadField.IsAlive;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not a thread is a background thread.
        /// </summary>
        public bool IsBackground
        {
            get
            {
                return threadField.IsBackground;
            }
            set
            {
                threadField.IsBackground = value;
            }
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates
        /// </summary>
        public void Join()
        {
            threadField.Join();
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses
        /// </summary>
        /// <param name="MiliSeconds">Time of wait in milliseconds</param>
        public void Join(long MiliSeconds)
        {
            lock (this)
            {
                threadField.Join(new System.TimeSpan(MiliSeconds * 10000));
            }
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses
        /// </summary>
        /// <param name="MiliSeconds">Time of wait in milliseconds</param>
        /// <param name="NanoSeconds">Time of wait in nanoseconds</param>
        public void Join(long MiliSeconds, int NanoSeconds)
        {
            lock (this)
            {
                threadField.Join(new System.TimeSpan(MiliSeconds * 10000 + NanoSeconds * 100));
            }
        }

        /// <summary>
        /// Resumes a thread that has been suspended
        /// </summary>
        public void Resume()
        {
            System.Threading.Monitor.PulseAll(threadField);
        }

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked, 
        /// to begin the process of terminating the thread. Calling this method 
        /// usually terminates the thread
        /// </summary>
        public void Abort()
        {
            threadField.Abort();
        }

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked, 
        /// to begin the process of terminating the thread while also providing
        /// exception information about the thread termination. 
        /// Calling this method usually terminates the thread.
        /// </summary>
        /// <param name="stateInfo">An object that contains application-specific information, such as state, which can be used by the thread being aborted</param>
        public void Abort(System.Object stateInfo)
        {
            lock (this)
            {
                threadField.Abort(stateInfo);
            }
        }

        /// <summary>
        /// Suspends the thread, if the thread is already suspended it has no effect
        /// </summary>
        public void Suspend()
        {
            System.Threading.Monitor.Wait(threadField);
        }

        /// <summary>
        /// Obtain a String that represents the current Object
        /// </summary>
        /// <returns>A String that represents the current Object</returns>
        public override System.String ToString()
        {
            return "Thread[" + Name + "," + Priority.ToString() + "," + "" + "]";
        }

        [ThreadStatic]
        static ThreadClass This = null;

        /// <summary>
        /// Gets the currently running thread
        /// </summary>
        /// <returns>The currently running thread</returns>
        public static ThreadClass Current()
        {
            if (This == null)
            {
                This = new ThreadClass();
                This.Instance = System.Threading.Thread.CurrentThread;
            }
            return This;
        }
    }

    /// <summary>
    /// Represents the methods to support some operations over files.
    /// </summary>
    public class FileSupport
    {
        /// <summary>
        /// Returns an array of abstract pathnames representing the files and directories of the specified path.
        /// </summary>
        /// <param name="path">The abstract pathname to list it childs.</param>
        /// <returns>An array of abstract pathnames childs of the path specified or null if the path is not a directory</returns>
        public static System.IO.FileInfo[] GetFiles(System.IO.FileInfo path)
        {
            if ((path.Attributes & System.IO.FileAttributes.Directory) > 0)
            {																 
                String[] fullpathnames = System.IO.Directory.GetFileSystemEntries(path.FullName);
                System.IO.FileInfo[] result = new System.IO.FileInfo[fullpathnames.Length];
                for (int i = 0; i < result.Length ; i++)
                    result[i] = new System.IO.FileInfo(fullpathnames[i]);
                return result;
            }
            else
                return null;
        }

        /// <summary>
        /// Returns a list of files in a give directory.
        /// </summary>
        /// <param name="fullName">The full path name to the directory.</param>
        /// <param name="indexFileNameFilter"></param>
        /// <returns>An array containing the files.</returns>
        public static System.String[] GetLuceneIndexFiles(System.String fullName, 
                                                          Lucene.Net.Index.IndexFileNameFilter indexFileNameFilter)
        {
            System.IO.DirectoryInfo dInfo = new System.IO.DirectoryInfo(fullName);
            System.Collections.ArrayList list = new System.Collections.ArrayList();
            foreach (System.IO.FileInfo fInfo in dInfo.GetFiles())
            {
                if (indexFileNameFilter.Accept(fInfo, fInfo.Name) == true)
                {
                    list.Add(fInfo.Name);
                }
            }
            System.String[] retFiles = new System.String[list.Count];
            list.CopyTo(retFiles);
            return retFiles;
        }
    }

    /// <summary>
    /// A simple class for number conversions.
    /// </summary>
    public class Number
    {
        /// <summary>
        /// Min radix value.
        /// </summary>
        public const int MIN_RADIX = 2;
        /// <summary>
        /// Max radix value.
        /// </summary>
        public const int MAX_RADIX = 36;

        private const System.String digits = "0123456789abcdefghijklmnopqrstuvwxyz";


        /// <summary>
        /// Converts a number to System.String.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static System.String ToString(long number)
        {
            System.Text.StringBuilder s = new System.Text.StringBuilder();

            if (number == 0)
            {
                s.Append("0");
            }
            else
            {
                if (number < 0)
                {
                    s.Append("-");
                    number = -number;
                }

                while (number > 0)
                {
                    char c = digits[(int)number % 36];
                    s.Insert(0, c);
                    number = number / 36;
                }
            }

            return s.ToString();
        }


        /// <summary>
        /// Converts a number to System.String.
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static System.String ToString(float f)
        {
            if (((float)(int)f) == f)
            {
                return ((int)f).ToString() + ".0";
            }
            else
            {
                return f.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
            }
        }

        /// <summary>
        /// Converts a number to System.String in the specified radix.
        /// </summary>
        /// <param name="i">A number to be converted.</param>
        /// <param name="radix">A radix.</param>
        /// <returns>A System.String representation of the number in the specified redix.</returns>
        public static System.String ToString(long i, int radix)
        {
            if (radix < MIN_RADIX || radix > MAX_RADIX)
                radix = 10;

            char[] buf = new char[65];
            int charPos = 64;
            bool negative = (i < 0);

            if (!negative) 
            {
                i = -i;
            }

            while (i <= -radix) 
            {
                buf[charPos--] = digits[(int)(-(i % radix))];
                i = i / radix;
            }
            buf[charPos] = digits[(int)(-i)];

            if (negative) 
            {
                buf[--charPos] = '-';
            }

            return new System.String(buf, charPos, (65 - charPos)); 
        }

        /// <summary>
        /// Parses a number in the specified radix.
        /// </summary>
        /// <param name="s">An input System.String.</param>
        /// <param name="radix">A radix.</param>
        /// <returns>The parsed number in the specified radix.</returns>
        public static long Parse(System.String s, int radix)
        {
            if (s == null) 
            {
                throw new ArgumentException("null");
            }

            if (radix < MIN_RADIX) 
            {
                throw new NotSupportedException("radix " + radix +
                    " less than Number.MIN_RADIX");
            }
            if (radix > MAX_RADIX) 
            {
                throw new NotSupportedException("radix " + radix +
                    " greater than Number.MAX_RADIX");
            }

            long result = 0;
            long mult = 1;

            s = s.ToLower();
			
            for (int i = s.Length - 1; i >= 0; i--)
            {
                int weight = digits.IndexOf(s[i]);
                if (weight == -1)
                    throw new FormatException("Invalid number for the specified radix");

                result += (weight * mult);
                mult *= radix;
            }

            return result;
        }

        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        public static int URShift(int number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            else
                return (number >> bits) + (2 << ~bits);
        }


        /// <summary>
        /// Performs an unsigned bitwise right shift with the specified number
        /// </summary>
        /// <param name="number">Number to operate on</param>
        /// <param name="bits">Ammount of bits to shift</param>
        /// <returns>The resulting number from the shift operation</returns>
        public static long URShift(long number, int bits)
        {
            if (number >= 0)
                return number >> bits;
            else
                return (number >> bits) + (2 << ~bits);
        }


        /// <summary>
        /// Returns the index of the first bit that is set to true that occurs 
        /// on or after the specified starting index. If no such bit exists 
        /// then -1 is returned.
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        /// <param name="fromIndex">The index to start checking from (inclusive).</param>
        /// <returns>The index of the next set bit.</returns>
        public static int NextSetBit(System.Collections.BitArray bits, int fromIndex)
        {
            for (int i = fromIndex; i < bits.Length; i++)
            {
                if (bits[i] == true)
                {
                    return i;
                }
            }
            return -1;
        }


        /// <summary>
        /// Returns the number of bits set to true in this BitSet.
        /// </summary>
        /// <param name="bits">The BitArray object.</param>
        /// <returns>The number of bits set to true in this BitSet.</returns>
        public static int Cardinality(System.Collections.BitArray bits)
        {
            int count = 0;
            for (int i = 0; i < bits.Count; i++)
            {
                if (bits[i] == true)
                    count++;
            }
            return count;
        }

        
        /// <summary>
        /// Converts a System.String number to long.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static long ToInt64(System.String s)
        {
            long number = 0;
            int factor;

            // handle negative number
            if (s.StartsWith("-"))
            {
                s = s.Substring(1);
                factor = -1;
            }
            else
            {
                factor = 1;
            }

            // generate number
            for (int i = s.Length - 1; i > -1; i--)
            {
                int n = digits.IndexOf(s[i]);

                // not supporting fractional or scientific notations
                if (n < 0)
                    throw new System.ArgumentException("Invalid or unsupported character in number: " + s[i]);

                number += (n * factor);
                factor *= 36;
            }

            return number;
        }
    }

    /// <summary>
    /// Mimics Java's Character class.
    /// </summary>
    public class Character
    {
        private const char charNull= '\0';
        private const char charZero = '0';
        private const char charA = 'a';

        /// <summary>
        /// </summary>
        public static int MAX_RADIX
        {
            get
            {
                return 36;
            }
        }

        /// <summary>
        /// </summary>
        public static int MIN_RADIX
        {
            get
            {
                return 2;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="digit"></param>
        /// <param name="radix"></param>
        /// <returns></returns>
        public static char ForDigit(int digit, int radix)
        {
            // if radix or digit is out of range,
            // return the null character.
            if (radix < Character.MIN_RADIX)
                return charNull;
            if (radix > Character.MAX_RADIX)
                return charNull;
            if (digit < 0)
                return charNull;
            if (digit >= radix)
                return charNull;

            // if digit is less than 10,
            // return '0' plus digit
            if (digit < 10)
                return (char) ( (int) charZero + digit);

            // otherwise, return 'a' plus digit.
            return (char) ((int) charA + digit - 10);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Date
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        static public long GetTime(DateTime dateTime)
        {
            TimeSpan ts = dateTime.Subtract(new DateTime(1970, 1, 1));
            ts = ts.Subtract(TimeZone.CurrentTimeZone.GetUtcOffset(dateTime));
            return ts.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class Single
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="style"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s, System.Globalization.NumberStyles style, System.IFormatProvider provider)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1), style, provider);
                else
                    return System.Single.Parse(s, style, provider);
            }
            catch (System.FormatException fex)
            {
                throw fex;					
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s, System.IFormatProvider provider)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1), provider);
                else
                    return System.Single.Parse(s, provider);
            }
            catch (System.FormatException fex)
            {
                throw fex;					
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s, System.Globalization.NumberStyles style)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1), style);
                else
                    return System.Single.Parse(s, style);
            }
            catch(System.FormatException fex)
            {
                throw fex;					
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static System.Single Parse(System.String s)
        {
            try
            {
                if (s.EndsWith("f") || s.EndsWith("F"))
                    return System.Single.Parse(s.Substring(0, s.Length - 1).Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
                else
                    return System.Single.Parse(s.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator));
            }
            catch(System.FormatException fex)
            {
                throw fex;					
            }
        }

        public static bool TryParse(System.String s, out float f)
        {
            bool ok = false;
            
            if (s.EndsWith("f") || s.EndsWith("F"))
                ok=System.Single.TryParse(s.Substring(0, s.Length - 1).Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator),out f);
            else
                ok=System.Single.TryParse(s.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator),out f);

            return ok;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <returns></returns>
        public static string ToString(float f)
        {
            return f.ToString().Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="f"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToString(float f, string format)
        {
            return f.ToString(format).Replace(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, ".");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class AppSettings
    {
        static System.Collections.Specialized.ListDictionary settings = new System.Collections.Specialized.ListDictionary();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        public static void Set(System.String key, int defValue)
        {
            settings[key] = defValue;
            //System.Configuration.ConfigurationManager.AppSettings.Set(key, defValue.ToString()); // {{Aroush-2.3.1}} try this instead
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        public static void Set(System.String key, long defValue)
        {
            settings[key] = defValue;
            //System.Configuration.ConfigurationManager.AppSettings.Set(key, defValue.ToString()); // {{Aroush-2.3.1}} try this instead
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public static void Set(System.String key, System.String defValue)
        {
            settings[key] = defValue;
            //System.Configuration.ConfigurationManager.AppSettings.Set(key, defValue); // {{Aroush-2.3.1}} try this instead
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public static int Get(System.String key, int defValue)
        {
            if (settings[key] != null)
            {
                return (int) settings[key];
            }

            System.String theValue = System.Configuration.ConfigurationManager.AppSettings.Get(key);
            if (theValue == null)
            {
                return defValue;
            }
            return System.Convert.ToInt16(theValue.Trim());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public static long Get(System.String key, long defValue)
        {
            if (settings[key] != null)
            {
                return (long) settings[key];
            }

            System.String theValue = System.Configuration.ConfigurationManager.AppSettings.Get(key);
            if (theValue == null)
            {
                return defValue;
            }
            return System.Convert.ToInt32(theValue.Trim());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defValue"></param>
        /// <returns></returns>
        public static System.String Get(System.String key, System.String defValue)
        {
            if (settings[key] != null)
            {
                return (System.String) settings[key];
            }

            System.String theValue = System.Configuration.ConfigurationManager.AppSettings.Get(key);
            if (theValue == null)
            {
                return defValue;
            }
            return theValue;
        }
    }

    public static System.Collections.SortedList TailMap(System.Collections.SortedList list, System.Object limit)
    {
        System.Collections.Comparer comparer = System.Collections.Comparer.Default;
        System.Collections.SortedList newList = new System.Collections.SortedList();

        if (list != null)
        {
            if (list.Count > 0)
            {
                int index = 0;
                while (comparer.Compare(list.GetKey(index), limit) < 0)
                    index++;

                for (; index < list.Count; index++)
                    newList.Add(list.GetKey(index), list[list.GetKey(index)]);
            }
        }

        return newList;
    }

    /// <summary>
    /// Summary description for TestSupportClass.
    /// </summary>
    public class Compare
    {
        /// <summary>
        /// Compares two Term arrays for equality.
        /// </summary>
        /// <param name="t1">First Term array to compare</param>
        /// <param name="t2">Second Term array to compare</param>
        /// <returns>true if the Terms are equal in both arrays, false otherwise</returns>
        public static bool CompareTermArrays(Lucene.Net.Index.Term[] t1, Lucene.Net.Index.Term[] t2)
        {
            if (t1.Length != t2.Length)
                return false;
            for (int i = 0; i < t1.Length; i++)
            {
                if (t1[i].CompareTo(t2[i]) == 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compares two string arrays for equality.
        /// </summary>
        /// <param name="l1">First string array list to compare</param>
        /// <param name="l2">Second string array list to compare</param>
        /// <returns>true if the strings are equal in both arrays, false otherwise</returns>
        public static bool CompareStringArrays(System.String[] l1, System.String[] l2)
        {
            if (l1.Length != l2.Length)
                return false;
            for (int i = 0; i < l1.Length; i++)
            {
                if (l1[i] != l2[i])
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Use for .NET 1.1 Framework only.
    /// </summary>
    public class CompressionSupport
    {
        public interface ICompressionAdapter
        {
            byte[] Compress(byte[] input);
            byte[] Uncompress(byte[] input);
        }

#if SHARP_ZIP_LIB
        private static ICompressionAdapter compressionAdapter = new Lucene.Net.Index.Compression.SharpZipLibAdapter();
#else
        private static ICompressionAdapter compressionAdapter;
#endif

        public static byte[] Uncompress(byte[] input)
        {
            CheckCompressionSupport();
            return compressionAdapter.Uncompress(input);
        }

        public static byte[] Compress(byte[] input)
        {
            CheckCompressionSupport();
            return compressionAdapter.Compress(input);
        }

        private static void CheckCompressionSupport()
        {
            if (compressionAdapter == null)
            {
                System.String compressionLibClassName = SupportClass.AppSettings.Get("Lucene.Net.CompressionLib.class", null);
                if (compressionLibClassName == null)
                    throw new System.SystemException("Compression support not configured"); 

                Type compressionLibClass = Type.GetType(compressionLibClassName, true);
                System.Object compressionAdapterObj = Activator.CreateInstance(compressionLibClass);
                compressionAdapter = compressionAdapterObj as ICompressionAdapter;
                if (compressionAdapter == null)
                    throw new System.SystemException("Compression adapter does not support the ICompressionAdapter interface");
            }
        }
    }

    #region WEAKHASHTABLE
    /// <summary>
    /// A Hashtable which holds weak references to its keys so they
    /// can be collected during GC. 
    /// </summary>
    [System.Diagnostics.DebuggerDisplay("Count = {Values.Count}")]
    public class WeakHashTable : Hashtable, IEnumerable
    {
        /// <summary>
        /// A weak referene wrapper for the hashtable keys. Whenever a key\value pair 
        /// is added to the hashtable, the key is wrapped using a WeakKey. WeakKey saves the
        /// value of the original object hashcode for fast comparison.
        /// </summary>
        class WeakKey : WeakReference
        {
            int hashCode;

            public WeakKey(object key)
                : base(key)
            {
                if (key == null)
                    throw new ArgumentNullException("key");

                hashCode = key.GetHashCode();
            }

            public override int GetHashCode()
            {
                return hashCode;
            }
        }

        /// <summary>
        /// A Dictionary enumerator which wraps the original hashtable enumerator 
        /// and performs 2 tasks: Extract the real key from a WeakKey and skip keys
        /// that were already collected.
        /// </summary>
        class WeakDictionaryEnumerator : IDictionaryEnumerator
        {
            IDictionaryEnumerator baseEnumerator;
            object currentKey;
            object currentValue;

            public WeakDictionaryEnumerator(IDictionaryEnumerator baseEnumerator)
            {
                this.baseEnumerator = baseEnumerator;
            }

            public DictionaryEntry Entry
            {
                get
                {
                    return new DictionaryEntry(this.currentKey, this.currentValue);
                }
            }

            public object Key
            {
                get
                {
                    return this.currentKey;
                }
            }

            public object Value
            {
                get
                {
                    return this.currentValue;
                }
            }

            public object Current
            {
                get
                {
                    return Entry;
                }
            }

            public bool MoveNext()
            {
                while (baseEnumerator.MoveNext())
                {
                    object key = ((WeakKey)baseEnumerator.Key).Target;
                    if (key != null)
                    {
                        this.currentKey = key;
                        this.currentValue = baseEnumerator.Value;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                baseEnumerator.Reset();
                this.currentKey = null;
                this.currentValue = null;
            }
        }


        /// <summary>
        /// Serves as a simple "GC Monitor" that indicates whether cleanup is needed. 
        /// If collectableObject.IsAlive is false, GC has occurred and we should perform cleanup
        /// </summary>
        WeakReference collectableObject = new WeakReference(new Object());

        /// <summary>
        /// Customize the hashtable lookup process by overriding KeyEquals. KeyEquals
        /// will compare both WeakKey to WeakKey and WeakKey to real keys
        /// </summary>
        protected override bool KeyEquals(object x, object y)
        {
            if (x == y)
                return true;

            if (x is WeakKey)
            {
                x = ((WeakKey)x).Target;
                if (x == null)
                    return false;
            }

            if (y is WeakKey)
            {
                y = ((WeakKey)y).Target;
                if (y == null)
                    return false;
            }

            return x.Equals(y);
        }

        protected override int GetHash(object key)
        {
            return key.GetHashCode();
        }

        /// <summary>
        /// Perform cleanup if GC occurred
        /// </summary>
        private void CleanIfNeeded()
        {
            if (collectableObject.Target == null)
            {
                Clean();
                collectableObject = new WeakReference(new Object());
            }
        }

        /// <summary>
        /// Iterate over all keys and remove keys that were collected
        /// </summary>
        private void Clean()
        {
            ArrayList keysToDelete = new ArrayList();
            foreach (WeakKey wtk in base.Keys)
            {
                if (!wtk.IsAlive)
                {
                    keysToDelete.Add(wtk);
                }
            }

            foreach (WeakKey wtk in keysToDelete)
                Remove(wtk);
        }


        /// <summary>
        /// Wrap each key with a WeakKey and add it to the hashtable
        /// </summary>
        public override void Add(object key, object value)
        {
            CleanIfNeeded();
            base.Add(new WeakKey(key), value);
        }

        public override IDictionaryEnumerator GetEnumerator()
        {
            return new WeakDictionaryEnumerator(base.GetEnumerator());
        }

        /// <summary>
        /// Create a temporary copy of the real keys and return that
        /// </summary>
        public override ICollection Keys
        {
            get
            {
                ArrayList keys = new ArrayList(Count);
                foreach (WeakKey key in base.Keys)
                {
                    object realKey = key.Target;
                    if (realKey != null)
                        keys.Add(realKey);
                }
                return keys;
            }
        }

        public override object this[object key]
        {
            get
            {
                return base[key];
            }
            set
            {
                CleanIfNeeded();
                base[new WeakKey(key)] = value;
            }
        }

        public override void CopyTo(Array array, int index)
        {
            int arrayIndex = index;
            foreach (DictionaryEntry de in this)
            {
                array.SetValue(de, arrayIndex++);
            }
        }

        public override int Count
        {
            get
            {
                CleanIfNeeded();
                return base.Count;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    #endregion

    public class Cryptography
    {
        static public bool FIPSCompliant = false;

        static public System.Security.Cryptography.HashAlgorithm GetHashAlgorithm()
        {
            if (FIPSCompliant)
            {
                //LUCENENET-175
                //No Assumptions should be made on the HashAlgorithm. It may change in time.
                //SHA256 SHA384 SHA512 etc.
                return System.Security.Cryptography.SHA1.Create();
            }
            return System.Security.Cryptography.MD5.Create();
        }
    }


}
