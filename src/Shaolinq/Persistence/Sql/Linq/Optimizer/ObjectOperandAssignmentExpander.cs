﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Expressions;

namespace Shaolinq.Persistence.Sql.Linq.Optimizer
{
	/// <summary>
	/// Turns an assignment involving a data access object into multiple assignments (one for each primary key)
	/// </summary>
	public class ObjectOperandAssignmentExpander
		: SqlExpressionVisitor
	{
		private readonly BaseDataAccessModel dataAccessModel;
		private readonly PersistenceContext persistenceContext;

		private ObjectOperandAssignmentExpander(BaseDataAccessModel dataAccessModel, PersistenceContext persistenceContext)
		{
			this.dataAccessModel = dataAccessModel;
			this.persistenceContext = persistenceContext;
		}

		public static Expression Expand(BaseDataAccessModel dataAccessModel, PersistenceContext persistenceContext, Expression expression)
		{
			return new ObjectOperandAssignmentExpander(dataAccessModel, persistenceContext).Visit(expression);
		}

		private IEnumerable<MemberAssignment> CreateMemberAssignments(MemberAssignment assignment)
		{
			var i = 0;
			var sqlObjectOperand = (SqlObjectOperand) assignment.Expression;
			var typeDescriptor = this.dataAccessModel.GetTypeDescriptor(assignment.Expression.Type);

			var ownerTypeDescriptor = this.dataAccessModel.GetTypeDescriptor(assignment.Member.ReflectedType);
			var ownerPropertyDescriptor = ownerTypeDescriptor.GetPropertyDescriptorByPropertyName(assignment.Member.Name);

			foreach (var primaryKey in typeDescriptor.PrimaryKeyProperties)
			{
				var type = this.dataAccessModel.GetConcreteTypeFromDefinitionType(assignment.Member.ReflectedType);
				var propertyInfo = type.GetProperties().FirstOrDefault(c => c.Name == ownerPropertyDescriptor.PersistedName + primaryKey.PersistedShortName);
				
				yield return Expression.Bind(propertyInfo, sqlObjectOperand.ExpressionsInOrder[i]);

				i++;
			}

			yield break;
		}

		protected override IEnumerable<MemberBinding> VisitBindingList(ReadOnlyCollection<MemberBinding> original)
		{
			List<MemberBinding> list = null;

			for (int i = 0, n = original.Count; i < n; i++)
			{
				if (original[i].BindingType == MemberBindingType.Assignment)
				{
					var assignment = (MemberAssignment)original[i];
					
					if (assignment.Expression.NodeType == (ExpressionType)SqlExpressionType.ObjectOperand)
					{
						foreach (var subAssignment in CreateMemberAssignments(assignment))
						{
							if (list != null)
							{
								list.Add(subAssignment);
							}
							else
							{
								list = new List<MemberBinding>(n);

								for (int j = 0; j < i; j++)
								{
									list.Add(original[j]);
								}

								list.Add(subAssignment);
							}
						}

						continue;
					}
				}

				var b = VisitBinding(original[i]);

				if (list != null)
				{
					list.Add(b);
				}
				else if (b != original[i])
				{
					list = new List<MemberBinding>(n);

					for (int j = 0; j < i; j++)
					{
						list.Add(original[j]);
					}

					list.Add(b);
				}
			}

			if (list != null)
			{
				return list;
			}

			return original;
		}
	}
}