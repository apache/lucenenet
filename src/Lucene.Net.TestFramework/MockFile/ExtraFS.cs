// Lucene version compatibility level <= 8.2.0
// LUCENENET NOTE: This class is only partially complete, and should be public when finally ported from 8.2.0+
using System;

namespace Lucene.Net.MockFile
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

    internal class ExtrasFS
    {
        private const string EXTRA_FILE_NAME = "extra0";

        // TODO: would be great if we overrode attributes, so file size was always zero for
        // our fake files. But this is tricky because its hooked into several places. 
        // Currently MDW has a hack so we don't break disk full tests.

        /// <summary>
        /// Return true if <paramref name="fileName"/> is one of the extra files added by this class.
        /// </summary>
        public static bool IsExtra(string fileName)
        {
            return fileName.Equals(EXTRA_FILE_NAME, StringComparison.Ordinal);
        }
    }
}