using System;

namespace Lucene.Net.DependencyInjection
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
    /// A service descriptor that can be used to lookup a service implementation
    /// using a <see cref="string"/> name.
    /// </summary>
    internal class NamedServiceDescriptor
    {
        public NamedServiceDescriptor(string name, Type serviceType)
        {
            this.Name = name;
            this.ServiceType = serviceType;
        }

        public string Name { get; private set; }
        public Type ServiceType { get; private set; }

        public override bool Equals(object obj)
        {
            if (!(obj is NamedServiceDescriptor))
                return false;

            var other = (NamedServiceDescriptor)obj;

            return Name.Equals(other.Name, StringComparison.OrdinalIgnoreCase) &&
                ServiceType.Equals(other.ServiceType);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ ServiceType.GetHashCode();
        }
    }
}
