/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Store
{
    internal class IsolatedStorageLock : Lucene.Net.Store.Lock
    {

        System.IO.IsolatedStorage.IsolatedStorageFile _dir;
        System.IO.IsolatedStorage.IsolatedStorageFileStream _file;
        string _name;

        public IsolatedStorageLock(System.IO.IsolatedStorage.IsolatedStorageFile dir, string name)
        {
            _dir = dir;
            _name = name;

        }

        public override bool Obtain()
        {
            try
            {
                _file = _dir.CreateFile(_name);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Release()
        {
            _file.Close();
            _dir.DeleteFile(_name);

        }

        public override bool IsLocked()
        {
            return _dir.FileExists(_name);
        }
    }
}
