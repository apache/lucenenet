using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Lucene.Net
{
    using System.Diagnostics;
    using System.Threading;

    public class TestClass
    {
        private static readonly ThreadLocal<System.Random> random;


        public static Random Random
        {
            get { return random.Value; }
        }

        static TestClass()
        {
            random = new ThreadLocal<System.Random>(() => 
                new System.Random((int) DateTime.Now.Ticks & 0x0000FFFF));
        }

        public class LuceneAssertionException : Exception
        {
            //
            // For guidelines regarding the creation of new exception types, see
            //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
            // and
            //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
            //

            public LuceneAssertionException()
            {
            }

            public LuceneAssertionException(string message) : base(message)
            {
            }

            public LuceneAssertionException(string message, Exception inner) : base(message, inner)
            {
            }

#if NET45
            protected LuceneAssertionException(
                System.Runtime.Serialization.SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
#endif
        }

#if XUNIT

        [DebuggerHidden]
        public static void Null(object value, string message = null, params  object[] args)
        {
            try
            {
                Assert.Null(value);
            }
            catch (Exception ex)
            {
                var msg = message ?? "The value must be null.";
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        public static void NotNull(object value, string message = null, params object[] args)
        {
            try
            {
                Assert.NotNull(value);
            }
            catch (Exception ex)
            {
                var msg = message ?? "The value must not be null.";
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        /// <summary>
        /// Asserts that two object are the same.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerHidden]
        public static void Same(object expected, object actual)
        {
            Assert.Same(expected, actual);
        }

        /// <summary>
        /// Assert that two objects are not the same.
        /// </summary>
        /// <param name="expected">The expected value.</param>
        /// <param name="actual">The actual value.</param>
        [DebuggerHidden]
        public static void NotSame(object expected, object actual)
        {
            Assert.NotSame(expected, actual);
        }

        [DebuggerHidden]
        public static void Equal(string expected, string actual, string message = null, params object[] args)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        [DebuggerHidden]
        public static void Equal<T>(T expected, T actual, string message = null, params object[] args)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        [DebuggerHidden]
        public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message= null, params object[] args)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
        }

        [DebuggerHidden]
        public static void NotEqual<T>(T expected, T actual, string message = null, params object[] args)
        {
            try
            {
                Assert.NotEqual(expected, actual);
            }
            catch (Exception ex)
            {
                if (message == null)
                    throw;

                var msg = message;
                if (args != null && args.Length > 0)
                    msg = string.Format(msg, args);

                throw new LuceneAssertionException(msg, ex);
            }
           
        }


        [DebuggerHidden]
        public static void Ok(bool condition, string message = null, params object[] values)
        {
            if (!string.IsNullOrWhiteSpace(message))
            {
                var exceptionMessage = message;

                if(values != null && values.Length > 0)
                {
                    exceptionMessage = String.Format(exceptionMessage, values);
                }

                Assert.True(condition, exceptionMessage);
            }
            else 
            {
                Assert.True(condition);    
            }
        }

        [DebuggerHidden]
        public static T Throws<T>(Action code) where T : Exception
        {
            return Assert.Throws<T>(code);
        }
        
        #endif
    }
}
