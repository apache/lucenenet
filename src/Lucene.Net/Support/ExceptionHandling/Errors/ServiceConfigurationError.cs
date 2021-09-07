using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Lucene
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
    /// Error thrown when something goes wrong while loading a service provider.
    /// <para/>
    /// This error will be thrown in the following situations:
    /// <list type="bullet">
    ///     <item><description>The format of a provider-configuration file violates the specification;</description></item>
    ///     <item><description>An <see cref="IOException"/> occurs while reading a provider-configuration file;</description></item>
    ///     <item><description>A concrete provider class named in a provider-configuration file cannot be found;</description></item>
    ///     <item><description>A concrete provider class is not a subclass of the service class;</description></item>
    ///     <item><description>A concrete provider class cannot be instantiated; or</description></item>
    ///     <item><description>Some other kind of error occurs.</description></item>
    /// </list>
    /// <para/>
    /// This is a Java compatibility exception, and should be thrown in
    /// Lucene.NET everywhere Lucene throws it, however catch blocks should
    /// always use the <see cref="ExceptionExtensions.IsServiceConfigurationError(Exception)"/> method.
    /// <code>
    /// catch (Exception ex) when (ex.IsServiceConfigurationError())
    /// </code>
    /// <para/>
    /// Error can be thrown, but cannot be subclassed in C# because it is internal.
    /// For all Lucene exceptions that subclass Error, implement the <see cref="IError"/>
    /// interface, then choose the most logical exception type in .NET to subclass.
    /// </summary>
    // LUCENENET: It is no longer good practice to use binary serialization. 
    // See: https://github.com/dotnet/corefx/issues/23584#issuecomment-325724568
#if FEATURE_SERIALIZABLE_EXCEPTIONS
    [Serializable]
#endif
    internal class ServiceConfigurationError : InvalidOperationException, IError
    {
        [Obsolete("Use ServiceConfigurationError.Create() instead.", error: true)]
        public ServiceConfigurationError()
        {
        }

        [Obsolete("Use ServiceConfigurationError.Create() instead.", error: true)]
        public ServiceConfigurationError(string message) : base(message)
        {
        }

        [Obsolete("Use ServiceConfigurationError.Create() instead.", error: true)]
        public ServiceConfigurationError(string message, Exception innerException) : base(message, innerException)
        {
        }

        [Obsolete("Use ServiceConfigurationError.Create() instead.", error: true)]
        public ServiceConfigurationError(Exception cause)
            : base(cause?.ToString(), cause)
        {
        }

        private ServiceConfigurationError(bool privateOverload)
        {
        }

        private ServiceConfigurationError(string message, bool privateOverload) : base(message)
        {
        }

        private ServiceConfigurationError(string message, Exception innerException, bool privateOverload) : base(message, innerException)
        {
        }

        private ServiceConfigurationError(Exception cause, bool privateOverload)
            : base(cause?.ToString(), cause)
        {
        }

#if FEATURE_SERIALIZABLE_EXCEPTIONS
        /// <summary>
        /// Initializes a new instance of this class with serialized data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected ServiceConfigurationError(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
#endif

        // Static factory methods

        // LUCENENET: Since this exception is only thrown in AnalysisSPILoader, we are simply throwing the internal type.
        // Users can catch it as InvalidOperationException.
        // Since it is possible that AnalysisSPILoader will someday be factored out in favor of true dependency injection,
        // it is not sensible to make a public exception that will be factored out along with it.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create() => new ServiceConfigurationError(privateOverload: true);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message) => new ServiceConfigurationError(message, privateOverload: true);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(string message, Exception innerException) => new ServiceConfigurationError(message, innerException, privateOverload: true);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Exception Create(Exception cause) => new ServiceConfigurationError(cause.Message, cause, privateOverload: true);
    }
}
