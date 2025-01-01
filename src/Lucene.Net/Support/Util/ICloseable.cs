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
    /// Represents a source or destination of data that can be closed.
    /// </summary>
    /// <remarks>
    /// LUCENENET specific - this interface is to be used when a class
    /// is designed to be reusable after being closed, unlike IDisposable,
    /// when the instance is no longer usable after being disposed.
    /// </remarks>
    public interface ICloseable
    {
        /// <summary>
        /// Closes this object in a way that allows it to be reused.
        /// </summary>
        void Close();
    }
}
