using System;
using System.IO;

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
    ///
    /// </summary>
    public sealed class CommandLineUtil
    {
        private CommandLineUtil()
        {
        }

        /// <summary>
        /// Creates a specific FSDirectory instance starting from its class name </summary>
        /// <param name="clazzName"> The name of the FSDirectory class to load </param>
        /// <param name="file"> The file to be used as parameter constructor </param>
        /// <returns> the new FSDirectory instance </returns>
        public static FSDirectory NewFSDirectory(string clazzName, DirectoryInfo dir)
        {
            try
            {
                Type clazz = LoadFSDirectoryClass(clazzName);
                return NewFSDirectory(clazz, dir);
            }
            catch (TypeLoadException e)
            {
                throw new System.ArgumentException(typeof(FSDirectory).Name + " implementation not found: " + clazzName, e);
            }
            catch (System.InvalidCastException e)
            {
                throw new System.ArgumentException(clazzName + " is not a " + typeof(FSDirectory).Name + " implementation", e);
            }
            catch (MissingMethodException e)
            {
                throw new System.ArgumentException(clazzName + " constructor with " + typeof(FileInfo).Name + " as parameter not found", e);
            }
            catch (Exception e)
            {
                throw new System.ArgumentException("Error creating " + clazzName + " instance", e);
            }
        }

        /// <summary>
        /// Loads a specific Directory implementation </summary>
        /// <param name="clazzName"> The name of the Directory class to load </param>
        /// <returns> The Directory class loaded </returns>
        /// <exception cref="ClassNotFoundException"> If the specified class cannot be found. </exception>
        public static Type LoadDirectoryClass(string clazzName)
        {
            return Type.GetType(AdjustDirectoryClassName(clazzName));
        }

        /// <summary>
        /// Loads a specific FSDirectory implementation </summary>
        /// <param name="clazzName"> The name of the FSDirectory class to load </param>
        /// <returns> The FSDirectory class loaded </returns>
        /// <exception cref="ClassNotFoundException"> If the specified class cannot be found. </exception>
        public static Type LoadFSDirectoryClass(string clazzName)
        {
            return Type.GetType(AdjustDirectoryClassName(clazzName));
        }

        private static string AdjustDirectoryClassName(string clazzName)
        {
            if (clazzName == null || clazzName.Trim().Length == 0)
            {
                throw new System.ArgumentException("The " + typeof(FSDirectory).Name + " implementation cannot be null or empty");
            }

            if (clazzName.IndexOf(".") == -1) // if not fully qualified, assume .store
            {
                clazzName = typeof(Directory).Namespace + "." + clazzName;
            }
            return clazzName;
        }

        /// <summary>
        /// Creates a new specific FSDirectory instance </summary>
        /// <param name="clazz"> The class of the object to be created </param>
        /// <param name="file"> The file to be used as parameter constructor </param>
        /// <returns> The new FSDirectory instance </returns>
        /// <exception cref="NoSuchMethodException"> If the Directory does not have a constructor that takes <code>File</code>. </exception>
        /// <exception cref="InstantiationException"> If the class is abstract or an interface. </exception>
        /// <exception cref="IllegalAccessException"> If the constructor does not have public visibility. </exception>
        /// <exception cref="InvocationTargetException"> If the constructor throws an exception </exception>
        public static FSDirectory NewFSDirectory(Type clazz, DirectoryInfo dir)
        {
            // Assuming every FSDirectory has a ctor(File):
            //Constructor<?> ctor = clazz.GetConstructor(typeof(File));
            return (FSDirectory)Activator.CreateInstance(clazz, dir);
        }
    }
}