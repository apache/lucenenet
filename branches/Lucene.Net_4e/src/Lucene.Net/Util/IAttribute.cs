// -----------------------------------------------------------------------
// <copyright file="IAttribute.cs" company="Apache">
//
//      Licensed to the Apache Software Foundation (ASF) under one or more
//      contributor license agreements.  See the NOTICE file distributed with
//      this work for additional information regarding copyright ownership.
//      The ASF licenses this file to You under the Apache License, Version 2.0
//      (the "License"); you may not use this file except in compliance with
//      the License.  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
//
// </copyright>
// -----------------------------------------------------------------------


namespace Lucene.Net.Util
{
    using System.Diagnostics.CodeAnalysis;

    /// <summary> 
    /// The contract interface for attributes.  This interface is used as 
    /// a way to query types that extend or implement an interface that 
    /// extends this interface and create references to those types that do.
    /// </summary>
    /// <remarks>
    ///     <note>
    ///         <para>
    ///             <b>Java File: </b> <a href="https://github.com/apache/lucene-solr/blob/trunk/lucene/src/java/org/apache/lucene/util/Attribute.java">
    ///             lucene/src/java/org/apache/lucene/util/Attribute.java
    ///         </a>
    ///         </para>
    ///         <para>
    ///             <b>C# File: </b> <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/src/Lucene.Net/Util/IAttribute.cs">
    ///              src/Lucene.Net/Util/IAttribute.cs
    ///             </a>
    ///         </para>
    ///         <para>
    ///             <b>C# Tests: </b>  <a href="https://github.com/wickedsoftware/lucene.net/tree/lucene-net-4/test/Lucene.Net.Test/Util/IAttributeTest.cs">
    ///             test/Lucene.Net.Test/Util/IAttributeTest.cs
    ///             </a>
    ///         </para>
    ///     </note>
    /// </remarks>
    [SuppressMessage("Microsoft.Design", "CA1040:AvoidEmptyInterfaces",
        Justification = "This interface services as a way to query all interfaces that inherit this one.")]
    public interface IAttribute
    {
    }
}
