// LUCENENET specific - Factored out NLS so end users can elect to use .NET localization or not
// rather than forcing them to use it.

//using Lucene.Net.Support;
//using Lucene.Net.Util;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Reflection;
//using System.Resources;

//namespace Lucene.Net.QueryParsers.Flexible.Messages
//{
//    /*
//     * Licensed to the Apache Software Foundation (ASF) under one or more
//     * contributor license agreements.  See the NOTICE file distributed with
//     * this work for additional information regarding copyright ownership.
//     * The ASF licenses this file to You under the Apache License, Version 2.0
//     * (the "License"); you may not use this file except in compliance with
//     * the License.  You may obtain a copy of the License at
//     *
//     *     http://www.apache.org/licenses/LICENSE-2.0
//     *
//     * Unless required by applicable law or agreed to in writing, software
//     * distributed under the License is distributed on an "AS IS" BASIS,
//     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//     * See the License for the specific language governing permissions and
//     * limitations under the License.
//     */

//    /// <summary>
//    /// MessageBundles classes extend this class, to implement a bundle.
//    /// 
//    /// For Native Language Support (NLS), system of software internationalization.
//    /// 
//    /// This interface is similar to the NLS class in eclipse.osgi.util.NLS class -
//    /// initializeMessages() method resets the values of all static strings, should
//    /// only be called by classes that extend from NLS (see TestMessages.java for
//    /// reference) - performs validation of all message in a bundle, at class load
//    /// time - performs per message validation at runtime - see NLSTest.java for
//    /// usage reference
//    /// 
//    /// MessageBundle classes may subclass this type.
//    /// </summary>
//    public abstract class NLS // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
//    {
//        /// <summary>
//        /// LUCENENET specific factory reference to inject instances of <see cref="ResourceManager"/>
//        /// into this class.
//        /// </summary>
//        private static IResourceManagerFactory resourceManagerFactory = new BundleResourceManagerFactory();
//        private static readonly IDictionary<string, Type> bundles = new Dictionary<string, Type>(0); // LUCENENET: marked readonly

//        protected NLS()
//        {
//            // Do not instantiate
//        }

//        /// <summary>
//        /// Gets the static <see cref="IResourceManagerFactory"/> instance responsible
//        /// for creating <see cref="ResourceManager"/> instances in this class. LUCENENET specific.
//        /// </summary>
//        // LUCENENET NOTE: Don't make this into a property in case we need to make it into an extension method
//        // in a centralized DI configuration builder.
//        public static IResourceManagerFactory GetResourceManagerFactory()
//        {
//            return resourceManagerFactory;
//        }

//        /// <summary>
//        /// Sets the <see cref="IResourceManagerFactory"/> used to create instances of <see cref="ResourceManager"/>
//        /// for retrieving localized resources. Defaults to <see cref="BundleResourceManagerFactory"/> if not set. LUCENENET specific.
//        /// </summary>
//        /// <param name="resourceManagerFactory">The <see cref="IResourceManagerFactory"/> instance. Cannot be <c>null</c>.</param>
//        // LUCENENET NOTE: Don't make this into a property in case we need to make it into an extension method
//        // in a centralized DI configuration builder.
//        public static void SetResourceManagerFactory(IResourceManagerFactory resourceManagerFactory)
//        {
//            NLS.resourceManagerFactory = resourceManagerFactory ?? throw new ArgumentNullException(nameof(resourceManagerFactory));
//        }

//        public static string GetLocalizedMessage(string key)
//        {
//            return GetLocalizedMessage(key, CultureInfo.InvariantCulture);
//        }

//        public static string GetLocalizedMessage(string key, CultureInfo locale)
//        {
//            string message = GetResourceBundleObject(key, locale);
//            if (message is null)
//            {
//                return "Message with key:" + key + " and locale: " + locale
//                    + " not found.";
//            }
//            return message;
//        }

//        public static string GetLocalizedMessage(string key, CultureInfo locale,
//            params object[] args)
//        {
//            string str = GetLocalizedMessage(key, locale);

//            if (args.Length > 0)
//            {
//                str = string.Format(locale, str, args);
//            }

//            return str;
//        }

//        public static string GetLocalizedMessage(string key, params object[] args)
//        {
//            return GetLocalizedMessage(key, CultureInfo.CurrentUICulture, args);
//        }

//        /// <summary>
//        /// Initialize a given class with the message bundle Keys Should be called from
//        /// a class that extends NLS in a static block at class load time.
//        /// </summary>
//        /// <param name="bundleName">Property file with that contains the message bundle</param>
//        /// <param name="clazz">where constants will reside</param>
//        protected static void InitializeMessages(string bundleName, Type clazz)
//        {
//            try
//            {
//                Load(clazz);
//                if (!bundles.ContainsKey(bundleName))
//                    bundles[bundleName] = clazz;
//            }
//            catch (Exception e) when (e.IsThrowable())
//            {
//                // ignore all errors and exceptions
//                // because this function is supposed to be called at class load time.
//            }
//        }

//        private static string GetResourceBundleObject(string messageKey, CultureInfo locale)
//        {
//            // slow resource checking
//            // need to loop thru all registered resource bundles
//            foreach(var key in bundles.Keys)
//            {
//                Type clazz = bundles[key];
//                ResourceManager resourceBundle = resourceManagerFactory.Create(clazz);
//                if (resourceBundle != null)
//                {
//                    try
//                    {
//                        string obj = resourceBundle.GetString(messageKey, locale);
//                        if (obj != null)
//                            return obj;
//                    }
//                    catch (Exception e) when (e.IsMissingResourceException())
//                    {
//                        // just continue it might be on the next resource bundle
//                    }
//                    finally
//                    {
//                        resourceManagerFactory.Release(resourceBundle);
//                    }
//                }
//            }
//            // if resource is not found
//            return null;
//        }

//        private static void Load(Type clazz)
//        {
//            FieldInfo[] fieldArray = clazz.GetFields();

//            // build a map of field names to Field objects
//            int len = fieldArray.Length;
//            IDictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(len * 2);
//            for (int i = 0; i < len; i++)
//            {
//                fields[fieldArray[i].Name] = fieldArray[i];
//                LoadFieldValue(fieldArray[i], clazz);
//            }
//        }

//        private static void LoadFieldValue(FieldInfo field, Type clazz)
//        {
//            field.SetValue(null, field.Name);
//            ValidateMessage(field.Name, clazz);
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="key">Message Key</param>
//        /// <param name="clazz"></param>
//        private static void ValidateMessage(string key, Type clazz)
//        {
//            // Test if the message is present in the resource bundle
//            try
//            {
//                ResourceManager resourceBundle = resourceManagerFactory.Create(clazz);
//                if (resourceBundle != null)
//                {
//                    try
//                    {
//                        string obj = resourceBundle.GetString(key);
//                        //if (obj is null)
//                        //  System.err.println("WARN: Message with key:" + key + " and locale: "
//                        //      + Locale.getDefault() + " not found.");
//                    }
//                    finally
//                    {
//                        resourceManagerFactory.Release(resourceBundle);
//                    }
//                }
//            }
//            catch (Exception e) when (e.IsMissingResourceException())
//            {
//                //System.err.println("WARN: Message with key:" + key + " and locale: "
//                //    + Locale.getDefault() + " not found.");
//            }
//            catch (Exception e) when (e.IsThrowable())
//            {
//                // ignore all other errors and exceptions
//                // since this code is just a test to see if the message is present on the
//                // system
//            }
//        }
//    }
//}
