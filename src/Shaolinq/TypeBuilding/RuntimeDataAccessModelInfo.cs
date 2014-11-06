﻿// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence;

namespace Shaolinq.TypeBuilding
{
	public class RuntimeDataAccessModelInfo
	{
		private readonly Type dataAccessModelType;
		public TypeDescriptorProvider TypeDescriptorProvider { get; private set; }
		public Assembly ConcreteAssembly { get; private set; }
		public Assembly DefinitionAssembly { get; private set; }
		
		private readonly Dictionary<Type, Type> enumerableTypes = new Dictionary<Type, Type>();
		private readonly Dictionary<Type, Type> typesByConcreteType = new Dictionary<Type, Type>();
		private readonly Dictionary<Type, Type> concreteTypesByType = new Dictionary<Type, Type>();
		private readonly Dictionary<Type, Type> dataAccessObjectsTypes = new Dictionary<Type, Type>();
		private Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>> dataAccessObjectConstructors = new Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>>();
		private readonly Func<DataAccessModel> dataAccessModelConstructor;
		
		public RuntimeDataAccessModelInfo(TypeDescriptorProvider typeDescriptorProvider, Assembly concreteAssembly, Assembly definitionAssembly)
		{
			this.TypeDescriptorProvider = typeDescriptorProvider;
			this.dataAccessModelType = typeDescriptorProvider.DataAccessModelType;
			
			Debug.Assert(dataAccessModelType.Assembly == definitionAssembly);

			this.ConcreteAssembly = concreteAssembly;
			this.DefinitionAssembly = definitionAssembly;

			var concreteDataAccessModelType = concreteAssembly.GetType(dataAccessModelType.Namespace + "." + dataAccessModelType.Name);
			this.dataAccessModelConstructor = Expression.Lambda<Func<DataAccessModel>>(Expression.Convert(Expression.New(concreteDataAccessModelType), dataAccessModelType)).Compile();

			var typeProvider = new TypeDescriptorProvider(dataAccessModelType);

			foreach (var type in typeProvider.GetTypeDescriptors())
			{
				var concreteType = concreteAssembly.GetType(type.Type.Namespace + "." + type.Type.Name);

				this.concreteTypesByType[type.Type] = concreteType;
				this.typesByConcreteType[concreteType] = type.Type;
				
				this.dataAccessObjectsTypes[type.Type] = TypeHelper.DataAccessObjectsType.MakeGenericType(type.Type);
				this.enumerableTypes[type.Type] = TypeHelper.IEnumerableType.MakeGenericType(type.Type);
			}
		}
		
		public Type GetDataAccessObjectsType(Type type)
		{
			Type retval;
			
			if (this.dataAccessObjectsTypes.TryGetValue(type, out retval))
			{
				return retval;
			}

			return TypeHelper.DataAccessObjectsType.MakeGenericType(type);
		}

		public DataAccessModel NewDataAccessModel()
		{
			return this.dataAccessModelConstructor();
		}

		public DataAccessObject CreateDataAccessObject(Type dataAccessObjectType, DataAccessModel dataAccessModel, bool isNew)
		{
			return GetDataAccessObjectConstructor(dataAccessObjectType)(dataAccessModel, isNew);
		}

		private Func<DataAccessModel, bool, DataAccessObject> GetDataAccessObjectConstructor(Type dataAccessObjectType)
		{
			Func<DataAccessModel, bool, DataAccessObject> constructor;

			if (!dataAccessObjectConstructors.TryGetValue(dataAccessObjectType, out constructor))
			{
				Type type;
                
				if (dataAccessObjectType.Assembly == this.ConcreteAssembly || dataAccessObjectType.Assembly == this.DefinitionAssembly)
				{
					if (!this.concreteTypesByType.TryGetValue(dataAccessObjectType, out type))
					{
						throw new InvalidDataAccessObjectModelDefinition("Could not find metadata for {0}", dataAccessObjectType);
					}
				}
				else
				{
					throw new InvalidOperationException("The type is not part of " + this.dataAccessModelType.Name);
				}

				var isNewParam = Expression.Parameter(typeof(bool));
				var dataAccessModelParam = Expression.Parameter(typeof(DataAccessModel));

				constructor = Expression.Lambda<Func<DataAccessModel, bool, DataAccessObject>>(Expression.Convert(Expression.New(type.GetConstructor(new[] { typeof(DataAccessModel), typeof(bool) }), dataAccessModelParam, isNewParam), dataAccessObjectType), dataAccessModelParam, isNewParam).Compile();

				var newDataAccessObjectConstructors = new Dictionary<Type, Func<DataAccessModel, bool, DataAccessObject>>(dataAccessObjectConstructors);

				newDataAccessObjectConstructors[dataAccessObjectType] = constructor;

				dataAccessObjectConstructors = newDataAccessObjectConstructors;
			}

			return constructor;
		}

		public T CreateDataAccessObject<T>(DataAccessModel dataAccessModel, bool isNew)
			where T : DataAccessObject
		{
			return (T)this.GetDataAccessObjectConstructor(typeof(T))(dataAccessModel, isNew);
		}

		public Type GetConcreteType(Type definitionType)
		{
			Type retval;

			if (this.ConcreteAssembly == definitionType.Assembly)
			{
				return definitionType;
			}

			if (this.concreteTypesByType.TryGetValue(definitionType, out retval))
			{
				return retval;
			}

			throw new InvalidOperationException(string.Format("Type {0} is unexpected", definitionType.Name));
		}

		public Type GetDefinitionType(Type concreteType)
		{
			Type retval;

			if (this.DefinitionAssembly == concreteType.Assembly)
			{
				return concreteType;
			}

			if (this.typesByConcreteType.TryGetValue(concreteType, out retval))
			{
				return retval;
			}

			throw new InvalidOperationException(string.Format("Type {0} is unexpected", concreteType.Name));
		}
	}
}