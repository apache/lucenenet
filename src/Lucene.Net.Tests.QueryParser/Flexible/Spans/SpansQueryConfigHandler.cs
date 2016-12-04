using Lucene.Net.QueryParsers.Flexible.Core.Config;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// This query config handler only adds the <see cref="IUniqueFieldAttribute"/> to it.
    /// <para/>
    /// It does not return any configuration for a field in specific.
    /// </summary>
    public class SpansQueryConfigHandler : QueryConfigHandler
    {
        public readonly static ConfigurationKey<string> UNIQUE_FIELD = ConfigurationKey.NewInstance<string>();

        public SpansQueryConfigHandler()
        {
            // empty constructor
        }


        public override FieldConfig GetFieldConfig(string fieldName)
        {

            // there is no field configuration, always return null
            return null;

        }
    }
}
