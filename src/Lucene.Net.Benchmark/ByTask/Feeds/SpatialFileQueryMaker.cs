using Lucene.Net.Benchmarks.ByTask.Utils;
using Lucene.Net.Queries;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Spatial;
using Lucene.Net.Spatial.Queries;
using Spatial4n.Shapes;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Benchmarks.ByTask.Feeds
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Reads spatial data from the body field docs from an internally created <see cref="LineDocSource"/>.
    /// It's parsed by <see cref="Spatial4n.Context.SpatialContext.ReadShapeFromWkt(string)"/> and then
    /// further manipulated via a configurable <see cref="IShapeConverter"/>. When using point
    /// data, it's likely you'll want to configure the shape converter so that the query shapes actually
    /// cover a region. The queries are all created &amp; cached in advance. This query maker works in
    /// conjunction with <see cref="SpatialDocMaker"/>.  See spatial.alg for a listing of options, in
    /// particular the options starting with "query.".
    /// </summary>
    public class SpatialFileQueryMaker : AbstractQueryMaker
    {
        protected SpatialStrategy m_strategy;
        protected double m_distErrPct;//NaN if not set
        protected SpatialOperation m_operation;
        protected bool m_score;

        protected IShapeConverter m_shapeConverter;

        public override void SetConfig(Config config)
        {
            m_strategy = SpatialDocMaker.GetSpatialStrategy(config.RoundNumber);
            m_shapeConverter = SpatialDocMaker.MakeShapeConverter(m_strategy, config, "query.spatial.");

            m_distErrPct = config.Get("query.spatial.distErrPct", double.NaN);
            m_operation = SpatialOperation.Get(config.Get("query.spatial.predicate", "Intersects"));
            m_score = config.Get("query.spatial.score", false);

            base.SetConfig(config);//call last, will call prepareQueries()
        }

        protected override Query[] PrepareQueries()
        {
            int maxQueries = m_config.Get("query.file.maxQueries", 1000);
            Config srcConfig = new Config(new Dictionary<string, string>());
            srcConfig.Set("docs.file", m_config.Get("query.file", null));
            srcConfig.Set("line.parser", m_config.Get("query.file.line.parser", null));
            srcConfig.Set("content.source.forever", "false");

            JCG.List<Query> queries = new JCG.List<Query>();
            LineDocSource src = new LineDocSource();
            try
            {
                src.SetConfig(srcConfig);
                src.ResetInputs();
                DocData docData = new DocData();
                for (int i = 0; i < maxQueries; i++)
                {
                    docData = src.GetNextDocData(docData);
                    IShape shape = SpatialDocMaker.MakeShapeFromString(m_strategy, docData.Name, docData.Body);
                    if (shape != null)
                    {
                        shape = m_shapeConverter.Convert(shape);
                        queries.Add(MakeQueryFromShape(shape));
                    }
                    else
                    {
                        i--;//skip
                    }
                }
            }
#pragma warning disable 168
            catch (NoMoreDataException e)
#pragma warning restore 168
            {
                //all-done
            }
            finally
            {
                src.Dispose();
            }
            return queries.ToArray();
        }


        protected virtual Query MakeQueryFromShape(IShape shape)
        {
            SpatialArgs args = new SpatialArgs(m_operation, shape);
            if (!double.IsNaN(m_distErrPct))
                args.DistErrPct = m_distErrPct;

            if (m_score)
            {
                ValueSource valueSource = m_strategy.MakeDistanceValueSource(shape.Center);
                return new CustomScoreQuery(m_strategy.MakeQuery(args), new FunctionQuery(valueSource));
            }
            else
            {
                //strategy.makeQuery() could potentially score (isn't well defined) so instead we call
                // makeFilter() and wrap

                Filter filter = m_strategy.MakeFilter(args);
                if (filter is QueryWrapperFilter queryWrapperFilter)
                {
                    return queryWrapperFilter.Query;
                }
                else
                {
                    return new ConstantScoreQuery(filter);
                }
            }
        }
    }
}
