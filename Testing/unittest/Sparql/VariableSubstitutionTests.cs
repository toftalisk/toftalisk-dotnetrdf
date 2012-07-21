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
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VDS.RDF.Parsing;
using VDS.RDF.Query;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Expressions;
using VDS.RDF.Query.Expressions.Primary;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Query.Optimisation;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Test.Sparql
{
    [TestClass]
    public class VariableSubstitutionTests
    {
        private SparqlQueryParser _parser = new SparqlQueryParser();
        private SparqlFormatter _formatter = new SparqlFormatter(new NamespaceMapper());
        private NodeFactory _factory = new NodeFactory();

        private void TestSubstitution(SparqlQuery q, String findVar, String replaceVar, IEnumerable<String> expected, IEnumerable<String> notExpected)
        {
            Console.WriteLine("Input Query:");
            Console.WriteLine(this._formatter.Format(q));
            Console.WriteLine();

            ISparqlAlgebra algebra = q.ToAlgebra();
            VariableSubstitutionTransformer transformer = new VariableSubstitutionTransformer(findVar, replaceVar);
            try
            {
                ISparqlAlgebra resAlgebra = transformer.Optimise(algebra);
                algebra = resAlgebra;
            }
            catch (Exception ex)
            {
                //Ignore errors
                Console.WriteLine("Error Transforming - " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
            }

            SparqlQuery resQuery = algebra.ToQuery();
            String resText = this._formatter.Format(resQuery);
            Console.WriteLine("Resulting Query:");
            Console.WriteLine(resText);
            Console.WriteLine();

            foreach (String x in expected)
            {
                Assert.IsTrue(resText.Contains(x), "Expected Transformed Query to contain string '" + x + "'");
            }
            foreach (String x in notExpected)
            {
                Assert.IsFalse(resText.Contains(x), "Transformed Query contained string '" + x + "' which was expected to have been transformed");
            }
        }

        private void TestSubstitution(SparqlQuery q, String findVar, INode replaceTerm, IEnumerable<String> expected, IEnumerable<String> notExpected)
        {
            Console.WriteLine("Input Query:");
            Console.WriteLine(this._formatter.Format(q));
            Console.WriteLine();

            ISparqlAlgebra algebra = q.ToAlgebra();
            VariableSubstitutionTransformer transformer = new VariableSubstitutionTransformer(findVar, replaceTerm);
            try
            {
                ISparqlAlgebra resAlgebra = transformer.Optimise(algebra);
                algebra = resAlgebra;
            }
            catch (Exception ex)
            {
                //Ignore errors
                Console.WriteLine("Error Transforming - " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
            }

            SparqlQuery resQuery = algebra.ToQuery();
            String resText = this._formatter.Format(resQuery);
            Console.WriteLine("Resulting Query:");
            Console.WriteLine(resText);
            Console.WriteLine();

            foreach (String x in expected)
            {
                Assert.IsTrue(resText.Contains(x), "Expected Transformed Query to contain string '" + x + "'");
            }
            foreach (String x in notExpected)
            {
                Assert.IsFalse(resText.Contains(x), "Transformed Query contained string '" + x + "' which was not expected");
            }
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub1()
        {
            String query = "SELECT * WHERE { ?s ?p ?o }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub2()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . ?s a ?type }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub3()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . FILTER(ISBLANK(?s)) }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub4()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . BIND(ISBLANK(?s) AS ?blank) }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub5()
        {
            try
            {
                this._parser.SyntaxMode = SparqlQuerySyntax.Extended;

                String query = "SELECT * WHERE { ?s ?p ?o . LET (?blank := ISBLANK(?s)) }";
                SparqlQuery q = this._parser.ParseFromString(query);
                this.TestSubstitution(q, "s", "x", new String[] { "?x" }, new String[] { "?s" });
            }
            finally
            {
                this._parser.SyntaxMode = SparqlQuerySyntax.Sparql_1_1;
            }
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub6()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . FILTER(EXISTS { ?s a ?type }) }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub7()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . { ?s a ?type } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub8()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . OPTIONAL { ?s a ?type } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x", "OPTIONAL" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub9()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . GRAPH <http://example.org/graph> { ?s a ?type } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x", "GRAPH" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub10()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . MINUS { ?s a ?type } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x", "MINUS" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub11()
        {
            String query = "SELECT * WHERE { { ?s ?p ?o . } UNION { ?s a ?type } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?x", "UNION" }, new String[] { "?s" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSub12()
        {
            String query = "SELECT * WHERE { GRAPH ?g { ?s ?p ?o } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "g", "x", new String[] { "?x", "GRAPH" }, new String[] { "?g" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub1()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . }";
            SparqlQuery q = this._parser.ParseFromString(query);
            NodeFactory factory = new NodeFactory();
            this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub2()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . ?o ?x ?y }";
            SparqlQuery q = this._parser.ParseFromString(query);
            NodeFactory factory = new NodeFactory();
            this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub3()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . FILTER(ISURI(?o)) }";
            SparqlQuery q = this._parser.ParseFromString(query);
            NodeFactory factory = new NodeFactory();
            this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub4()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . BIND(ISURI(?o) AS ?uri) }";
            SparqlQuery q = this._parser.ParseFromString(query);
            NodeFactory factory = new NodeFactory();
            this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub5()
        {
            try
            {
                this._parser.SyntaxMode = SparqlQuerySyntax.Extended;

                String query = "SELECT * WHERE { ?s ?p ?o . LET (?uri := ISURI(?o))}";
                SparqlQuery q = this._parser.ParseFromString(query);
                NodeFactory factory = new NodeFactory();
                this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>" }, new String[] { "?o" });
            }
            finally
            {
                this._parser.SyntaxMode = SparqlQuerySyntax.Sparql_1_1;
            }
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub6()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . OPTIONAL { ?o ?x ?y } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            NodeFactory factory = new NodeFactory();
            this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>", "OPTIONAL" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub7()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . GRAPH <http://example.org/graph> { ?o ?x ?y } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            NodeFactory factory = new NodeFactory();
            this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>", "GRAPH" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub8()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . MINUS { ?o ?x ?y } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            NodeFactory factory = new NodeFactory();
            this.TestSubstitution(q, "o", factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>", "MINUS" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub9()
        {
            String query = "SELECT * WHERE { { ?s ?p ?o .} UNION { ?o ?x ?y } }";
            SparqlQuery q = this._parser.ParseFromString(query);

            this.TestSubstitution(q, "o", this._factory.CreateUriNode(UriFactory.Create("http://example.org/object")), new String[] { "<http://example.org/object>", "UNION" }, new String[] { "?o" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraTermSub10()
        {
            String query = "SELECT * WHERE { GRAPH ?g { ?s ?p ?o } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "g", this._factory.CreateUriNode(UriFactory.Create("http://example.org/graph")), new String[] { "<http://example.org/graph>", "GRAPH" }, new String[] { "?g" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSubBad1()
        {
            String query = "SELECT * WHERE { ?s <http://predicate>+ ?o }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?s", "+" }, new String[] { "?x" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSubBad2()
        {
            String query = "SELECT * WHERE { { SELECT * WHERE { ?s ?p ?o } } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?s" }, new String[] { "?x" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSubBad3()
        {
            String query = "SELECT * WHERE { SERVICE <http://example.org/sparql> { ?s ?p ?o } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "s", "x", new String[] { "?s", "SERVICE" }, new String[] { "?x" });
        }

        [TestMethod]
        public void SparqlOptimiserAlgebraVarSubBad4()
        {
            String query = "SELECT * WHERE { GRAPH ?g { ?s ?p ?o } }";
            SparqlQuery q = this._parser.ParseFromString(query);
            this.TestSubstitution(q, "g", this._factory.CreateLiteralNode("graph"), new String[] { "?g" }, new String[] { "\"graph\"" });
        }
    }
}
