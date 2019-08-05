using System;
using System.Globalization;
using System.Threading;

namespace Lucene.Net.Support
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
    /// Allows switching the current thread to a new culture in a using block that will automatically 
    /// return the culture to its previous state upon completion.
    /// </summary>
    public sealed class CultureContext : IDisposable
    {
#if !NETSTANDARD
        public CultureContext(int culture)
            : this(new CultureInfo(culture), CultureInfo.CurrentUICulture)
        {
        }

        public CultureContext(int culture, int uiCulture)
            : this(new CultureInfo(culture), new CultureInfo(uiCulture))
        {
        }
#endif

        public CultureContext(string cultureName)
            : this(new CultureInfo(cultureName), CultureInfo.CurrentUICulture)
        {
        }

        public CultureContext(string cultureName, string uiCultureName)
            : this(new CultureInfo(cultureName), new CultureInfo(uiCultureName))
        {
        }

        public CultureContext(CultureInfo culture)
            : this(culture, CultureInfo.CurrentUICulture)
        {
        }

        public CultureContext(CultureInfo culture, CultureInfo uiCulture)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");
            if (uiCulture == null)
                throw new ArgumentNullException("uiCulture");

            // Record the current culture settings so they can be restored later.
            this.originalCulture = CultureInfo.CurrentCulture;
            this.originalUICulture = CultureInfo.CurrentUICulture;

            // Set both the culture and UI culture for this context.
#if !NETSTANDARD
            this.currentThread = Thread.CurrentThread;
            currentThread.CurrentCulture = culture;
            currentThread.CurrentUICulture = uiCulture;
#else
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = uiCulture;
#endif
        }

#if !NETSTANDARD
        private readonly Thread currentThread;
#endif
        private readonly CultureInfo originalCulture;
        private readonly CultureInfo originalUICulture;

        public CultureInfo OriginalCulture
        {
            get { return this.originalCulture; }
        }

        public CultureInfo OriginalUICulture
        {
            get { return this.originalUICulture; }
        }

        public void RestoreOriginalCulture()
        {
            // Restore the culture to the way it was before the constructor was called.
#if !NETSTANDARD
            currentThread.CurrentCulture = originalCulture;
            currentThread.CurrentUICulture = originalUICulture;
#else
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
#endif
        }
        public void Dispose()
        {
            RestoreOriginalCulture();
        }
    }
}
