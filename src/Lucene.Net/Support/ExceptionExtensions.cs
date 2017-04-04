using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Extensions to the <see cref="Exception"/> class to allow for
    /// adding and retrieving suppressed exceptions, like you can do in Java.
    /// </summary>
    public static class ExceptionExtensions
    {
        public static readonly string SUPPRESSED_EXCEPTIONS_KEY = "Lucene_SuppressedExceptions";

        public static Exception[] GetSuppressed(this Exception e)
        {
            return e.GetSuppressedAsList().ToArray();
        }

        public static IList<Exception> GetSuppressedAsList(this Exception e)
        {
            IList<Exception> suppressed;
            if (!e.Data.Contains(SUPPRESSED_EXCEPTIONS_KEY))
            {
                suppressed = new List<Exception>();
                e.Data.Add(SUPPRESSED_EXCEPTIONS_KEY, suppressed);
            }
            else
            {
                suppressed = e.Data[SUPPRESSED_EXCEPTIONS_KEY] as IList<Exception>;
            }

            return suppressed;
        }

        public static void AddSuppressed(this Exception e, Exception exception)
        {
            e.GetSuppressedAsList().Add(exception);
        }
    }
}
