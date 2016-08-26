using System;
using System.Globalization;
using System.Threading;

namespace Lucene.Net.Support
{
    /// <summary>
    /// Allows switching the current thread to a new culture in a using block that will automatically 
    /// return the culture to its previous state upon completion.
    /// </summary>
    public class CultureContext : IDisposable
    {
        public CultureContext(int culture)
            : this(new CultureInfo(culture), Thread.CurrentThread.CurrentUICulture)
        {
        }

        public CultureContext(int culture, int uiCulture)
            : this(new CultureInfo(culture), new CultureInfo(uiCulture))
        {
        }

        public CultureContext(string cultureName)
            : this(new CultureInfo(cultureName), Thread.CurrentThread.CurrentUICulture)
        {
        }

        public CultureContext(string cultureName, string uiCultureName)
            : this(new CultureInfo(cultureName), new CultureInfo(uiCultureName))
        {
        }

        public CultureContext(CultureInfo culture)
            : this(culture, Thread.CurrentThread.CurrentUICulture)
        {
        }

        public CultureContext(CultureInfo culture, CultureInfo uiCulture)
        {
            if (culture == null)
                throw new ArgumentNullException("culture");
            if (uiCulture == null)
                throw new ArgumentNullException("uiCulture");

            this.currentThread = Thread.CurrentThread;

            // Record the current culture settings so they can be restored later.
            this.originalCulture = this.currentThread.CurrentCulture;
            this.originalUICulture = this.currentThread.CurrentUICulture;

            // Set both the culture and UI culture for this context.
            this.currentThread.CurrentCulture = culture;
            this.currentThread.CurrentUICulture = uiCulture;
        }

        private readonly Thread currentThread;
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
            this.currentThread.CurrentCulture = this.originalCulture;
            this.currentThread.CurrentUICulture = this.originalUICulture;
        }
        public void Dispose()
        {
            RestoreOriginalCulture();
        }
    }
}
