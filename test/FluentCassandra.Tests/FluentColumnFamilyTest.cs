﻿using System;
using System.Linq;
using NUnit.Framework;
using FluentCassandra.Types;

namespace FluentCassandra
{
	[TestFixture]
	public class FluentColumnFamilyTest
	{
		[Test]
		public void Self_Set()
		{
			// arrange

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");

			// assert
			Assert.AreSame(actual, actual.GetSelf().ColumnFamily);
		}

		[Test]
		public void Path_Set()
		{
			// arrange

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");

			// assert
			Assert.AreSame(actual, actual.GetPath().ColumnFamily);
		}

		[Test]
		public void Constructor_Test()
		{
			// arrange
			var col1 = new FluentColumn<AsciiType> { ColumnName = "Test1", ColumnValue = 300M };
			var col2 = new FluentColumn<AsciiType> { ColumnName = "Test2", ColumnValue = "Hello" };

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Columns.Add(col1);
			actual.Columns.Add(col2);

			// assert
			Assert.AreEqual(2, actual.Columns.Count);
			Assert.AreSame(col1.Family, actual);
			Assert.AreSame(col2.Family, actual);
		}

		[Test]
		public void Constructor_Dynamic_Test()
		{
			// arrange
			var col1 = "Test1";
			var colValue1 = 300M;
			var col2 = "Test2";
			var colValue2 = "Hello";

			// act
			dynamic actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Test1 = colValue1;
			actual.Test2 = colValue2;

			// assert
			Assert.AreEqual(colValue1, (decimal)actual.Test1);
			Assert.AreEqual(colValue1, (decimal)actual[col1]);
			Assert.AreEqual(colValue2, (string)actual.Test2);
			Assert.AreEqual(colValue2, (string)actual[col2]);
		}

		[Test]
		public void Get_NonExistent_Column()
		{
			// arrange
			var colValue1 = 300M;
			var colValue2 = "Hello";
			string expected = null;

			// act
			dynamic family = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			family.Test1 = colValue1;
			family.Test2 = colValue2;
			var actual = family.Test3;

			// assert
			Assert.AreEqual(expected, actual);
		}

		[Test]
		public void Mutation()
		{
			// arrange
			var col1 = new FluentColumn<AsciiType> { ColumnName = "Test1", ColumnValue = 300M };
			var col2 = new FluentColumn<AsciiType> { ColumnName = "Test2", ColumnValue = "Hello" };

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Columns.Add(col1);
			actual.Columns.Add(col2);

			// assert
			var mutations = actual.MutationTracker.GetMutations();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(2, mutations.Count(x => x.Type == MutationType.Added));

			var mut1 = mutations.FirstOrDefault(x => x.Column.ColumnName == "Test1");
			var mut2 = mutations.FirstOrDefault(x => x.Column.ColumnName == "Test2");

			Assert.AreSame(col1, mut1.Column);
			Assert.AreSame(col2, mut2.Column);

			Assert.AreSame(actual, mut1.Column.GetParent().ColumnFamily);
			Assert.AreSame(actual, mut2.Column.GetParent().ColumnFamily);
		}

		[Test]
		public void Dynamic_Mutation()
		{
			// arrange
			var col1 = "Test1";
			var colValue1 = 300M;
			var col2 = "Test2";
			var colValue2 = "Hello";

			// act
			dynamic actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Test1 = colValue1;
			actual.Test2 = colValue2;

			// assert
			var mutations = ((IFluentRecord)actual).MutationTracker.GetMutations();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(2, mutations.Count(x => x.Type == MutationType.Added));

			var mut1 = mutations.FirstOrDefault(x => x.Column.ColumnName == col1);
			var mut2 = mutations.FirstOrDefault(x => x.Column.ColumnName == col2);

			Assert.IsNotNull(mut1);
			Assert.IsNotNull(mut2);

			Assert.AreSame(actual, mut1.Column.GetParent().ColumnFamily);
			Assert.AreSame(actual, mut2.Column.GetParent().ColumnFamily);
		}

