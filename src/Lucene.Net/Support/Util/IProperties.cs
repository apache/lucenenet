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
    /// Contract for Java-style properties.
    /// </summary>
    internal interface IProperties
    {
        /// <summary>
        /// Retrieves the value of a property from the current process.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        string GetProperty(string key);

        /// <summary>
        /// Retrieves the value of a property from the current process, 
        /// with a default value if it doens't exist or the caller doesn't have 
        /// permission to read the value.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist 
        /// or the caller doesn't have permission to read the value.</param>
        /// <returns>The property value.</returns>
        string GetProperty(string key, string defaultValue);

        /// <summary>
        /// Retrieves the value of a property from the current process
        /// as <see cref="bool"/>. If the value cannot be cast to <see cref="bool"/>, returns <c>false</c>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        bool GetPropertyAsBoolean(string key);

        /// <summary>
        /// Retrieves the value of a property from the current process as <see cref="bool"/>, 
        /// with a default value if it doens't exist, the caller doesn't have permission to read the value, 
        /// or the value cannot be cast to a <see cref="bool"/>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist,
        /// the caller doesn't have permission to read the value, or the value cannot be cast to <see cref="bool"/>.</param>
        /// <returns>The property value.</returns>
        bool GetPropertyAsBoolean(string key, bool defaultValue);

        /// <summary>
        /// Retrieves the value of a property from the current process
        /// as <see cref="int"/>. If the value cannot be cast to <see cref="int"/>, returns <c>0</c>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <returns>The property value.</returns>
        int GetPropertyAsInt32(string key);

        /// <summary>
        /// Retrieves the value of a property from the current process as <see cref="int"/>, 
        /// with a default value if it doens't exist, the caller doesn't have permission to read the value, 
        /// or the value cannot be cast to a <see cref="int"/>.
        /// </summary>
        /// <param name="key">The name of the property.</param>
        /// <param name="defaultValue">The value to use if the property does not exist,
        /// the caller doesn't have permission to read the value, or the value cannot be cast to <see cref="int"/>.</param>
        /// <returns>The property value.</returns>
        int GetPropertyAsInt32(string key, int defaultValue);
    }
}
