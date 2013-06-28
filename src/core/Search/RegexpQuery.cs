using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;

namespace Lucene.Net.Search
{
    public class RegexpQuery : AutomatonQuery
    {
        private sealed class AnonymousDefaultAutomatonProvider
        {
            public override Automaton GetAutomaton(string name)
            {
                return null;
            }
        }

        private static AutomatonProvider defaultProvider = new AnonymousDefaultAutomatonProvider();

        public RegexpQuery(Term term) : this(term, RegExp.ALL) { }

        public RegexpQuery(Term term, int flags) : this(term, flags, defaultProvider) { }

        public RegexpQuery(Term term, int flags, AutomatonProvider provider) : base(term, new RegExp(term.Text, flags).ToAutomaton(provider)) { }

        public override string ToString(string field)
        {
            var buffer = new StringBuilder();
            if (!term.Field.Equals(field))
            {
                buffer.Append(term.Field);
                buffer.Append(":");
            }
            buffer.Append('/');
            buffer.Append(term.Text);
            buffer.Append('/');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }
    }
}
