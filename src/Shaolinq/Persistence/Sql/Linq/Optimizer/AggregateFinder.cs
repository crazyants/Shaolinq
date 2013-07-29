﻿using System.Collections.Generic;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	/// <summary>
	/// Finds and returns all aggregates within an expression.
	/// </summary>
	public class AggregateFinder
		: SqlExpressionVisitor
	{
		private readonly List<SqlAggregateSubqueryExpression> aggregatesFound;
		
		private AggregateFinder()
		{
			aggregatesFound = new List<SqlAggregateSubqueryExpression>();
		}

		public static List<SqlAggregateSubqueryExpression> Gather(Expression expression)
		{
			var gatherer = new AggregateFinder();

			gatherer.Visit(expression);

			return gatherer.aggregatesFound;
		}

		protected override Expression VisitAggregateSubquery(SqlAggregateSubqueryExpression aggregate)
		{
			this.aggregatesFound.Add(aggregate);

			return base.VisitAggregateSubquery(aggregate);
		}
	}
}