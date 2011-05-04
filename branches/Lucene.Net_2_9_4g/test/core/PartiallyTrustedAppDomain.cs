/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.IO;
using System.Dynamic;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Test
{
    public class PartiallyTrustedAppDomain<T> : DynamicObject, IDisposable where T : MarshalByRefObject
    {
        string      _TempDir;
        AppDomain   _AppDomain;
        Type        _ClassType;
        object      _RealObject;

        public PartiallyTrustedAppDomain()
        {
            Init(null);
        }

        public PartiallyTrustedAppDomain(params object[] constructorArgs)
        {
            Init(constructorArgs);
        }


        void Init(object[] constructorArgs)
        {
            _TempDir = System.Environment.GetEnvironmentVariable("TEMP");

            _ClassType = typeof(T);

            AppDomainSetup setup = new AppDomainSetup { ApplicationBase = Environment.CurrentDirectory };

            PermissionSet permissions = new PermissionSet(null);
            permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
            permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, TempDir));

            _AppDomain = AppDomain.CreateDomain("PartiallyTrustedAppDomain", null, setup, permissions);

            _RealObject = _AppDomain.CreateInstanceAndUnwrap(
                    _ClassType.Assembly.FullName,
                    _ClassType.FullName,
                    false,
                    BindingFlags.CreateInstance,
                    null,
                    constructorArgs,
                    System.Globalization.CultureInfo.CurrentCulture,
                    null);
        }

        object Run(string methodName, object[] constructorArgs, object[] methodArgs)
        {
            try
            {
                object ret = _ClassType.InvokeMember(
                    methodName,
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    _RealObject,
                    methodArgs);

                return ret;
            }
            catch (TargetInvocationException tiex)
            {
                throw tiex.InnerException;
            }
        }

        public string TempDir
        {
            get { return _TempDir; }
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                result = Run(binder.Name, null, args);
                return true;
            }
            catch (MissingMethodException)
            {
                result = null;
                return false;
            }
        }

        public void Dispose()
        {
            if (_AppDomain != null) AppDomain.Unload(_AppDomain);
        }
    }
}

