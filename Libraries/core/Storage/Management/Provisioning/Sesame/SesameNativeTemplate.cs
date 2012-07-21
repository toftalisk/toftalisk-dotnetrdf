/*

Copyright dotNetRDF Project 2009-12
dotnetrdf-develop@lists.sf.net

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.ComponentModel;
#if SILVERLIGHT && !WINDOWS_PHONE
using System.ComponentModel.DataAnnotations;
#endif
using System.Linq;
using System.Text;

namespace VDS.RDF.Storage.Management.Provisioning.Sesame
{
    /// <summary>
    /// Sesame Native index modes
    /// </summary>
    public enum SesameNativeIndexMode
    {
        SPOC,
        POSC
    }

    /// <summary>
    /// Template for creating Sesame Native stores
    /// </summary>
    /// <remarks>
    /// <para>
    /// This template generates a Sesame repository config graph like the following, depending on exact options the graph may differ:
    /// </para>
    /// <pre>
    /// @prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#>.
    /// @prefix rep: <http://www.openrdf.org/config/repository#>.
    /// @prefix sr: <http://www.openrdf.org/config/repository/sail#>.
    /// @prefix sail: <http://www.openrdf.org/config/sail#>.
    /// @prefix ns: <http://www.openrdf.org/config/sail/native#>.
    /// 
    /// [] a rep:Repository ;
    ///    rep:repositoryID "{this.ID}" ;
    ///    rdfs:label "{this.Label}" ;
    ///    rep:repositoryImpl [
    ///       rep:repositoryType "openrdf:SailRepository" ;
    ///       sr:sailImpl [
    ///          sail:sailType "openrdf:NativeStore" ;
    ///          ns:tripleIndexes "{this.IndexMode}"
    ///       ]
    ///    ].
    /// </pre>
    /// <para>
    /// The placeholders of the form <strong>{this.Property}</strong> represent properties of this class whose values will be inserted into the repository config graph and used to create a new store in Sesame.
    /// </para>
    /// </remarks>
    class SesameNativeTemplate
        : BaseSesameTemplate
    {
        public SesameNativeTemplate(String id)
            : base(id, "Sesame Native", "A Sesame native store resides on disk")
        {
            this.IndexMode = SesameNativeIndexMode.SPOC;
        }

        public override IGraph GetTemplateGraph()
        {
            IGraph g = this.GetBaseTemplateGraph();
            INode impl = g.CreateBlankNode();
            g.Assert(this.ContextNode, g.CreateUriNode("rep:repositoryImpl"), impl);
            g.Assert(impl, g.CreateUriNode("rep:repositoryType"), g.CreateLiteralNode("openrdf:SailRepository"));
            INode sailImpl = g.CreateBlankNode();
            g.Assert(impl, g.CreateUriNode("sr:sailImpl"), sailImpl);

            if (this.DirectTypeHierarchyInferencing)
            {
                INode sailDelegate = g.CreateBlankNode();
                g.Assert(sailImpl, g.CreateUriNode("sail:sailType"), g.CreateLiteralNode("openrdf:DirectTypeHierarchyInferencer"));
                g.Assert(sailImpl, g.CreateUriNode("sail:delegate"), sailDelegate);
                sailImpl = sailDelegate;
            }
            if (this.RdfSchemaInferencing)
            {
                INode sailDelegate = g.CreateBlankNode();
                g.Assert(sailImpl, g.CreateUriNode("sail:sailType"), g.CreateLiteralNode("openrdf:ForwardChainingRDFSInferencer"));
                g.Assert(sailImpl, g.CreateUriNode("sail:delegate"), sailDelegate);
                sailImpl = sailDelegate;
            }

            g.Assert(sailImpl, g.CreateUriNode("sail:sailType"), g.CreateLiteralNode("openrdf:NativeStore"));
            String mode = this.IndexMode.ToString().ToLower();
            if (mode.Contains(".")) mode = mode.Substring(mode.LastIndexOf('.') + 1);
            g.Assert(sailImpl, g.CreateUriNode("ns:tripleIndexes"), g.CreateLiteralNode(mode));
            return g;
        }

        /// <summary>
        /// Gets/Sets the Indexing Mode
        /// </summary>
#if !SILVERLIGHT || WINDOWS_PHONE
        [Category("Sesame Configuration"), DisplayName("Triple Indexing Mode"), Description("Sets the indexing mode for the store"), DefaultValue(SesameNativeIndexMode.SPOC)]
#else
        [Category("Sesame Configuration"), Display(Name = "Triple Indexing Mode"), Description("Sets the indexing mode for the store"), DefaultValue(SesameNativeIndexMode.SPOC)]
#endif
        public SesameNativeIndexMode IndexMode
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets whether to enable direct type hierarchy inferencing
        /// </summary>
#if !SILVERLIGHT || WINDOWS_PHONE
        [Category("Sesame Reasoning"), DisplayName("Direct Type Hierarchy Inference"), Description("Enables/Disables Direct Type Hierarchy Inference"), DefaultValue(false)]
#else
        [Category("Sesame Reasoning"), Display(Name = "Direct Type Hierarchy Inference"), Description("Enables/Disables Direct Type Hierarchy Inference"), DefaultValue(false)]
#endif
        public bool DirectTypeHierarchyInferencing
        {
            get;
            set;
        }

        /// <summary>
        /// Gets/Sets whether to enable RDF Schema Inferencing
        /// </summary>
#if !SILVERLIGHT
        [Category("Sesame Reasoning"), DisplayName("RDF Schema Inference"), Description("Enables/Disables RDF Schema inferencing"), DefaultValue(false)]
#else
        [Category("Sesame Reasoning"), Display(Name = "RDF Schema Inference"), Description("Enables/Disables RDF Schema inferencing"), DefaultValue(false)]
#endif
        public bool RdfSchemaInferencing
        {
            get;
            set;
        }
    }
}
