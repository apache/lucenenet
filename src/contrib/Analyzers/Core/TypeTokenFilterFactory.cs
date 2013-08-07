using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public class TypeTokenFilterFactory : TokenFilterFactory, IResourceLoaderAware
    {
        private readonly bool useWhitelist;
        private readonly bool enablePositionIncrements;
        private readonly String stopTypesFiles;
        private ISet<String> stopTypes;

        public TypeTokenFilterFactory(IDictionary<String, String> args)
            : base(args)
        {
            stopTypesFiles = Require(args, "types");
            enablePositionIncrements = GetBoolean(args, "enablePositionIncrements", false);
            useWhitelist = GetBoolean(args, "useWhitelist", false);
            if (args.Count > 0)
            {
                throw new ArgumentException("Unknown parameters: " + args);
            }
        }

        public void Inform(IResourceLoader loader)
        {
            IList<String> files = SplitFileNames(stopTypesFiles);
            if (files.Count > 0)
            {
                stopTypes = new HashSet<String>();
                foreach (String file in files)
                {
                    IList<String> typesLines = GetLines(loader, file.Trim());
                    stopTypes.UnionWith(typesLines);
                }
            }
        }

        public bool IsEnablePositionIncrements
        {
            get
            {
                return enablePositionIncrements;
            }
        }

        public ISet<String> StopTypes
        {
            get
            {
                return stopTypes;
            }
        }

        public override TokenStream Create(TokenStream input)
        {
            return new TypeTokenFilter(enablePositionIncrements, input, stopTypes, useWhitelist);
        }
    }
}
