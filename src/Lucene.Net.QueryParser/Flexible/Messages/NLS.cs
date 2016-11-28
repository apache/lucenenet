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
    /// <summary>
    /// MessageBundles classes extend this class, to implement a bundle.
    /// 
    /// For Native Language Support (NLS), system of software internationalization.
    /// 
    /// This interface is similar to the NLS class in eclipse.osgi.util.NLS class -
    /// initializeMessages() method resets the values of all static strings, should
    /// only be called by classes that extend from NLS (see TestMessages.java for
    /// reference) - performs validation of all message in a bundle, at class load
    /// time - performs per message validation at runtime - see NLSTest.java for
    /// usage reference
    /// 
    /// MessageBundle classes may subclass this type.
    /// </summary>
    public class NLS
    {
        private static IDictionary<string, Type> bundles = new Dictionary<string, Type>(0);

        protected NLS()
        {
            // Do not instantiate
        }

        public static string GetLocalizedMessage(string key)
        {
            return GetLocalizedMessage(key, CultureInfo.InvariantCulture);
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

        public static string GetLocalizedMessage(string key, CultureInfo locale,
            params object[] args)
        {
            string str = GetLocalizedMessage(key, locale);

            // LUCENENET TODO: Figure out what the equivalent of this is
            //if (args.Length > 0)
            //{
            //    str = MessageFormat.Format(str, args);
            //}

            return str;
        }

        public static string GetLocalizedMessage(string key, params object[] args)
        {
            return GetLocalizedMessage(key, CultureInfo.InvariantCulture, args);
        }

        /**
         * Initialize a given class with the message bundle Keys Should be called from
         * a class that extends NLS in a static block at class load time.
         * 
         * @param bundleName
         *          Property file with that contains the message bundle
         * @param clazz
         *          where constants will reside
         */
        protected static void InitializeMessages(string bundleName, Type clazz)
        {
            try
            {
                Load(clazz);
                if (!bundles.ContainsKey(bundleName))
                    bundles[bundleName] = clazz;
            }
            catch (Exception e)
            {
                // ignore all errors and exceptions
                // because this function is supposed to be called at class load time.
            }
        }

        private static object GetResourceBundleObject(string messageKey, CultureInfo locale)
        {
            // Set the UI culture to the passed in locale.
            using (var culture = new CultureContext(locale, locale))
            {

                // slow resource checking
                // need to loop thru all registered resource bundles
                for (IEnumerator<string> it = bundles.Keys.GetEnumerator(); it.MoveNext();)
                {
                    Type clazz = bundles[it.Current];
                    ResourceManager resourceBundle = new ResourceManager(clazz);
                    if (resourceBundle != null)
                    {
                        try
                        {
                            object obj = resourceBundle.GetObject(messageKey);
                            if (obj != null)
                                return obj;
                        }
                        catch (MissingManifestResourceException e)
                        {
                            // just continue it might be on the next resource bundle
                        }
                    }


                    //ResourceBundle resourceBundle = ResourceBundle.getBundle(clazz.Name,
                    //    locale);
                    //if (resourceBundle != null)
                    //{
                    //    try
                    //    {
                    //        object obj = resourceBundle.getObject(messageKey);
                    //        if (obj != null)
                    //            return obj;
                    //    }
                    //    catch (MissingResourceException e)
                    //    {
                    //        // just continue it might be on the next resource bundle
                    //    }
                    //}
                }
                // if resource is not found
                return null;
            }
        }

        private static void Load(Type clazz)
        {
            FieldInfo[] fieldArray = clazz.GetFields();

            //bool isFieldAccessible = (clazz.getModifiers() & Modifier.PUBLIC) != 0;

            // build a map of field names to Field objects
            int len = fieldArray.Length;
            IDictionary<string, FieldInfo> fields = new Dictionary<string, FieldInfo>(len * 2);
            for (int i = 0; i < len; i++)
            {
                fields[fieldArray[i].Name] = fieldArray[i];
                loadfieldValue(fieldArray[i], /*isFieldAccessible,*/ clazz);
            }
        }

        private static void loadfieldValue(FieldInfo field, Type clazz)
        {
            field.SetValue(null, field.Name);
            ValidateMessage(field.Name, clazz);


            //int MOD_EXPECTED = Modifier.PUBLIC | Modifier.STATIC;
            //int MOD_MASK = MOD_EXPECTED | Modifier.FINAL;
            //if ((field.getModifiers() & MOD_MASK) != MOD_EXPECTED)
            //    return;

            //// Set a value for this empty field.
            //if (!isFieldAccessible)
            //    MakeAccessible(field);
            //try
            //{
            //    field.SetValue(null, field.Name);
            //    ValidateMessage(field.Name, clazz);
            //}
            //catch (ArgumentException e)
            //{
            //    // should not happen
            //}
            ////catch (IllegalAccessException e)
            ////{
            ////    // should not happen
            ////}
        }

        //private static void loadfieldValue(FieldInfo field, bool isFieldAccessible,
        //    Type clazz)
        //{
        //    int MOD_EXPECTED = Modifier.PUBLIC | Modifier.STATIC;
        //    int MOD_MASK = MOD_EXPECTED | Modifier.FINAL;
        //    if ((field.getModifiers() & MOD_MASK) != MOD_EXPECTED)
        //        return;

        //    // Set a value for this empty field.
        //    if (!isFieldAccessible)
        //        MakeAccessible(field);
        //    try
        //    {
        //        field.SetValue(null, field.Name);
        //        ValidateMessage(field.Name, clazz);
        //    }
        //    catch (ArgumentException e)
        //    {
        //        // should not happen
        //    }
        //    //catch (IllegalAccessException e)
        //    //{
        //    //    // should not happen
        //    //}
        //}

        /**
         * @param key
         *          - Message Key
         */
        private static void ValidateMessage(string key, Type clazz)
        {
            // Test if the message is present in the resource bundle
            try
            {
                ResourceManager resourceBundle = new ResourceManager(clazz);
                if (resourceBundle != null)
                {
                    object obj = resourceBundle.GetObject(key);
                    //if (obj == null)
                    //  System.err.println("WARN: Message with key:" + key + " and locale: "
                    //      + Locale.getDefault() + " not found.");
                }
            }
            catch (MissingManifestResourceException e)
            {
                //System.err.println("WARN: Message with key:" + key + " and locale: "
                //    + Locale.getDefault() + " not found.");
            }
            catch (Exception e)
            {
                // ignore all other errors and exceptions
                // since this code is just a test to see if the message is present on the
                // system
            }

            //// Test if the message is present in the resource bundle
            //try
            //{
            //    ResourceBundle resourceBundle = ResourceBundle.getBundle(clazz.Name,
            //        CultureInfo.InvariantCulture);
            //    if (resourceBundle != null)
            //    {
            //        Object obj = resourceBundle.getObject(key);
            //        //if (obj == null)
            //        //  System.err.println("WARN: Message with key:" + key + " and locale: "
            //        //      + Locale.getDefault() + " not found.");
            //    }
            //}
            //catch (MissingResourceException e)
            //{
            //    //System.err.println("WARN: Message with key:" + key + " and locale: "
            //    //    + Locale.getDefault() + " not found.");
            //}
            //catch (Exception e)
            //{
            //    // ignore all other errors and exceptions
            //    // since this code is just a test to see if the message is present on the
            //    // system
            //}
        }

  //      /*
  //       * Make a class field accessible
  //       */
  //      private static void MakeAccessible(FieldInfo field)
  //      {
  //          if (System.getSecurityManager() == null)
  //          {
  //              field.setAccessible(true);
  //          }
  //          else
  //          {
  //              AccessController.doPrivileged(new PrivilegedAction<Void>() {
  //      @Override
  //      public Void run()
  //      {
  //          field.setAccessible(true);
  //          return null;
  //      }
  //  });
  //  }
  //}
    }
}
