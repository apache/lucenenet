// NamespaceSupport.java - generic Namespace support for SAX.
// http://www.saxproject.org
// Written by David Megginson
// This class is in the Public Domain.  NO WARRANTY!
// $Id: NamespaceSupport.java,v 1.15 2004/04/26 17:34:35 dmegginson Exp $

using J2N.Text;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Sax.Helpers
{
    /// <summary>
    /// Encapsulate Namespace logic for use by applications using SAX,
    /// or internally by SAX drivers.
    /// <para/>
    /// <em>This module, both source code and documentation, is in the
    /// Public Domain, and comes with <strong>NO WARRANTY</strong>.</em>
    /// See <a href='http://www.saxproject.org'>http://www.saxproject.org</a>
    /// for further information.
    /// <para/>
    /// This class encapsulates the logic of Namespace processing: it
    /// tracks the declarations currently in force for each context and
    /// automatically processes qualified XML names into their Namespace
    /// parts; it can also be used in reverse for generating XML qnames
    /// from Namespaces.
    /// <para/>
    /// Namespace support objects are reusable, but the reset method
    /// must be invoked between each session.
    /// <para/>Here is a simple session:
    /// <code>
    ///     string parts[] = new string[3];
    ///     NamespaceSupport support = new NamespaceSupport();
    ///     support.PushContext();
    ///     support.DeclarePrefix("", "http://www.w3.org/1999/xhtml");
    ///     support.DeclarePrefix("dc", "http://www.purl.org/dc#");
    ///     parts = support.ProcessName("p", parts, false);
    ///     Console.WriteLine("Namespace URI: " + parts[0]);
    ///     Console.WriteLine("Local name: " + parts[1]);
    ///     Console.WriteLine("Raw name: " + parts[2]);
    ///     parts = support.ProcessName("dc:title", parts, false);
    ///     Console.WriteLine("Namespace URI: " + parts[0]);
    ///     Console.WriteLine("Local name: " + parts[1]);
    ///     Console.WriteLine("Raw name: " + parts[2]);
    ///     support.PopContext();
    /// </code>
    /// <para/>
    /// Note that this class is optimized for the use case where most
    /// elements do not contain Namespace declarations: if the same
    /// prefix/URI mapping is repeated for each context (for example), this
    /// class will be somewhat less efficient.
    /// <para/>
    /// Although SAX drivers (parsers) may choose to use this class to
    /// implement namespace handling, they are not required to do so.
    /// Applications must track namespace information themselves if they
    /// want to use namespace information.
    /// </summary>
    public class NamespaceSupport
    {
        /// <summary>
        /// The XML Namespace URI as a constant.
        /// The value is <c>http://www.w3.org/XML/1998/namespace</c>
        /// as defined in the "Namespaces in XML" * recommendation.
        /// <para>This is the Namespace URI that is automatically mapped to the "xml" prefix.</para>
        /// </summary>
        public const string XMLNS = "http://www.w3.org/XML/1998/namespace";

        /// <summary>
        /// The namespace declaration URI as a constant.
        /// The value is <c>http://www.w3.org/xmlns/2000/</c>, as defined
        /// in a backwards-incompatible erratum to the "Namespaces in XML"
        /// recommendation.  Because that erratum postdated SAX2, SAX2 defaults
        /// to the original recommendation, and does not normally use this URI.
        /// <para/>
        /// This is the Namespace URI that is optionally applied to
        /// <em>xmlns</em> and <em>xmlns:*</em> attributes, which are used to
        /// declare namespaces.
        /// </summary>
        /// <seealso cref="SetNamespaceDeclUris(bool)" />
        /// <seealso cref="IsNamespaceDeclUris" />
        ////
        public const string NSDECL = "http://www.w3.org/xmlns/2000/";


        ////////////////////////////////////////////////////////////////////
        // Constructor.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create a new Namespace support object.
        /// </summary>
        public NamespaceSupport()
        {
            Reset();
        }

        ////////////////////////////////////////////////////////////////////
        // Context management.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reset this Namespace support object for reuse.
        /// <para/>
        /// It is necessary to invoke this method before reusing the
        /// Namespace support object for a new session.  If namespace
        /// declaration URIs are to be supported, that flag must also
        /// be set to a non-default value.
        /// </summary>
        /// <seealso cref="SetNamespaceDeclUris" />
        public void Reset()
        {
            contexts = new Context[32];
            namespaceDeclUris = false;
            contextPos = 0;
            contexts[contextPos] = currentContext = new Context(this);
            currentContext.DeclarePrefix("xml", XMLNS);
        }

        /// <summary>
        /// Start a new Namespace context.
        /// The new context will automatically inherit
        /// the declarations of its parent context, but it will also keep
        /// track of which declarations were made within this context.
        /// <para/>
        /// Event callback code should start a new context once per element.
        /// This means being ready to call this in either of two places.
        /// For elements that don't include namespace declarations, the
        /// <see cref="IContentHandler.StartElement(string, string, string, IAttributes)"/> callback is the right place.
        /// For elements with such a declaration, it'd done in the first
        /// <see cref="IContentHandler.StartPrefixMapping(string, string)"/> callback.
        /// A boolean flag can be used to
        /// track whether a context has been started yet.  When either of
        /// those methods is called, it checks the flag to see if a new context
        /// needs to be started.  If so, it starts the context and sets the
        /// flag.  After <see cref="IContentHandler.StartElement(string, string, string, IAttributes)"/>
        /// does that, it always clears the flag.
        /// <para/>
        /// Normally, SAX drivers would push a new context at the beginning
        /// of each XML element.  Then they perform a first pass over the
        /// attributes to process all namespace declarations, making
        /// <see cref="IContentHandler.StartPrefixMapping(string, string)"/> callbacks.
        /// Then a second pass is made, to determine the namespace-qualified
        /// names for all attributes and for the element name.
        /// Finally all the information for the
        /// <see cref="IContentHandler.StartElement(string, string, string, IAttributes)"/> callback is available,
        /// so it can then be made.
        /// <para/>
        /// The Namespace support object always starts with a base context
        /// already in force: in this context, only the "xml" prefix is
        /// declared.
        /// </summary>
        /// <seealso cref="IContentHandler" />
        /// <seealso cref="PopContext()" />
        public void PushContext()
        {
            int max = contexts.Length;

            contexts[contextPos].declsOK = false;
            contextPos++;

            // Extend the array if necessary
            if (contextPos >= max)
            {
                var newContexts = new Context[max * 2];
                Arrays.Copy(contexts, 0, newContexts, 0, max);
                //max *= 2; // LUCENENET: IDE0059: Remove unnecessary value assignment
                contexts = newContexts;
            }

            // Allocate the context if necessary.
            currentContext = contexts[contextPos];
            if (currentContext is null)
            {
                contexts[contextPos] = currentContext = new Context(this);
            }

            // Set the parent, if any.
            if (contextPos > 0)
            {
                currentContext.SetParent(contexts[contextPos - 1]);
            }
        }

        /// <summary>
        /// Revert to the previous Namespace context.
        /// <para/>
        /// Normally, you should pop the context at the end of each
        /// XML element.  After popping the context, all Namespace prefix
        /// mappings that were previously in force are restored.
        /// <para/>
        /// You must not attempt to declare additional Namespace
        /// prefixes after popping a context, unless you push another
        /// context first.
        /// </summary>
        /// <seealso cref="PushContext()" />
        public void PopContext()
        {
            contexts[contextPos].Clear();
            contextPos--;
            if (contextPos < 0)
            {
                throw new InvalidOperationException();
            }
            currentContext = contexts[contextPos];
        }

        ////////////////////////////////////////////////////////////////////
        // Operations within a context.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Declare a Namespace prefix.  All prefixes must be declared
        /// before they are referenced.  For example, a SAX driver (parser)
        /// would scan an element's attributes
        /// in two passes:  first for namespace declarations,
        /// then a second pass using <see cref="ProcessName" /> to
        /// interpret prefixes against (potentially redefined) prefixes.
        /// <para/>
        /// This method declares a prefix in the current Namespace
        /// context; the prefix will remain in force until this context
        /// is popped, unless it is shadowed in a descendant context.
        /// <para/>
        /// To declare the default element Namespace, use the empty string as
        /// the prefix.
        /// <para/>
        /// Note that you must <em>not</em> declare a prefix after
        /// you've pushed and popped another Namespace context, or
        /// treated the declarations phase as complete by processing
        /// a prefixed name.
        /// <para/>
        /// Note that there is an asymmetry in this library: <see cref="GetPrefix" /> will not return the "" prefix,
        /// even if you have declared a default element namespace.
        /// To check for a default namespace,
        /// you have to look it up explicitly using <see cref="GetUri" />.
        /// This asymmetry exists to make it easier to look up prefixes
        /// for attribute names, where the default prefix is not allowed.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to declare, or the empty string to
        /// indicate the default element namespace.  This may never have
        /// the value "xml" or "xmlns".
        /// </param>
        /// <param name="uri">
        /// The Namespace URI to associate with the prefix.
        /// </param>
        /// <returns><c>true</c> if the prefix was legal, <c>false</c> otherwise</returns>
        /// <seealso cref="ProcessName(string, string[], bool)" />
        /// <seealso cref="GetUri(string)" />
        /// <seealso cref="GetPrefix(string)" />
        public bool DeclarePrefix(string prefix, string uri)
        {
            if (prefix.Equals("xml", StringComparison.Ordinal) || prefix.Equals("xmlns", StringComparison.Ordinal))
            {
                return false;
            }
            currentContext.DeclarePrefix(prefix, uri);
            return true;
        }

        /// <summary>
        /// Process a raw XML qualified name, after all declarations in the
        /// current context have been handled by <see cref="DeclarePrefix" />.
        /// <para>
        /// This method processes a raw XML qualified name in the
        /// current context by removing the prefix and looking it up among
        /// the prefixes currently declared.  The return value will be the
        /// array supplied by the caller, filled in as follows:
        /// </para>
        /// <list type="table">
        ///     <item>
        ///         <term>parts[0]</term>
        ///         <description>The Namespace URI, or an empty string if none is in use.</description>
        ///     </item>
        ///     <item>
        ///         <term>parts[1]</term>
        ///         <description>The local name (without prefix).</description>
        ///     </item>
        ///     <item>
        ///         <term>parts[2]</term>
        ///         <description>The original raw name.</description>
        ///     </item>
        /// </list>
        /// <para>
        /// All of the strings in the array will be internalized.  If
        /// the raw name has a prefix that has not been declared, then
        /// the return value will be null.
        /// </para>
        /// <para>
        /// Note that attribute names are processed differently than
        /// element names: an unprefixed element name will receive the
        /// default Namespace (if any), while an unprefixed attribute name
        /// will not.
        /// </para>
        /// </summary>
        /// <param name="qName">
        /// The XML qualified name to be processed.
        /// </param>
        /// <param name="parts">
        /// An array supplied by the caller, capable of
        /// holding at least three members.
        /// </param>
        /// <param name="isAttribute">
        /// A flag indicating whether this is an
        /// attribute name (true) or an element name (false).
        /// </param>
        /// <returns>
        /// The supplied array holding three internalized strings
        /// representing the Namespace URI (or empty string), the
        /// local name, and the XML qualified name; or null if there
        /// is an undeclared prefix.
        /// </returns>
        /// <seealso cref="DeclarePrefix" />
        /// <seealso cref="StringExtensions.Intern(string)" />
        public string[] ProcessName(string qName, string[] parts, bool isAttribute)
        {
            string[] myParts = currentContext.ProcessName(qName, isAttribute);
            if (myParts is null)
            {
                return null;
            }
            parts[0] = myParts[0];
            parts[1] = myParts[1];
            parts[2] = myParts[2];
            return parts;
        }

        /// <summary>
        /// Look up a prefix and get the currently-mapped Namespace URI.
        /// <para>
        /// This method looks up the prefix in the current context.
        /// Use the empty string ("") for the default Namespace.
        /// </para>
        /// </summary>
        /// <param name="prefix">
        /// The prefix to look up.
        /// </param>
        /// <returns>
        /// The associated Namespace URI, or null if the prefix
        /// is undeclared in this context.
        /// </returns>
        /// <seealso cref="GetPrefix" />
        /// <seealso cref="GetPrefixes()" />
        public string GetUri(string prefix)
        {
            return currentContext.GetURI(prefix);
        }

        /// <summary>
        /// Return an enumeration of all prefixes whose declarations are
        /// active in the current context.
        /// This includes declarations from parent contexts that have
        /// not been overridden.
        /// <para>
        /// <strong>Note:</strong> if there is a default prefix, it will not be
        /// returned in this enumeration; check for the default prefix
        /// using the <see cref="GetUri" /> with an argument of "".
        /// </para>
        /// </summary>
        /// <returns>An enumeration of prefixes (never empty).</returns>
        /// <seealso cref="GetDeclaredPrefixes" />
        /// <seealso cref="GetUri" />
        public IEnumerable GetPrefixes()
        {
            return currentContext.GetPrefixes();
        }

        /// <summary>
        /// Return one of the prefixes mapped to a Namespace URI.
        /// <para>
        /// If more than one prefix is currently mapped to the same
        /// URI, this method will make an arbitrary selection; if you
        /// want all of the prefixes, use the <see cref="GetPrefixes()" />
        /// method instead.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> this will never return the empty (default) prefix;
        /// to check for a default prefix, use the <see cref="GetUri" />
        /// method with an argument of "".
        /// </para>
        /// </summary>
        /// <param name="uri">
        /// the namespace URI
        /// </param>
        /// <returns>
        /// one of the prefixes currently mapped to the URI supplied,
        /// or null if none is mapped or if the URI is assigned to
        /// the default namespace
        /// </returns>
        /// <seealso cref="GetPrefixes(string)" />
        /// <seealso cref="GetUri" />
        public string GetPrefix(string uri)
        {
            return currentContext.GetPrefix(uri);
        }

        /// <summary>
        /// Return an enumeration of all prefixes for a given URI whose
        /// declarations are active in the current context.
        /// This includes declarations from parent contexts that have
        /// not been overridden.
        /// <para>
        /// This method returns prefixes mapped to a specific Namespace
        /// URI.  The xml: prefix will be included.  If you want only one
        /// prefix that's mapped to the Namespace URI, and you don't care
        /// which one you get, use the <see cref="GetPrefix" />
        /// method instead.
        /// </para>
        /// <para>
        /// <strong>Note:</strong> the empty (default) prefix is <em>never</em> included
        /// in this enumeration; to check for the presence of a default
        /// Namespace, use the <see cref="GetUri" /> method with an
        /// argument of "".
        /// </para>
        /// </summary>
        /// <param name="uri">
        /// The Namespace URI.
        /// </param>
        /// <returns>An enumeration of prefixes (never empty).</returns>
        /// <seealso cref="GetPrefix" />
        /// <seealso cref="GetDeclaredPrefixes" />
        /// <seealso cref="GetUri" />
        public IEnumerable GetPrefixes(string uri)
        {
            var prefixes = new ArrayList();
            // LUCENENET NOTE: IEnumerator is not disposable
            IEnumerator allPrefixes = GetPrefixes().GetEnumerator();
            while (allPrefixes.MoveNext())
            {
                var prefix = (string)allPrefixes.Current;
                if (uri.Equals(GetUri(prefix), StringComparison.Ordinal))
                {
                    prefixes.Add(prefix);
                }
            }
            return prefixes;
        }

        /// <summary>
        /// Return an enumeration of all prefixes declared in this context.
        /// <para>
        /// The empty (default) prefix will be included in this
        /// enumeration; note that this behaviour differs from that of
        /// <see cref="GetPrefix" /> and <see cref="GetPrefixes()" />.
        /// </para>
        /// </summary>
        /// <returns>
        /// An enumeration of all prefixes declared in this
        /// context.
        /// </returns>
        /// <seealso cref="GetPrefixes()" />
        /// <seealso cref="GetUri" />
        public IEnumerable GetDeclaredPrefixes()
        {
            return currentContext.GetDeclaredPrefixes();
        }

        /// <summary>
        /// Controls whether namespace declaration attributes are placed
        /// into the <see cref="NSDECL" /> namespace
        /// by <see cref="ProcessName" />.  This may only be
        /// changed before any contexts have been pushed.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// when attempting to set this
        /// after any context has been pushed.
        /// </exception>
        public void SetNamespaceDeclUris(bool value)
        {
            if (contextPos != 0)
            {
                throw new InvalidOperationException();
            }
            if (value == namespaceDeclUris)
            {
                return;
            }
            namespaceDeclUris = value;
            if (value)
            {
                currentContext.DeclarePrefix("xmlns", NSDECL);
            }
            else
            {
                contexts[contextPos] = currentContext = new Context(this);
                currentContext.DeclarePrefix("xml", XMLNS);
            }
        }

        /// <summary>
        /// Returns true if namespace declaration attributes are placed into
        /// a namespace.  This behavior is not the default.
        /// </summary>
        public bool IsNamespaceDeclUris => namespaceDeclUris;

        ////////////////////////////////////////////////////////////////////
        // Internal state.
        ////////////////////////////////////////////////////////////////////
        
        private Context[] contexts;
        private Context currentContext;
        private int contextPos;
        private bool namespaceDeclUris;

        ////////////////////////////////////////////////////////////////////
        // Internal classes.
        ////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Internal class for a single Namespace context.
        /// </summary>
        /// <remarks>
        /// This module caches and reuses Namespace contexts,
        /// so the number allocated
        /// will be equal to the element depth of the document, not to the total
        /// number of elements (i.e. 5-10 rather than tens of thousands).
        /// Also, data structures used to represent contexts are shared when
        /// possible (child contexts without declarations) to further reduce
        /// the amount of memory that's consumed.
        /// </remarks>
        internal sealed class Context
        {
            private readonly NamespaceSupport outerInstance;

            /// <summary>
            /// Create the root-level Namespace context.
            /// </summary>
            public Context(NamespaceSupport outerInstance)
            {
                this.outerInstance = outerInstance;
                CopyTables();
            }

            /// <summary>
            /// (Re)set the parent of this Namespace context.
            /// The context must either have been freshly constructed,
            /// or must have been cleared.
            /// </summary>
            /// <param name="parent">The parent Namespace context object.</param>
            public void SetParent(Context parent)
            {
                //this.parent = parent; // LUCENENET: Not used
                declarations = null;
                prefixTable = parent.prefixTable;
                uriTable = parent.uriTable;
                elementNameTable = parent.elementNameTable;
                attributeNameTable = parent.attributeNameTable;
                defaultNs = parent.defaultNs;
                declSeen = false;
                declsOK = true;
            }

            /// <summary>
            /// Makes associated state become collectible,
            /// invalidating this context.
            /// <see cref="SetParent(Context)"/> must be called before
            /// this context may be used again.
            /// </summary>
            public void Clear()
            {
                //parent = null; // LUCENENET: Not used
                prefixTable = null;
                uriTable = null;
                elementNameTable = null;
                attributeNameTable = null;
                defaultNs = null;
            }

            /// <summary>
            /// Declare a Namespace prefix for this context.
            /// </summary>
            /// <param name="prefix">The prefix to declare.</param>
            /// <param name="uri">The associated Namespace URI.</param>
            /// <seealso cref="DeclarePrefix(string, string)"/>
            public void DeclarePrefix(string prefix, string uri)
            {
                // Lazy processing...
                if (!declsOK)
                {
                    throw new InvalidOperationException("can't declare any more prefixes in this context");
                }
                if (!declSeen)
                {
                    CopyTables();
                }
                if (declarations is null)
                {
                    declarations = new JCG.List<string>();
                }

                prefix = prefix.Intern();
                uri = uri.Intern();
                if ("".Equals(prefix, StringComparison.Ordinal))
                {
                    if ("".Equals(uri, StringComparison.Ordinal))
                    {
                        defaultNs = null;
                    }
                    else
                    {
                        defaultNs = uri;
                    }
                }
                else
                {
                    prefixTable.Add(prefix, uri);
                    uriTable.Add(uri, prefix); // may wipe out another prefix
                }
                declarations.Add(prefix);
            }

            /// <summary>
            /// Process an XML qualified name in this context.
            /// </summary>
            /// <param name="qName">The XML qualified name.</param>
            /// <param name="isAttribute">true if this is an attribute name.</param>
            /// <returns>An array of three strings containing the
            /// URI part (or empty string), the local part,
            /// and the raw name, all internalized, or null
            /// if there is an undeclared prefix.</returns>
            /// <seealso cref="DeclarePrefix(string, string)"/>
            internal string[] ProcessName(string qName, bool isAttribute)
            {
                string[] name;
                IDictionary<string, string[]> table;

                // detect errors in call sequence
                declsOK = false;

                // Select the appropriate table.
                if (isAttribute)
                {
                    table = attributeNameTable;
                }
                else
                {
                    table = elementNameTable;
                }

                // Start by looking in the cache, and
                // return immediately if the name
                // is already known in this content
                if (table.TryGetValue(qName, out string[] value))
                {
                    return value;
                }

                // We haven't seen this name in this
                // context before.  Maybe in the parent
                // context, but we can't assume prefix
                // bindings are the same.
                name = new string[3];
                name[2] = qName.Intern();
                int index = qName.IndexOf(':');

                // No prefix.
                if (index == -1)
                {
                    if (isAttribute)
                    {
                        if (qName == "xmlns" && outerInstance.namespaceDeclUris)
                        {
                            name[0] = NSDECL;
                        }
                        else
                        {
                            name[0] = "";
                        }
                    }
                    else if (defaultNs is null)
                    {
                        name[0] = "";
                    }
                    else
                    {
                        name[0] = defaultNs;
                    }
                    name[1] = name[2];
                }

                // Prefix
                else
                {
                    string prefix = qName.Substring(0, index);
                    string local = qName.Substring(index + 1);
                    string uri = null;
                    if ("".Equals(prefix, StringComparison.Ordinal))
                    {
                        uri = defaultNs;
                    }
                    else if (prefixTable.ContainsKey(prefix))
                    {
                        uri = (string)prefixTable[prefix];
                    }
                    if (uri is null || (!isAttribute && "xmlns".Equals(prefix, StringComparison.Ordinal)))
                    {
                        return null;
                    }
                    name[0] = uri;
                    name[1] = local.Intern();
                }

                // Save in the cache for future use.
                // (Could be shared with parent context...)
                table.Add(name[2], name);
                return name;
            }

            /// <summary>
            /// Look up the URI associated with a prefix in this context.
            /// </summary>
            /// <param name="prefix">The prefix to look up.</param>
            /// <returns>The associated Namespace URI, or null if none is declared.</returns>
            /// <seealso cref="NamespaceSupport.GetUri(string)"/>
            internal string GetURI(string prefix)
            {
                if ("".Equals(prefix, StringComparison.Ordinal))
                {
                    return defaultNs;
                }
                if (prefixTable is null)
                {
                    return null;
                }
                if (prefixTable.ContainsKey(prefix))
                {
                    return (string)prefixTable[prefix];
                }
                return null;
            }

            /// <summary>
            /// Look up one of the prefixes associated with a URI in this context.
            /// <para/>
            /// Since many prefixes may be mapped to the same URI, the return value may be unreliable.
            /// </summary>
            /// <param name="uri">The URI to look up.</param>
            /// <returns>The associated prefix, or null if none is declared.</returns>
            /// <seealso cref="NamespaceSupport.GetPrefix(string)"/>
            internal string GetPrefix(string uri)
            {
                if (uriTable is null)
                {
                    return null;
                }
                if (uriTable.ContainsKey(uri))
                {
                    return (string)uriTable[uri];
                }
                return null;
            }

            /// <summary>
            /// Return an enumeration of prefixes declared in this context.
            /// </summary>
            /// <returns>An enumeration of prefixes (possibly empty).</returns>
            /// <seealso cref="NamespaceSupport.GetDeclaredPrefixes()"/>
            internal IEnumerable GetDeclaredPrefixes()
            {
                if (declarations is null)
                {
                    return Collections.EmptyList<object>();
                }
                return declarations;
            }

            /// <summary>
            /// Return an enumeration of all prefixes currently in force.
            /// <para/>
            /// The default prefix, if in force, is <em>not</em>
            /// returned, and will have to be checked for separately.
            /// </summary>
            /// <returns>An enumeration of prefixes (never empty).</returns>
            /// <seealso cref="NamespaceSupport.GetPrefixes()"/>
            internal IEnumerable GetPrefixes()
            {
                if (prefixTable is null)
                {
                    return Collections.EmptyList<object>();
                }
                return prefixTable.Keys;
            }

            ////////////////////////////////////////////////////////////////
            // Internal methods.
            ////////////////////////////////////////////////////////////////

            /// <summary>
            /// Copy on write for the internal tables in this context.
            /// <para/>
            /// This class is optimized for the normal case where most
            /// elements do not contain Namespace declarations.
            /// </summary>
            private void CopyTables()
            {
                if (prefixTable != null)
                {
                    prefixTable = (Hashtable)prefixTable.Clone();
                }
                else
                {
                    prefixTable = new Hashtable();
                }
                if (uriTable != null)
                {
                    uriTable = (Hashtable)uriTable.Clone();
                }
                else
                {
                    uriTable = new Hashtable();
                }
                elementNameTable = new Dictionary<string, string[]>();
                attributeNameTable = new Dictionary<string, string[]>();
                declSeen = true;
            }

            ////////////////////////////////////////////////////////////////
            // Protected state.
            ////////////////////////////////////////////////////////////////

            private Hashtable prefixTable;
            private Hashtable uriTable;
            private IDictionary<string, string[]> elementNameTable;
            private IDictionary<string, string[]> attributeNameTable;
            private string defaultNs;
            internal bool declsOK = true;

            ////////////////////////////////////////////////////////////////
            // Internal state.
            ////////////////////////////////////////////////////////////////

            private IList<string> declarations;
            private bool declSeen;
            //private Context parent; // LUCENENET: Not used
        }
    }
}
