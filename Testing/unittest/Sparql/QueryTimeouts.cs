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
using VDS.RDF.Query.Datasets;
using VDS.RDF.Writing.Formatting;

namespace VDS.RDF.Test.Sparql
{
    [TestClass]
    public class QueryTimeouts
    {
        private long[] _timeouts = new long[] { 50, 100, 250, 500, 1000 };
        private SparqlQueryParser _parser = new SparqlQueryParser();

        private ISparqlDataset AsDataset(IInMemoryQueryableStore store)
        {
            if (store.Graphs.Count == 1)
            {
                return new InMemoryDataset(store, store.Graphs.First().BaseUri);
            }
            else
            {
                return new InMemoryDataset(store);
            }
        }

        private void TestProductTimeout(IGraph data, String query, bool useGlobal, int expectedResults)
        {
            Console.WriteLine("Maximum Expected Results: " + expectedResults);
            Console.WriteLine("Initial Global Timeout: " + Options.QueryExecutionTimeout);
            Console.WriteLine();

            long globalOrig = Options.QueryExecutionTimeout;
            try
            {
                if (useGlobal)
                {
                    Console.WriteLine("Global Timeout setting in use");
                }
                else
                {
                    Console.WriteLine("Per Query Timeout setting in use");
                }
                Console.WriteLine();

                TripleStore store = new TripleStore();
                store.Add(data);

                SparqlQuery q = this._parser.ParseFromString(query);
                LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));

                SparqlFormatter formatter = new SparqlFormatter();
                Console.WriteLine("Query:");
                Console.WriteLine(formatter.Format(q));

