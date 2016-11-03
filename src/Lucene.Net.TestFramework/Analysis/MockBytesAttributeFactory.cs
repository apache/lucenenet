namespace Lucene.Net.Analysis
{
    using System.Reflection;
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

    using Attribute = Lucene.Net.Util.Attribute;
    using AttributeSource = Lucene.Net.Util.AttributeSource;

    /// <summary>
    /// Attribute factory that implements CharTermAttribute with
    /// <seealso cref="MockUTF16TermAttributeImpl"/>
    /// </summary>
    public class MockBytesAttributeFactory : AttributeSource.AttributeFactory
    {
        private readonly AttributeSource.AttributeFactory @delegate = DEFAULT_ATTRIBUTE_FACTORY;

        public override Attribute CreateAttributeInstance<T>()
        {
            var attClass = typeof(T);
            return attClass.IsAssignableFrom(typeof(MockUTF16TermAttributeImpl)) ? new MockUTF16TermAttributeImpl() : @delegate.CreateAttributeInstance<T>();
        }
    }
}