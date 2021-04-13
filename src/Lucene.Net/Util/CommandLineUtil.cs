using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

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

    using Directory = Lucene.Net.Store.Directory;
    using FSDirectory = Lucene.Net.Store.FSDirectory;

    /// <summary>
    /// Class containing some useful methods used by command line tools
    /// </summary>
    public static class CommandLineUtil // LUCENENET specific - made static
    {
        /// <summary>
        /// Creates a specific <see cref="FSDirectory"/> instance starting from its class name. </summary>
        /// <param name="clazzName"> The name of the <see cref="FSDirectory"/> class to load. </param>
        /// <param name="dir"> The <see cref="DirectoryInfo"/> to be used as parameter constructor. </param>
        /// <returns> The new <see cref="FSDirectory"/> instance </returns>
        public static FSDirectory NewFSDirectory(string clazzName, DirectoryInfo dir)
        {
            try
            {
                Type clazz = LoadFSDirectoryClass(clazzName);

                // LUCENENET: In .NET, we get a null when the class is not found, so we need to throw here for compatibility
                if (clazz is null)
                    throw new ArgumentException(typeof(FSDirectory).Name + " implementation not found: " + clazzName);

                return NewFSDirectory(clazz, dir);
            }
            catch (Exception e) when (e.IsClassNotFoundException())
            {
                throw new ArgumentException(typeof(FSDirectory).Name + " implementation not found: " + clazzName, e);
            }
            catch (Exception e) when (e.IsClassCastException())
            {
                throw new ArgumentException(clazzName + " is not a " + typeof(FSDirectory).Name + " implementation", e);
            }
            catch (Exception e) when (e.IsNoSuchMethodException())
            {
                throw new ArgumentException(clazzName + " constructor with " + typeof(FileInfo).Name + " as parameter not found", e);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error creating " + clazzName + " instance", e);
            }
        }

        /// <summary>
        /// Loads a specific <see cref="Directory"/> implementation. </summary>
        /// <param name="clazzName"> The name of the <see cref="Directory"/> class to load. </param>
        /// <returns> The <see cref="Directory"/> class loaded. </returns>
        /// <exception cref="TypeLoadException"> If the specified class cannot be found. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type LoadDirectoryClass(string clazzName)
        {
            return Type.GetType(AdjustDirectoryClassName(clazzName));
        }

        /// <summary>
        /// Loads a specific <see cref="FSDirectory"/> implementation. </summary>
        /// <param name="clazzName"> The name of the <see cref="FSDirectory"/> class to load. </param>
        /// <returns> The <see cref="FSDirectory"/> class loaded. </returns>
        /// <exception cref="TypeLoadException"> If the specified class cannot be found. </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type LoadFSDirectoryClass(string clazzName)
        {
            return Type.GetType(AdjustDirectoryClassName(clazzName));
        }

        private static string AdjustDirectoryClassName(string clazzName)
        {
            if (clazzName is null || clazzName.Trim().Length == 0)
            {
                throw new ArgumentException("The " + typeof(FSDirectory).Name + " implementation cannot be null or empty");
            }

            // LUCENENET specific: Changed to use char rather than string so we get StringComparison.Ordinal,
            // otherwise this could fail on some operating systems in certain cultures.
            if (clazzName.IndexOf('.') == -1) // if not fully qualified, assume .store
            {
                // LUCENENET NOTE: .NET expects the type to be in the currently executing assembly or mscorlib
                // if not fully qualified. This fails on macOS if called from LuceneTestCase.NewFSDirectory() without the AssemblyQualifiedName.
                clazzName = typeof(Directory).AssemblyQualifiedName.Replace(nameof(Directory), clazzName);
            }
            return clazzName;
        }

        /// <summary>
        /// Creates a new specific <see cref="FSDirectory"/> instance. </summary>
        /// <param name="clazz"> The class of the object to be created </param>
        /// <param name="dir"> The <see cref="DirectoryInfo"/> to be used as parameter constructor </param>
        /// <returns> The new <see cref="FSDirectory"/> instance. </returns>
        /// <exception cref="MissingMethodException"> If the <see cref="Directory"/> does not have a constructor that takes <see cref="DirectoryInfo"/>. </exception>
        /// <exception cref="MemberAccessException"> If the class is abstract or an interface. </exception>
        /// <exception cref="TypeLoadException"> If the constructor does not have public visibility. </exception>
        /// <exception cref="TargetInvocationException"> If the constructor throws an exception </exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FSDirectory NewFSDirectory(Type clazz, DirectoryInfo dir)
        {
            // Assuming every FSDirectory has a ctor(File):
            //Constructor<?> ctor = clazz.GetConstructor(typeof(File));
            return (FSDirectory)Activator.CreateInstance(clazz, dir);
        }
    }
}