using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Util
{
    public abstract class TokenFilterFactory : AbstractAnalysisFactory
    {
        private static readonly AnalysisSPILoader<TokenFilterFactory> loader =
            new AnalysisSPILoader<TokenFilterFactory>(typeof(TokenFilterFactory),
                new String[] { "TokenFilterFactory", "FilterFactory" });

        public static TokenFilterFactory ForName(String name, IDictionary<String, String> args)
        {
            return loader.NewInstance(name, args);
        }

        public static Type LookupClass(String name)
        {
            return loader.LookupClass(name);
        }

        public static ICollection<String> AvailableTokenFilters
        {
            get
            {
                return loader.AvailableServices;
            }
        }

        public static void ReloadTokenFilters()
        {
            loader.Reload();
        }

        protected TokenFilterFactory(IDictionary<String, String> args)
            : base(args)
        {
        }

        public abstract TokenStream Create(TokenStream input);
    }
}