                //Evaluate for each Timeout
                foreach (long t in this._timeouts)
                {
                    //Set the Timeout and ask for Partial Results
                    if (useGlobal)
                    {
                        Options.QueryExecutionTimeout = t;
                    }
                    else
                    {
                        q.Timeout = t;
                    }
                    q.PartialResultsOnTimeout = true;

                    //Check that the reported Timeout matches the expected
                    SparqlEvaluationContext context = new SparqlEvaluationContext(q, null);
                    long expected;
                    if (useGlobal)
                    {
                        expected = t;
                    }
                    else
                    {
                        if (Options.QueryExecutionTimeout > 0 && t <= Options.QueryExecutionTimeout)
                        {
                            expected = t;
                        }
                        else if (Options.QueryExecutionTimeout == 0)
                        {
                            expected = t;
                        }
                        else
                        {
                            expected = Options.QueryExecutionTimeout;
                        }
                    }
                    Assert.AreEqual(expected, context.QueryTimeout, "Reported Timeout not as expected");

                    //Run the Query
                    Object results = processor.ProcessQuery(q);
                    if (results is SparqlResultSet)
                    {
                        SparqlResultSet rset = (SparqlResultSet)results;

                        Console.WriteLine("Requested Timeout: " + t + " - Actual Timeout: " + expected + "ms - Results: " + rset.Count + " - Query Time: " + q.QueryExecutionTime);
                        Assert.IsTrue(rset.Count <= expectedResults, "Results should be <= expected");
                    }
                    else
                    {
                        Assert.Fail("Did not get a Result Set as expected");
                    }
                }
            }
            finally
            {
                Options.QueryExecutionTimeout = globalOrig;
            }
        }

        private void TestProductTimeoutGlobalOverride(IGraph data, String query, long globalTimeout, int expectedResults)
        {
            long origGlobal = Options.QueryExecutionTimeout;
            try
            {
                Options.QueryExecutionTimeout = globalTimeout;
                this.TestProductTimeout(data, query, false, expectedResults);
            }
            finally
            {
                Options.QueryExecutionTimeout = origGlobal;
            }
        }

        [TestMethod]
        public void SparqlQueryTimeout()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . ?s ?p2 ?o2}";
            SparqlQuery q = this._parser.ParseFromString(query);
            q.Timeout = 1;

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
            store.Add(g);
            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            try
            {
                processor.ProcessQuery(q);
                Assert.Fail("Did not throw a RdfQueryTimeoutException as expected");
            }
            catch (RdfQueryTimeoutException timeoutEx)
            {
                TestTools.ReportError("Timeout", timeoutEx);

                Console.WriteLine();
                Console.WriteLine("Execution Time: " + q.QueryExecutionTime.Value.ToString());
            }
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProduct()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z }";
            this.TestProductTimeout(g, query, false, g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProduct2()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z . ?a ?b ?c }";
            this.TestProductTimeout(g, query, false, g.Triples.Count * g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutGlobalDuringProduct()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z }";
            this.TestProductTimeout(g, query, true, g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutGlobalDuringProduct2()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z . ?a ?b ?c }";
            this.TestProductTimeout(g, query, true, g.Triples.Count * g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProductOverriddenByGlobal()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z }";
            this.TestProductTimeoutGlobalOverride(g, query, 1000, g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProductOverriddenByGlobal2()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z . ?a ?b ?c }";
            this.TestProductTimeoutGlobalOverride(g, query, 1000, g.Triples.Count * g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProductNotOverriddenByGlobal()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z }";
            this.TestProductTimeoutGlobalOverride(g, query, 0, g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProductNotOverriddenByGlobal2()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z . ?a ?b ?c }";
            this.TestProductTimeoutGlobalOverride(g, query, 0, g.Triples.Count * g.Triples.Count * g.Triples.Count);
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProductLazy()
        {
            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z } LIMIT 5000";
            SparqlQuery q = this._parser.ParseFromString(query);
            q.Timeout = 100;

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
            store.Add(g);
            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            try
            {
                processor.ProcessQuery(q);
                Assert.Fail("Did not throw a RdfQueryTimeoutException as expected");
            }
            catch (RdfQueryTimeoutException timeoutEx)
            {
                TestTools.ReportError("Timeout", timeoutEx);

                Console.WriteLine();
                Console.WriteLine("Execution Time: " + q.QueryExecutionTime.Value.ToString());
            }
        }

        [TestMethod]
        public void SparqlQueryTimeoutDuringProductLazy2()
        {
            String query = "ASK WHERE { ?s ?p ?o . ?x ?y ?z }";
            SparqlQuery q = this._parser.ParseFromString(query);
            q.Timeout = 1;

            TripleStore store = new TripleStore();
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");
            store.Add(g);
            LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
            try
            {
                processor.ProcessQuery(q);
                Assert.Fail("Did not throw a RdfQueryTimeoutException as expected");
            }
            catch (RdfQueryTimeoutException timeoutEx)
            {
                TestTools.ReportError("Timeout", timeoutEx);

                Console.WriteLine();
                Console.WriteLine("Execution Time: " + q.QueryExecutionTime.Value.ToString());
            }
        }

        [TestMethod]
        public void SparqlQueryTimeoutNone()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z }";

            long currTimeout = Options.QueryExecutionTimeout;
            try
            {
                Options.QueryExecutionTimeout = 0;

                SparqlQuery q = this._parser.ParseFromString(query);
                TripleStore store = new TripleStore();
                store.Add(g);

                LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
                Object results = processor.ProcessQuery(q);
                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    Console.WriteLine("Results: " + rset.Count + " - Query Time: " + q.QueryExecutionTime);
                    Assert.AreEqual(g.Triples.Count * g.Triples.Count, rset.Count);
                }
                else
                {
                    Assert.Fail("Did not get a Result Set as expected");
                }
            }
            finally
            {
                Options.QueryExecutionTimeout = currTimeout;
            }
        }

        [TestMethod]
        public void SparqlQueryTimeoutMinimal()
        {
            Graph g = new Graph();
            g.LoadFromEmbeddedResource("VDS.RDF.Configuration.configuration.ttl");

            String query = "SELECT * WHERE { ?s ?p ?o . ?x ?y ?z }";

            long currTimeout = Options.QueryExecutionTimeout;
            try
            {
                Options.QueryExecutionTimeout = 1;

                SparqlQuery q = this._parser.ParseFromString(query);
                q.PartialResultsOnTimeout = true;
                TripleStore store = new TripleStore();
                store.Add(g);

                LeviathanQueryProcessor processor = new LeviathanQueryProcessor(AsDataset(store));
                Object results = processor.ProcessQuery(q);
                if (results is SparqlResultSet)
                {
                    SparqlResultSet rset = (SparqlResultSet)results;
                    Console.WriteLine("Results: " + rset.Count + " - Query Time: " + q.QueryExecutionTime);
                    Assert.IsTrue(rset.Count < (g.Triples.Count * g.Triples.Count));
                }
                else
                {
                    Assert.Fail("Did not get a Result Set as expected");
                }
            }
            finally
            {
                Options.QueryExecutionTimeout = currTimeout;
            }
        }
    }
}
