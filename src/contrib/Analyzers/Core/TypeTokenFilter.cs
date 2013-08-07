using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Analysis.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Analysis.Core
{
    public sealed class TypeTokenFilter : FilteringTokenFilter
    {
        private readonly ISet<String> stopTypes;
        private readonly ITypeAttribute typeAttribute; // = addAttribute(TypeAttribute.class);
        private readonly bool useWhiteList;

        public TypeTokenFilter(bool enablePositionIncrements, TokenStream input, ISet<String> stopTypes, bool useWhiteList)
            : base(enablePositionIncrements, input)
        {
            this.stopTypes = stopTypes;
            this.useWhiteList = useWhiteList;
            typeAttribute = AddAttribute<ITypeAttribute>();
        }

        public TypeTokenFilter(bool enablePositionIncrements, TokenStream input, ISet<String> stopTypes)
            : this(enablePositionIncrements, input, stopTypes, false)
        {
        }

        protected override bool Accept()
        {
            return useWhiteList == stopTypes.Contains(typeAttribute.Type);
        }
    }
}
