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
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;

using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Test
{
    public class PartiallyTrustedAppDomain
    {
        public static string TEMPDIR = @"c:\temp\testindex";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="clazz">class to create</param>
        /// <param name="methodName">method to invoke</param>
        /// <param name="constructorArgs">constructor's parameters</param>
        /// <param name="methodArgs">method's parameters</param>
        public static object Run(Type clazz, string methodName, object[] constructorArgs, object[] methodArgs)
        {
            AppDomain appDomain = null;
            try
            {
                AppDomainSetup setup = new AppDomainSetup { ApplicationBase = Environment.CurrentDirectory };

                PermissionSet permissions = new PermissionSet(null);
                permissions.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                permissions.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
                permissions.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, TEMPDIR));

                appDomain = AppDomain.CreateDomain("PartiallyTrustedAppDomain", null, setup, permissions);

                object obj = appDomain.CreateInstanceAndUnwrap(
                    clazz.Assembly.FullName,
                    clazz.FullName,
                    false,
                    BindingFlags.CreateInstance,
                    null,
                    constructorArgs,
                    System.Globalization.CultureInfo.CurrentCulture,
                    null);

                object ret = clazz.InvokeMember(
                    methodName,
                    BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    obj,
                    methodArgs);

                return ret;
            }
            catch (TargetInvocationException tiex)
            {
                throw tiex.InnerException;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if(appDomain!=null) AppDomain.Unload(appDomain);
            }
        }
    }
}

