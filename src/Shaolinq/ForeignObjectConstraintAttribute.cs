﻿// Copyright (c) 2007-2016 Thong Nguyen (tumtumtum@gmail.com)

using System;

namespace Shaolinq
{
	/// <summary>
	/// Configures how foriegn key constraints react to deletes or primary keys
	/// </summary>
	public class ForeignObjectConstraintAttribute
		: Attribute
	{
		public ForeignObjectAction OnDeleteAction { get; set; }
		public ForeignObjectAction OnUpdateAction { get; set; }
	}
}
