using Lucene.Net.Util;

namespace Lucene.Net.Replicator
{
    /// <summary>
    /// Wraps a InfoStream for a specific component.
    /// This is intented to make it a little easier to work with the InfoStreams.
    /// </summary>
    /// <remarks>
    /// .NET Specific
    /// </remarks>
    public sealed class ComponentWrapperInfoStream : InfoStream
    {
        private readonly string component;
        private readonly InfoStream innerStream;

        public ComponentWrapperInfoStream(string component, InfoStream innerStream)
        {
            this.component = component;
            this.innerStream = innerStream;
        }

        public override void Message(string component, string message)
        {
            if (IsEnabled(component))
                innerStream.Message(component, message);
        }

        public bool IsEnabled()
        {
            return IsEnabled(component);
        }

        public override bool IsEnabled(string component)
        {
            return innerStream.IsEnabled(component);
        }

        public override object Clone()
        {
            return new ComponentWrapperInfoStream(component, (InfoStream)innerStream.Clone());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                innerStream.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}