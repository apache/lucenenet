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

namespace Lucene.Net.Util
{
    // This class might be better off as a base attribute to simplify the way the code queries
    // for types that implement this interface.  Look at the AttributeSource.cs class. If there
    // is a good reason to keep this as-is, please notate this in the comments and remove this one.
	
    // JAVA: src/java/org/apache/lucene/util/Attribute.java

	/// <summary> 
    /// The contract interface for attributes.
    /// This interface is used as a way to query types that implement this interface and 
    /// references to those types that do.
    /// </summary>
	public interface IAttribute
	{
	}
}