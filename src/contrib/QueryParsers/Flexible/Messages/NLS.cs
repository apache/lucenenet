using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Messages
{
    public abstract class NLS
    {
        // .NET Port: QueryParserMessages has to inherit from NLS, otherwise we would make this a static class.
        
        private static IDictionary<string, Type> bundles = new HashMap<String, Type>(0);
        
        public static string GetLocalizedMessage(string key)
        {
            return GetLocalizedMessage(key, CultureInfo.CurrentCulture);
        }

        public static string GetLocalizedMessage(string key, CultureInfo locale)
        {
            object message = GetResourceBundleObject(key, locale);
            if (message == null)
            {
                return "Message with key:" + key + " and locale: " + locale
                    + " not found.";
            }
            return message.ToString();
        }

        public static string GetLocalizedMessage(string key, CultureInfo locale, params object[] args)
        {
            string str = GetLocalizedMessage(key, locale);

            if (args.Length > 0)
            {
                str = string.Format(str, args);
            }

            return str;
        }

        public static string GetLocalizedMessage(string key, params object[] args)
        {
            return GetLocalizedMessage(key, CultureInfo.CurrentCulture, args);
        }

        protected static void InitializeMessages(string bundleName, Type clazz)
        {
            try
            {
                Load(clazz);
                if (!bundles.ContainsKey(bundleName))
                    bundles[bundleName] = clazz;
            }
            catch
            {
                // ignore all errors and exceptions
                // because this function is supposed to be called at class load time.
            }
        }

        private static object GetResourceBundleObject(string messageKey, CultureInfo locale)
        {
            // TODO: .NET Port -- is this ResourceManager logic correct?

            // slow resource checking
            // need to loop thru all registered resource bundles
            foreach (string key in bundles.Keys)
            {
                Type clazz = bundles[key];

                var resourceBundle = new ResourceManager(clazz);

                try
                {
                    Object obj = resourceBundle.GetObject(messageKey);
                    if (obj != null)
                        return obj;
                }
                catch (MissingManifestResourceException)
                {
                    // just continue it might be on the next resource bundle
                }

            }
            // if resource is not found
            return null;
        }

        private static void Load(Type clazz)
        {
            FieldInfo[] fieldArray = clazz.GetFields(BindingFlags.Public | BindingFlags.Instance);

            bool isFieldAccessible = clazz.IsPublic;

            // build a map of field names to Field objects
            int len = fieldArray.Length;
            IDictionary<String, FieldInfo> fields = new HashMap<String, FieldInfo>(len * 2);
            for (int i = 0; i < len; i++)
            {
                fields[fieldArray[i].Name] = fieldArray[i];
                LoadFieldValue(fieldArray[i], isFieldAccessible, clazz);
            }
        }

        private static void LoadFieldValue(FieldInfo field, bool isFieldAccessible, Type clazz)
        {
            if (field.IsInitOnly || !field.IsPublic || !field.IsStatic)
                return;

            // Set a value for this empty field.
            if (!isFieldAccessible)
                MakeAccessible(field);
            try
            {
                field.SetValue(null, field.Name);
                ValidateMessage(field.Name, clazz);
            }
            catch (ArgumentException)
            {
                // should not happen
            }
            catch (FieldAccessException)
            {
                // should not happen
            }
        }

        private static void ValidateMessage(string key, Type clazz)
        {
            // TODO: .NET Port -- is the ResourceManager logic correct?

            // Test if the message is present in the resource bundle
            try
            {
                var resourceBundle = new ResourceManager(clazz);
                if (resourceBundle != null)
                {
                    Object obj = resourceBundle.GetObject(key);
                    //if (obj == null)
                    //  System.err.println("WARN: Message with key:" + key + " and locale: "
                    //      + Locale.getDefault() + " not found.");
                }
            }
            catch (MissingManifestResourceException)
            {
                //System.err.println("WARN: Message with key:" + key + " and locale: "
                //    + Locale.getDefault() + " not found.");
            }
            catch (Exception)
            {
                // ignore all other errors and exceptions
                // since this code is just a test to see if the message is present on the
                // system
            }
        }

        private static void MakeAccessible(FieldInfo field)
        {
            // .NET Port: no way to make a field accessible here, noop

            //if (System.getSecurityManager() == null) {
            //  field.setAccessible(true);
            //} else {
            //  AccessController.doPrivileged(new PrivilegedAction<Void>() {
            //    @Override
            //    public Void run() {
            //      field.setAccessible(true);
            //      return null;
            //    }
            //  });
            //}
        }
    }
}
