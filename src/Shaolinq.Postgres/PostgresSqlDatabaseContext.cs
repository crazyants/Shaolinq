﻿// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data.Common;
using System.Transactions;
using Npgsql;
﻿using Shaolinq.Persistence;
using Shaolinq.Postgres.Shared;

﻿namespace Shaolinq.Postgres
{
	public class PostgresSqlDatabaseContext
		: SystemDataBasedSqlDatabaseContext
	{
		public string Host { get; set; }
		public string Userid { get; set; }
		public string Password { get; set; }
		public string DatabaseName { get; set; }
		public int Port { get; set; }
		
		public override string GetConnectionString()
		{
			return connectionString;
		}

		internal readonly string connectionString;
		internal readonly string databaselessConnectionString;

		public static PostgresSqlDatabaseContext Create(PostgresDatabaseContextInfo contextInfo, ConstraintDefaults constraintDefaults)
		{
			var sqlDialect = PostgresSharedSqlDialect.Default;
			var sqlDataTypeProvider = new PostgresSharedSqlDataTypeProvider(constraintDefaults, contextInfo.NativeUuids, contextInfo.DateTimeKindIfUnspecified);
			var sqlQueryFormatterManager = new DefaultSqlQueryFormatterManager(sqlDialect, sqlDataTypeProvider, (options, sqlDataTypeProviderArg, sqlDialectArg) => new PostgresSharedSqlQueryFormatter(options, sqlDataTypeProviderArg, sqlDialectArg, contextInfo.SchemaName));

			return new PostgresSqlDatabaseContext(sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo);
		}

		protected PostgresSqlDatabaseContext(SqlDialect sqlDialect, SqlDataTypeProvider sqlDataTypeProvider, SqlQueryFormatterManager sqlQueryFormatterManager, PostgresDatabaseContextInfo contextInfo)
			: base(sqlDialect, sqlDataTypeProvider, sqlQueryFormatterManager, contextInfo)
		{
			this.Host = contextInfo.ServerName;
			this.Userid = contextInfo.UserId;
			this.Password = contextInfo.Password;
			this.DatabaseName = contextInfo.DatabaseName;
			this.Port = contextInfo.Port;
			this.CommandTimeout = TimeSpan.FromSeconds(contextInfo.CommandTimeout);

			this.connectionString = String.Format("Host={0};User Id={1};Password={2};Database={3};Port={4};Pooling={5};MinPoolSize={6};MaxPoolSize={7};Enlist=false;Timeout={8};CommandTimeout={9}", contextInfo.ServerName, contextInfo.UserId, contextInfo.Password, contextInfo.DatabaseName, contextInfo.Port, contextInfo.Pooling, contextInfo.MinPoolSize, contextInfo.MaxPoolSize, contextInfo.ConnectionTimeout, contextInfo.CommandTimeout);
			this.databaselessConnectionString = String.Format("Host={0};User Id={1};Password={2};Port={4};Pooling={5};MinPoolSize={6};MaxPoolSize={7};Enlist=false;Timeout={8};CommandTimeout={9}", contextInfo.ServerName, contextInfo.UserId, contextInfo.Password, contextInfo.DatabaseName, contextInfo.Port, contextInfo.Pooling, contextInfo.MinPoolSize, contextInfo.MaxPoolSize, contextInfo.ConnectionTimeout, contextInfo.CommandTimeout);
		}

		public override DatabaseTransactionContext CreateDatabaseTransactionContext(DataAccessModel dataAccessModel, Transaction transaction)
		{
			return new PostgresSqlDatabaseTransactionContext(this, dataAccessModel, transaction);
		}

		public override DbProviderFactory CreateDbProviderFactory()
		{
			return NpgsqlFactory.Instance;
		}

		public override DatabaseCreator CreateDatabaseCreator(DataAccessModel model)
		{
			return new PostgresDatabaseCreator(this, model);
		}

		public override IDisabledForeignKeyCheckContext AcquireDisabledForeignKeyCheckContext(DatabaseTransactionContext databaseTransactionContext)
		{
			return new DisabledForeignKeyCheckContext(databaseTransactionContext);	
		}

		public override void DropAllConnections()
		{
			NpgsqlConnection.ClearAllPools();
		}
	}
}
