// Lucene version compatibility level 4.8.1
using System;
using System.IO;
using System.Text;

namespace Lucene.Net.Analysis.Util
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
    /// Fake resource loader for tests: works if you want to fake reading a single file </summary>
    public class StringMockResourceLoader : IResourceLoader
    {
        internal string text;

        public StringMockResourceLoader(string text)
        {
            this.text = text;
        }

        public Type FindType(string cname)
        {
            try
            {
                return Type.GetType(cname);
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create("Cannot load class: " + cname, e);
            }
        }

        public T NewInstance<T>(string cname)
        {
            Type clazz = FindType(cname);
            try
            {
                return (T)Activator.CreateInstance(clazz);
            }
            catch (Exception e) when (e.IsException())
            {
                throw RuntimeException.Create("Cannot create instance: " + cname, e);
            }
        }

        public Stream OpenResource(string resource)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }
    }
}