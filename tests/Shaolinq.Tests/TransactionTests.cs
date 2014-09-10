﻿// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Linq;
using System.Transactions;
using NUnit.Framework;

namespace Shaolinq.Tests
{
	[TestFixture("MySql")]
	[TestFixture("Postgres")]
	[TestFixture("Postgres.DotConnect")]
	[TestFixture("Postgres.DotConnect.Unprepared")]
	[TestFixture("Sqlite")]
	[TestFixture("SqliteInMemory")]
	[TestFixture("SqliteClassicInMemory")]
	public class TransactionTests
		: BaseTests
	{
		public TransactionTests(string providerName)
			: base(providerName)
		{
		}

		[Test]
		public void Test_Create_Object()
		{
			using (var scope = new TransactionScope())
			{
				var school = model.Schools.Create();
				
				school.Name = "Kung Fu School";

				var student = model.Students.Create();

				student.Firstname = "Bruce";
				student.Lastname = "Lee";
				student.School = school;

				scope.Complete();
			}

			using (var scope = new TransactionScope())
			{
				var student = model.Students.First(c => c.Firstname == "Bruce");

				Assert.AreEqual("Bruce Lee", student.Fullname);

				scope.Complete();
			}
		}

		[Test]
		public void Test_Create_Object_And_Abort()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();
				var student = school.Students.Create();

				student.Firstname = "StudentThatShouldNotExist";
			}

			using (var scope = new TransactionScope())
			{
				Assert.Catch<InvalidOperationException>(() => model.Students.First(c => c.Firstname == "StudentThatShouldNotExist"));
			}
		}

		[Test]
		public void Test_Create_Object_And_Flush_Then_Abort()
		{
			using (var scope = new TransactionScope())
			{
				var school = this.model.Schools.Create();
				var student = school.Students.Create();

				student.Firstname = "StudentThatShouldNotExist";

				scope.Flush(model);

				Assert.IsNotNull(model.Students.FirstOrDefault(c => c.Firstname == "StudentThatShouldNotExist"));
			}

			using (var scope = new TransactionScope())
			{
				Assert.Catch<InvalidOperationException>(() => model.Students.First(c => c.Firstname == "StudentThatShouldNotExist"));
			}
		}

		[Test]
		public void Test_Multiple_Updates_In_Single_Transaction()
		{
			var address1Name = Guid.NewGuid().ToString();
			var address2Name = Guid.NewGuid().ToString();

			// Create some objects
			using (var scope = new TransactionScope())
			{
				var address1 = this.model.Address.Create();
				address1.Country = address1Name;

				var address2 = this.model.Address.Create();
				address2.Country = address2Name;

				scope.Flush(model);

				Console.WriteLine("Address1 Id: {0}", address1.Id);
				Console.WriteLine("Address2 Id: {0}", address2.Id);
				
				scope.Complete();
			}

			// Update them
			using (var scope = new TransactionScope())
			{
				var address1 = this.model.Address.Single(x => x.Country == address1Name);
				var address2 = this.model.Address.Single(x => x.Country == address2Name);

				Console.WriteLine("Address1 Id: {0}", address1.Id);
				Console.WriteLine("Address2 Id: {0}", address2.Id);

				address1.Street = "Street1";
				address2.Street = "Street2";

				Console.WriteLine("Address1 changed: {0}", ((IDataAccessObject)address1).HasObjectChanged);
				Console.WriteLine("Address2 changed: {0}", ((IDataAccessObject)address2).HasObjectChanged);

				scope.Complete();
			}

			// Check they were both updated
			using (var scope = new TransactionScope())
			{
				var address1 = this.model.Address.Single(x => x.Country == address1Name);
				var address2 = this.model.Address.Single(x => x.Country == address2Name);

				Assert.That(address1.Street, Is.EqualTo("Street1"));
				Assert.That(address2.Street, Is.EqualTo("Street2"));
			}
		}
	}
}
