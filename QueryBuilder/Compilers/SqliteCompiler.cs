using System.Collections.Generic;
using System.Linq;
using SqlKata;
using SqlKata.Compilers;

namespace SqlKata.Compilers
{
    public class SqliteCompiler : Compiler
    {
        public override string EngineCode { get; } = EngineCodes.Sqlite;
        public override string parameterPlaceholder { get; } = "?";
        public override string parameterPrefix { get; } = "@p";
        public override string OpeningIdentifier { get; } = "\"";
        public override string ClosingIdentifier { get; } = "\"";
        public override string LastId { get; } = "select last_insert_rowid() as id";

        public override string CompileTrue()
        {
            return "1";
        }

        public override string CompileFalse()
        {
            return "0";
        }

        public override string CompileLimit(SqlResult ctx)
        {
            var limit = ctx.Query.GetLimit(EngineCode);
            var offset = ctx.Query.GetOffset(EngineCode);

            if (limit == 0 && offset > 0)
            {
                ctx.Bindings.Add(offset);
                return "LIMIT -1 OFFSET ?";
            }

            return base.CompileLimit(ctx);
        }

        protected override string CompileBasicDateCondition(SqlResult ctx, BasicDateCondition condition)
        {
            var column = Wrap(condition.Column);
            var value = Parameter(ctx, condition.Value);

            var formatMap = new Dictionary<string, string> {
                { "date", "%Y-%m-%d" },
                { "time", "%H:%M:%S" },
                { "year", "%Y" },
                { "month", "%m" },
                { "day", "%d" },
                { "hour", "%H" },
                { "minute", "%M" },
            };

            if (!formatMap.ContainsKey(condition.Part))
            {
                return $"{column} {condition.Operator} {value}";
            }

            var sql = $"strftime('{formatMap[condition.Part]}', {column}) {condition.Operator} cast({value} as text)";

            if (condition.IsNot)
            {
                return $"NOT ({sql})";
            }

            return sql;
        }

        protected override string CompileColumns(SqlResult ctx)
        {
            int numApproxColumns = 0;

            // This is not made available as a generic implementation because
            // the SelectRaw() below (with the CAST-RANK-OVER construction) is
            // not tested to work on databases other than SQLite.
            ctx.Query.Clauses = ctx.Query.Clauses.Select(clause =>
            {
                if (clause is SqlKata.AggregatePercentileApproxColumn column)
                {
                    ++numApproxColumns;
                    var percentile = $"sqlkata_generated__percentile{ (numApproxColumns > 1 ? numApproxColumns.ToString() : "")}";
                    var q = new Query()
                        .With($"{percentile}_value", x =>
                            {
                                x
                                .SelectAs((column.Column, "value"))
                                .SelectRaw($@"CAST(RANK() OVER(ORDER BY ""{column.Column}"") - 1 AS FLOAT) / (COUNT(*) OVER() - 1) AS ""percentile""")
                                ;
                                x.Clauses.AddRange(ctx.Query.Clauses.Where(c => c is AbstractFrom));
                                return x;
                            }
                        )
                        .With($"{percentile}", x => x
                            .SelectAs(("value", "result"))
                            .From($"{percentile}_value")
                            .Where("percentile", ">=", column.Percentile)
                            .Limit(1)
                        )
                        .Join($"{percentile}", j => j)
                        ;

                    if (numApproxColumns == 1 && !ctx.Query.HasComponent("group"))
                    {
                        q.GroupBy($"{percentile}.result");
                    }

                    q.Clauses.Add(new Column
                    {
                        Component = column.Component,
                        Engine = column.Engine,
                        Name = $"{percentile}.result",
                        Alias = column.Alias ?? "percentileapprox",
                    });

                    return q.Clauses;
                }
                return new List<AbstractClause> { clause };
            }).SelectMany(x => x).ToList();

            return base.CompileColumns(ctx);
        }
    }
}
