// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Class)]
	public class DataAccessObjectAttribute
		: Attribute
	{
		public string Name { get; set; }
		public bool NotPersisted { get; set; }

		public string GetName(Type type)
		{
			return this.Name ?? type.Name;
		}
	}
}