		[Test]
		public void Mutation_Added()
		{
			// arrange
			var col1 = new FluentColumn<AsciiType> { ColumnName = "Test1", ColumnValue = 300M };
			var col2 = new FluentColumn<AsciiType> { ColumnName = "Test2", ColumnValue = "Hello" };

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Columns.Add(col1);
			actual.Columns.Add(col2);

			// assert
			var mutations = actual.MutationTracker.GetMutations();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(2, mutations.Count(x => x.Type == MutationType.Added));
		}

		[Test]
		public void Mutation_Changed()
		{
			// arrange
			var col1 = new FluentColumn<AsciiType> { ColumnName = "Test1", ColumnValue = 300M };
			var col2 = new FluentColumn<AsciiType> { ColumnName = "Test1", ColumnValue = "Hello" };

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Columns.Add(col1);
			actual.Columns[0] = col2;

			// assert
			var mutations = actual.MutationTracker.GetMutations().ToList();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(MutationType.Added, mutations[0].Type);
			Assert.AreEqual(MutationType.Changed, mutations[1].Type);
		}


		[Test]
		public void Mutation_Replaced()
		{
			// arrange
			var col1 = new FluentColumn<AsciiType> { ColumnName = "Test1", ColumnValue = 300M };
			var col2 = new FluentColumn<AsciiType> { ColumnName = "Test2", ColumnValue = "Hello" };

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Columns.Add(col1);
			actual.Columns[0] = col2;

			// assert
			var mutations = actual.MutationTracker.GetMutations().ToList();

			Assert.AreEqual(3, mutations.Count());
			Assert.AreEqual(MutationType.Added, mutations[0].Type);
			Assert.AreEqual(MutationType.Removed, mutations[1].Type);
			Assert.AreEqual(MutationType.Added, mutations[2].Type);
		}

		[Test]
		public void Mutation_Removed()
		{
			// arrange
			var col1 = new FluentColumn<AsciiType> { ColumnName = "Test1", ColumnValue = 300M };

			// act
			var actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Columns.Add(col1);
			actual.RemoveColumn("Test1");

			// assert
			var mutations = actual.MutationTracker.GetMutations().ToList();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(MutationType.Added, mutations[0].Type);
			Assert.AreEqual(MutationType.Removed, mutations[1].Type);
		}

		[Test]
		public void Dynamic_Mutation_Added()
		{
			// arrange
			var colValue1 = 300M;
			var colValue2 = "Hello";

			// act
			dynamic actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Test1 = colValue1;
			actual.Test2 = colValue2;

			// assert
			var mutations = ((IFluentRecord)actual).MutationTracker.GetMutations();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(2, mutations.Count(x => x.Type == MutationType.Added));
		}

		[Test]
		public void Dynamic_Mutation_Changed()
		{
			// arrange
			var colValue1 = 300M;
			var colValue2 = "Hello";

			// act
			dynamic actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Test1 = colValue1;
			actual.Test1 = colValue2;

			// assert
			var mutations = ((IFluentRecord)actual).MutationTracker.GetMutations().ToList();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(MutationType.Added, mutations[0].Type);
			Assert.AreEqual(MutationType.Changed, mutations[1].Type);
		}

		[Test]
		public void Dynamic_Mutation_Removed()
		{
			// arrange
			var colValue1 = 300M;

			// act
			dynamic actual = new FluentColumnFamily<AsciiType>("Keyspace1", "Standard1");
			actual.Test1 = colValue1;
			actual.RemoveColumn("Test1");

			// assert
			var mutations = ((IFluentRecord)actual).MutationTracker.GetMutations().ToList();

			Assert.AreEqual(2, mutations.Count());
			Assert.AreEqual(MutationType.Added, mutations[0].Type);
			Assert.AreEqual(MutationType.Removed, mutations[1].Type);
		}
	}
}
