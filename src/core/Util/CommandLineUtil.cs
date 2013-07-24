using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public static class CommandLineUtil
    {
        public static FSDirectory NewFSDirectory(string clazzName, DirectoryInfo dir)
        {
            try
            {
                Type clazz = LoadFSDirectoryClass(clazzName);
                return NewFSDirectory(clazz, dir);

            }
            catch (TypeLoadException e)
            {
                throw new ArgumentException(typeof(FSDirectory).Name
                    + " implementation not found: " + clazzName, e);
            }
            catch (MissingMethodException e)
            {
                throw new ArgumentException(clazzName + " constructor with "
                    + typeof(FileInfo).Name + " as parameter not found", e);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Error creating " + clazzName + " instance", e);
            }
        }

        public static Type LoadDirectoryClass(String clazzName)
        {
            return Type.GetType(AdjustDirectoryClassName(clazzName));
        }

        public static Type LoadFSDirectoryClass(String clazzName)
        {
            return Type.GetType(AdjustDirectoryClassName(clazzName));
        }

        private static String AdjustDirectoryClassName(String clazzName)
        {
            if (clazzName == null || clazzName.Trim().Length == 0)
            {
                throw new ArgumentException("The " + typeof(FSDirectory).Name
                    + " implementation cannot be null or empty");
            }

            if (clazzName.IndexOf(".") == -1)
            {
                // if not fully qualified, assume .store
                clazzName = typeof(Lucene.Net.Store.Directory).Namespace + "." + clazzName;
            }
            return clazzName;
        }

        public static FSDirectory NewFSDirectory(Type clazz, DirectoryInfo dir)
        {
            // this is the .NET version of the java line, but is unneccessary with Activator.CreateInstance
            //var ctor = clazz.GetConstructor(new[] { typeof(DirectoryInfo) });

            return (FSDirectory)Activator.CreateInstance(clazz, dir);
        }
    }
}
