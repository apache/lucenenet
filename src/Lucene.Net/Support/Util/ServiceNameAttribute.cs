using System;

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
    /// LUCENENET specific abstract class for <see cref="System.Attribute"/>s that can
    /// be used to override the default convention-based names of services. For example,
    /// "Lucene40Codec" will by convention be named "Lucene40". Using the <see cref="Codecs.CodecNameAttribute"/>,
    /// the name can be overridden with a custom value.
    /// </summary>
    public abstract class ServiceNameAttribute : System.Attribute
    {
        /// <summary>
        /// Sole constructor. Initializes the service name.
        /// </summary>
        /// <param name="name"></param>
        protected ServiceNameAttribute(string name) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            this.Name = name;
        }

        /// <summary>
        /// Gets the service name.
        /// </summary>
        public string Name { get; private set; }
    }
}
