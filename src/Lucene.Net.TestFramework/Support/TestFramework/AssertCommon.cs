using Lucene.Net.Util;
using System;
using System.IO;

namespace Lucene.Net.TestFramework
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
    /// Common assertion code
    /// </summary>
    internal partial class Assert
    {
        private const int WARN_WIN32_FILE_EXISTS = unchecked((int)0x80070050);

        private static bool IsFileAlreadyExistsException(Exception ex, string filePath)
        {
            if (!typeof(IOException).Equals(ex))
                return false;
            else if (Constants.WINDOWS)
                return ex.HResult == WARN_WIN32_FILE_EXISTS;
            else
                return File.Exists(filePath);
        }
    }
}
