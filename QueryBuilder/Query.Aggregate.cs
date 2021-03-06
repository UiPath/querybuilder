using System.Collections.Generic;
using System.Linq;

namespace SqlKata
{
    public partial class Query
    {
        /**********************************************************************
         ** Generic aggregate                                                **
         **********************************************************************/
        public Query SelectAggregate(string type, IEnumerable<string> columns, AggregateColumn.AggregateDistinct distinct, string alias = null)
        {
            if (columns.Count() == 0)
            {
                throw new System.ArgumentException("Cannot aggregate without columns");
            }

            // According to ISO/IEC 9075:2016 all aggregates take only a single
            // value expression argument (i.e. one column). However, for the
            // special case of count(...), SqlKata implements a transform to
            // a sub query.
            if (columns.Count() > 1 && type != "count")
            {
                throw new System.ArgumentException("Cannot aggregate more than one column at once");
            }

            if (type != "count" || (columns.Count() == 1 && !this.IsDistinct))
            {
                Method = "select";
                this.AddComponent("select", new AggregateColumn
                {
                    Alias = alias,
                    Type = type,
                    Column = columns.First(),
                    Distinct = distinct,
                });
            }
            else
            {
                if (this.HasComponent("aggregate"))
                {
                    throw new System.InvalidOperationException("Cannot add more than one top-level aggregate clause");
                }
                if (columns.Count() > 1 && distinct == AggregateColumn.AggregateDistinct.aggregateDistinct)
                {
                    throw new System.NotImplementedException("Count distinct over multiple columns is not implemented");
                }
                Method = "aggregate";
                this.AddComponent("aggregate", new AggregateClause
                {
                    Alias = alias,
                    Type = type,
                    Columns = columns.ToList(),
                });
            }

            return this;
        }


        /**********************************************************************
         ** Count                                                            **
         **********************************************************************/
        public Query SelectCount(string column = null, string alias = null)
        {
            return SelectCount(column != null ? new[] { column } : new string[] { }, alias);
        }

        public Query SelectCountDistinct(string column = null, string alias = null)
        {
            return SelectCountDistinct(column != null ? new[] { column } : new string[] { }, alias);
        }

        public Query SelectCount(IEnumerable<string> columns, string alias = null)
        {
            return SelectAggregate("count", columns.Count() == 0 ? new[] { "*" } : columns, AggregateColumn.AggregateDistinct.aggregateNonDistinct, alias);
        }

        public Query SelectCountDistinct(IEnumerable<string> columns, string alias = null)
        {
            return SelectAggregate("count", columns.Count() == 0 ? new[] { "*" } : columns, AggregateColumn.AggregateDistinct.aggregateDistinct, alias);
        }


        /**********************************************************************
         ** Average                                                          **
         **********************************************************************/
        public Query SelectAvg(string column, string alias = null)
        {
            return SelectAggregate("avg", new[] { column }, AggregateColumn.AggregateDistinct.aggregateNonDistinct, alias);
        }

        public Query SelectAvgDistinct(string column, string alias = null)
        {
            return SelectAggregate("avg", new[] { column }, AggregateColumn.AggregateDistinct.aggregateDistinct, alias);
        }

        public Query SelectAverage(string column, string alias = null)
        {
            return SelectAvg(column, alias);
        }

        public Query SelectAverageDistinct(string column, string alias = null)
        {
            return SelectAvg(column, alias);
        }


        /**********************************************************************
         ** Sum                                                              **
         **********************************************************************/
        public Query SelectSum(string column, string alias = null)
        {
            return SelectAggregate("sum", new[] { column }, AggregateColumn.AggregateDistinct.aggregateNonDistinct, alias);
        }

        public Query SelectSumDistinct(string column, string alias = null)
        {
            return SelectAggregate("sum", new[] { column }, AggregateColumn.AggregateDistinct.aggregateDistinct, alias);
        }


        /**********************************************************************
         ** Maximum                                                          **
         **********************************************************************/
        public Query SelectMax(string column, string alias = null)
        {
            return SelectAggregate("max", new[] { column }, AggregateColumn.AggregateDistinct.aggregateNonDistinct, alias);
        }

        public Query SelectMaxDistinct(string column, string alias = null)
        {
            return SelectAggregate("max", new[] { column }, AggregateColumn.AggregateDistinct.aggregateDistinct, alias);
        }


        /**********************************************************************
         ** Minimum                                                          **
         **********************************************************************/
        public Query SelectMin(string column, string alias = null)
        {
            return SelectAggregate("min", new[] { column }, AggregateColumn.AggregateDistinct.aggregateNonDistinct, alias);
        }

        public Query SelectMinDistinct(string column, string alias = null)
        {
            return SelectAggregate("min", new[] { column }, AggregateColumn.AggregateDistinct.aggregateDistinct, alias);
        }
    }
}
