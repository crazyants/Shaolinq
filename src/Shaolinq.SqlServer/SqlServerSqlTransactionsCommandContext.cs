﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Transactions;
using Shaolinq.Persistence;

namespace Shaolinq.SqlServer
{
	public class SqlServerSqlTransactionsCommandContext
		: DefaultSqlTransactionalCommandsContext
	{
		public SqlServerSqlTransactionsCommandContext(SqlDatabaseContext sqlDatabaseContext, Transaction transaction)
			: base(sqlDatabaseContext, transaction)
		{
		}

		protected override DbType GetDbType(Type type)
		{
			var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

			if (underlyingType == typeof(DateTime))
			{
				return DbType.DateTime2;
			}

			return base.GetDbType(type);
		}
	}
}
