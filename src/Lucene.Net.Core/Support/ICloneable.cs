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

namespace Lucene.Net.Support
{
    /// <summary>
    /// Supports cloning, which creates a new instance of a class with the same value as an existing instance.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This <see cref="System.ICloneable"/> interface is not supported in all versions of the .NET framework. This is 
    ///         Microsoft BCL developer state that the interface does not inform users of the type of clone that is being performed. 
    ///         <see href="http://blogs.msdn.com/b/brada/archive/2003/04/09/49935.aspx">Brad Abrams even suggests not to use it.</see>
    ///     </para>
    ///     <para>
    ///         The Lucene code base makes heavy use of the Java equivelant of ICloneable in its public APIs. In order to help keep on 
    ///         par with Lucene.Net's parent project and keep with design standards, the <see cref="ICloneable.Clone(bool)"/> method
    ///         has a parameter that instructs the instance to make a deep clone when true.  If a deep or shallow clone is not supported,
    ///         an exception must be thrown. 
    ///     </para>
    ///     <seealso cref="SupportExtensionMethods.CloneAndCast{T}(T, bool)"/>
    /// </remarks>
    public interface ICloneable
    {
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <param name="deepClone">Instructs the instance to create a deep copy when true.</param>
        /// <returns>A clone of the current instance.</returns>
        /// <exception cref="Lucene.Net.Support.ShallowCloneNotSupportedException">Thrown when a shallow clone is not supported.</exception>
        /// <exception cref="Lucene.Net.Support.DeepCloneNotSupportedException">Thrown when a deep clone is not supported.</exception>
        object Clone(bool deepClone);
    }
}
