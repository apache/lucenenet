using System;

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
    /// Attribute to define a property or method as a writable array.
    /// Per MSDN, members should never return arrays because the array contents
    /// can be updated, which makes the behavior confusing. However,
    /// Lucene's design sometimes relies on other classes to update arrays -
    /// both as array fields and as methods that return arrays. So, in these
    /// cases we are making an exception to this rule and marking them with
    /// <see cref="WritableArrayAttribute"/> to signify that this is intentional.
    /// <para/>
    /// For properties that violate this rule, you should also use
    /// the <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>:
    /// <code>
    /// [WritableArray, SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class WritableArrayAttribute : Attribute 
    {
    }
}
