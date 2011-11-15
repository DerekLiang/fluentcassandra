﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentCassandra.Types;
using FluentCassandra.Linq;

namespace FluentCassandra
{
	public class FluentColumnFamily<CompareWith> : FluentRecord<IFluentColumn<CompareWith>>, IFluentColumnFamily<CompareWith>, ICqlRow<CompareWith>
		where CompareWith : CassandraType
	{
		private FluentColumnList<IFluentColumn<CompareWith>> _columns;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="columnFamily"></param>
		public FluentColumnFamily(BytesType key, string columnFamily)
		{
			Key = key;
			FamilyName = columnFamily;

			_columns = new FluentColumnList<IFluentColumn<CompareWith>>(GetSelf());
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="key"></param>
		/// <param name="columnFamily"></param>
		/// <param name="columns"></param>
		internal FluentColumnFamily(BytesType key, string columnFamily, IEnumerable<IFluentColumn<CompareWith>> columns)
		{
			Key = key;
			FamilyName = columnFamily;

			_columns = new FluentColumnList<IFluentColumn<CompareWith>>(GetSelf(), columns);
		}

		/// <summary>
		/// 
		/// </summary>
		public BytesType Key { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public string FamilyName { get; set; }

		/// <summary>
		/// 
		/// </summary>
		public ColumnType ColumnType { get { return ColumnType.Standard; } }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="columnName"></param>
		/// <returns></returns>
		public BytesType this[CompareWith columnName]
		{
			get
			{
				var value = GetColumnValue(columnName);

				if (value is NullType)
					throw new CassandraException(String.Format("Column, {0}, could not be found.", columnName));

				return value as BytesType;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public IFluentColumn<CompareWith> CreateColumn()
		{
			return new FluentColumn<CompareWith>();
		}

		/// <summary>
		/// 
		/// </summary>
		public override IList<IFluentColumn<CompareWith>> Columns
		{
			get { return _columns; }
		}

		/// <summary>
		/// Gets the path.
		/// </summary>
		/// <returns></returns>
		public FluentColumnPath GetPath()
		{
			return new FluentColumnPath(this, null, null);
		}

		/// <summary>
		/// Gets the parent.
		/// </summary>
		/// <returns></returns>
		public FluentColumnParent GetSelf()
		{
			return new FluentColumnParent(this, null);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		private CassandraType GetColumnValue(object name)
		{
			var col = Columns.FirstOrDefault(c => c.ColumnName == name);
			var result = (col == null) ? (CassandraType)NullType.Value : (CassandraType)col.ColumnValue;

			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="result"></param>
		/// <returns></returns>
		public override bool TryGetColumn(object name, out object result)
		{
			result = GetColumnValue(name);

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public override bool TrySetColumn(object name, object value)
		{
			var col = Columns.FirstOrDefault(c => c.ColumnName == name);
			var mutationType = MutationType.Changed;

			// if column doesn't exisit create it and add it to the columns
			if (col == null)
			{
				mutationType = MutationType.Added;

				col = new FluentColumn<CompareWith>();
				((FluentColumn<CompareWith>)col).ColumnName = CassandraType.GetType<CompareWith>(name);

				_columns.SupressChangeNotification = true;
				_columns.Add(col);
				_columns.SupressChangeNotification = false;
			}

			// set the column value
			col.ColumnValue = CassandraType.GetType<BytesType>(value);

			// notify the tracker that the column has changed
			OnColumnMutated(mutationType, col);

			return true;
		}
	}
}
