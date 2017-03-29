using System.Collections.Generic;

namespace Lucene.Net.Support
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
    /// A set of utilities uses for easily wrapping .NET
    /// collections so they can be used with <see cref="object.Equals(object)"/>
    /// <see cref="object.GetHashCode()"/>, and <see cref="object.ToString()"/>
    /// behavior similar to that in Java. The equality checking of collections
    /// will recursively compare the values of all elements and any nested collections.
    /// The same goes for using <see cref="object.ToString()"/> - the string is based
    /// on the values in the collection and any nested collections.
    /// <para/>
    /// Do note this has a side-effect that any custom <see cref="object.Equals(object)"/>
    /// <see cref="object.GetHashCode()"/>, and <see cref="object.ToString()"/> implementations
    /// for types that implement <see cref="IList{T}"/> (including arrays), <see cref="ISet{T}"/>,
    /// or <see cref="IDictionary{TKey, TValue}"/> will be ignored.
    /// </summary>
    public static class Equatable
    {
        /// <summary>
        /// Wraps any <see cref="IList{T}"/> (including <see cref="T:T[]"/>) with a 
        /// lightweight <see cref="EquatableList{T}"/> class that changes the behavior
        /// of <see cref="object.Equals(object)"/>
        /// <see cref="object.GetHashCode()"/>, and <see cref="object.ToString()"/>
        /// so they consider all values in the <see cref="IList{T}"/> or any nested
        /// collections when comparing or making strings to represent them.
        /// No other behavior is changed - only these 3 methods.
        /// <para/>
        /// Note that if the list is already an <see cref="EquatableList{T}"/> or a subclass
        /// of it, this method simply returns the provided <paramref name="list"/>.
        /// </summary>
        /// <typeparam name="T">the type of element</typeparam>
        /// <param name="list">Any <see cref="IList{T}"/> (including <see cref="T:T[]"/>)</param>
        /// <returns>An <see cref="EquatableList{T}"/> that wraps the provided <paramref name="list"/>, 
        /// or the value of <paramref name="list"/> unmodified if it already is an <see cref="EquatableList{T}"/></returns>
        public static IList<T> Wrap<T>(IList<T> list)
        {
            return (list is EquatableList<T>) ? list : new EquatableList<T>(list, true);
        }

        /// <summary>
        /// Wraps any <see cref="ISet{T}"/> with a 
        /// lightweight <see cref="EquatableSet{T}"/> class that changes the behavior
        /// of <see cref="object.Equals(object)"/>
        /// <see cref="object.GetHashCode()"/>, and <see cref="object.ToString()"/>
        /// so they consider all values in the <see cref="ISet{T}"/> or any nested
        /// collections when comparing or making strings to represent them.
        /// No other behavior is changed - only these 3 methods.
        /// <para/>
        /// Note that if the set is already an <see cref="EquatableSet{T}"/> or a subclass
        /// of it, this method simply returns the provided <paramref name="set"/>.
        /// </summary>
        /// <typeparam name="T">the type of element</typeparam>
        /// <param name="set">Any <see cref="IList{T}"/> (including <see cref="T:T[]"/>)</param>
        /// <returns>An <see cref="EquatableSet{T}"/> that wraps the provided <paramref name="set"/>, 
        /// or the value of <paramref name="set"/> unmodified if it already is an <see cref="EquatableSet{T}"/></returns>
        public static ISet<T> Wrap<T>(ISet<T> set)
        {
            return (set is EquatableSet<T>) ? set : new EquatableSet<T>(set, true);
        }
    }
}
