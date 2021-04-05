namespace Lucene
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
    /// Used to identify an exception as a Java RuntimeException type.
    /// <para/>
    /// Lucene Exception types need to be identified as an RuntimeException to our exception
    /// handling framework when they derive from Error in Java.
    /// However, <see cref="RuntimeException"/> is internal and C# doesn't allow a
    /// public exception to subclass an internal one, so as a workaround,
    /// add this interface instead and subclass the most logical exception in .NET.
    /// </summary>
    interface IRuntimeException
    {
    }
}
