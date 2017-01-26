using Lucene.Net.Queries.Function;

namespace Lucene.Net.Expressions
{
	/// <summary>Binds variable names in expressions to actual data.</summary>
	/// <remarks>
	/// Binds variable names in expressions to actual data.
	/// <p>
	/// These are typically DocValues fields/FieldCache, the document's
	/// relevance score, or other ValueSources.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class Bindings
	{
		/// <summary>Sole constructor.</summary>
		/// <remarks>
		/// Sole constructor. (For invocation by subclass
		/// constructors, typically implicit.)
		/// </remarks>
		protected Bindings()
		{
		}

		/// <summary>Returns a ValueSource bound to the variable name.</summary>
		
		public abstract ValueSource GetValueSource(string name);

		/// <summary>
		/// Returns a
		/// <code>ValueSource</code>
		/// over relevance scores
		/// </summary>
		protected ValueSource GetScoreValueSource()
		{
			return new ScoreValueSource();
		}
	}
}
