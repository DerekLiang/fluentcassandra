using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentCassandra.Types;

namespace FluentCassandra.Linq
{
	/// <see href="https://github.com/apache/cassandra/blob/trunk/doc/cql/CQL.textile"/>
	internal class CqlQueryEvaluator<CompareWith>
		where CompareWith : CassandraType
	{
		private string _columnFamily;

		internal CqlQueryEvaluator()
		{
			FieldsArray = new List<string>();
		}

		public string Query
		{
			get
			{
				var select = Fields;
				var from = _columnFamily;
				var where = WhereCriteria;

				var query = String.Format("SELECT {0} \nFROM {1}", select, from);

				if (!String.IsNullOrWhiteSpace(where))
					query += " \nWHERE " + where;

				return query;
			}
		}

		private IList<string> FieldsArray { get; set; }

		private string Fields
		{
			get
			{
				if (FieldsArray.Count == 0)
					return "*";

				return String.Join(", ", FieldsArray.ToArray());
			}
		}

		private string WhereCriteria { get; set; }

		private void AddTable<CompareWith>(CassandraColumnFamily<CompareWith> provider)
			where CompareWith : CassandraType
		{
			_columnFamily = provider.FamilyName;
		}

		private void AddField(Expression exp)
		{
			foreach (var f in VisitSelectExpression(exp))
				FieldsArray.Add(f);
		}

		private void AddCriteria(Expression exp)
		{
			string newCriteria = VisitWhereExpression(exp);

			if (!String.IsNullOrEmpty(WhereCriteria))
				WhereCriteria = "(" + WhereCriteria + " AND " + newCriteria + ")";
			else
				WhereCriteria = newCriteria;
		}

		#region Expression Helpers

		private static Expression SimplifyExpression(Expression exp)
		{
			switch (exp.NodeType)
			{
				case ExpressionType.Convert:
				case ExpressionType.Quote:
					return SimplifyExpression(((UnaryExpression)exp).Operand);

				case ExpressionType.Lambda:
					return SimplifyExpression(((LambdaExpression)exp).Body);

				default:
					return exp;
			}
		}

		private static string GetPropertyName(Expression exp)
		{
			exp = SimplifyExpression(exp);

			if (exp.NodeType != ExpressionType.Call)
				throw new NotSupportedException(exp.NodeType.ToString() + " is not supported.");

			var field = SimplifyExpression(((MethodCallExpression)exp).Arguments[0]);

			if (field.NodeType != ExpressionType.Constant)
				throw new NotSupportedException(exp.NodeType.ToString() + " is not supported.");

			return ((ConstantExpression)field).Value.ToString();
		}

		#endregion

		#region Expression Parsing

		public static string GetCql<CompareWith>(Expression expression)
			where CompareWith : CassandraType
		{
			var eval = GetEvaluator<CompareWith>(expression);
			return eval.Query;
		}

		public static CqlQueryEvaluator<CompareWith> GetEvaluator<CompareWith>(Expression expression)
			where CompareWith : CassandraType
		{
			var eval = new CqlQueryEvaluator<CompareWith>();
			eval.Evaluate(expression);

			return eval;
		}

		private void Evaluate(Expression exp, Action<string> call = null)
		{
			switch (exp.NodeType)
			{
				case ExpressionType.Lambda:
					Evaluate(((LambdaExpression)exp).Body);
					break;

				case ExpressionType.Call:
					VisitMethodCall((MethodCallExpression)exp);
					break;

				case ExpressionType.New:
					VisitNew((NewExpression)exp, call);
					break;

				case ExpressionType.MemberInit:
					VisitMemberInit((MemberInitExpression)exp, call);
					break;

				case ExpressionType.MemberAccess:
					VisitMemberAccess((MemberExpression)exp, call);
					break;

				case ExpressionType.Constant:
					AddTable(((ConstantExpression)exp).Value as CassandraColumnFamily<CompareWith>);
					break;
			}
		}

		private void VisitMemberAccess(MemberExpression exp, Action<string> call)
		{
			call(GetPropertyName(exp));
		}

		private void VisitMemberInit(MemberInitExpression exp, Action<string> call)
		{
			foreach (MemberAssignment member in exp.Bindings)
				call(GetPropertyName(member.Expression));
		}

		private void VisitNew(NewExpression exp, Action<string> call)
		{
			foreach (var arg in exp.Arguments)
				call(GetPropertyName(arg));

			VisitMemberInit(Expression.MemberInit(exp, new MemberBinding[0]), call);
		}

		private void VisitMethodCall(MethodCallExpression exp)
		{
			Evaluate(exp.Arguments[0]);

			if (exp.Method.Name == "Where")
				AddCriteria(exp.Arguments[1]);
			else if (exp.Method.Name == "Select")
				AddField(SimplifyExpression(exp.Arguments[1]));
			else
				throw new NotSupportedException("Method call to " + exp.Method.Name + " is not supported.");
		}

		private IEnumerable<CompareWith> VisitSelectExpression(Expression exp)
		{
			switch (exp.NodeType)
			{
				case ExpressionType.Parameter:
					return new CompareWith[0];

				case ExpressionType.Constant:
					return VisitSelectColumnExpression((ConstantExpression)exp);

				default:
					throw new NotSupportedException(exp.NodeType.ToString() + " is not supported.");
			}
		}

		private IEnumerable<CompareWith> VisitSelectColumnExpression(ConstantExpression exp)
		{
			return (IEnumerable<CompareWith>)exp.Value;
		}

		private string VisitWhereExpression(Expression exp)
		{
			switch (exp.NodeType)
			{
				case ExpressionType.Convert:
				case ExpressionType.Lambda:
				case ExpressionType.Quote:
					return VisitWhereExpression(SimplifyExpression(exp));

				case ExpressionType.Not:
					return VisitWhereUnaryExpression((UnaryExpression)exp);

				case ExpressionType.Equal:
				case ExpressionType.NotEqual:
				case ExpressionType.GreaterThan:
				case ExpressionType.GreaterThanOrEqual:
				case ExpressionType.LessThan:
				case ExpressionType.LessThanOrEqual:
					return VisitWhereRelationalExpression((BinaryExpression)exp);

				case ExpressionType.And:
				case ExpressionType.AndAlso:
				case ExpressionType.Or:
				case ExpressionType.OrElse:
					return VisitWhereConditionalExpression((BinaryExpression)exp);

				case ExpressionType.Call:
					return VisitWhereMethodCallExpression((MethodCallExpression)exp);

				default:
					throw new NotSupportedException(exp.NodeType.ToString() + " is not supported.");
			}
		}

		private string VisitWhereUnaryExpression(UnaryExpression exp)
		{
			switch (exp.NodeType)
			{
				case ExpressionType.Not:
					return "NOT (" + VisitWhereExpression(exp.Operand) + ")";

				default:
					throw new NotSupportedException(exp.NodeType.ToString() + " is not a supported unary criteria.");
			}
		}

		private string RightObjectToString(object obj)
		{
			string value = obj.ToString();
			if (obj is String)
				return String.Concat("'", value, "'");
			return value;
		}

		private string VisitWhereMethodCallExpression(MethodCallExpression exp)
		{
			if (exp.Method.Name == "Contains")
			{
				var left = GetPropertyName(exp.Arguments[1]);
				var values = (IEnumerable)Expression.Lambda(exp.Arguments[0]).Compile().DynamicInvoke();
				var rightArray = new List<string>();
				foreach (var obj in values)
					rightArray.Add(RightObjectToString(obj));
				var right = String.Join(",", rightArray);

				return left + " IN (" + right + ")";
			}
			else
				throw new NotSupportedException("Method call to " + exp.Method.Name + " is not supported.");
		}

		private string VisitWhereRelationalExpression(BinaryExpression exp)
		{
			string criteria;

			string left = GetPropertyName(exp.Left);
			object rightObj = Expression.Lambda(exp.Right).Compile().DynamicInvoke();
			string right = RightObjectToString(rightObj);

			switch (exp.NodeType)
			{
				case ExpressionType.Equal:
					if (rightObj == null)
						criteria = left + " IS NULL";
					else
						criteria = left + " = " + right;
					break;
				case ExpressionType.NotEqual:
					if (rightObj == null)
						criteria = left + " IS NOT NULL";
					else
						criteria = left + " != " + right;
					break;
				case ExpressionType.GreaterThan:
					criteria = left + " > " + right;
					break;
				case ExpressionType.GreaterThanOrEqual:
					criteria = left + " >= " + right;
					break;
				case ExpressionType.LessThan:
					criteria = left + " < " + right;
					break;
				case ExpressionType.LessThanOrEqual:
					criteria = left + " <= " + right;
					break;

				default:
					throw new NotSupportedException(
						exp.NodeType.ToString() + " is not a supported relational criteria.");
			}

			return criteria;
		}

		private string VisitWhereConditionalExpression(BinaryExpression exp)
		{
			string criteria;

			switch (exp.NodeType)
			{
				case ExpressionType.And:
				case ExpressionType.AndAlso:
					criteria = "(" + VisitWhereExpression(exp.Left) + " AND " + VisitWhereExpression(exp.Right) + ")";
					break;
				case ExpressionType.Or:
				case ExpressionType.OrElse:
					criteria = "(" + VisitWhereExpression(exp.Left) + " OR " + VisitWhereExpression(exp.Right) + ")";
					break;

				default:
					throw new NotSupportedException(exp.NodeType.ToString() + " is not a supported conditional criteria.");
			}

			return criteria;
		}
		#endregion
	}
}