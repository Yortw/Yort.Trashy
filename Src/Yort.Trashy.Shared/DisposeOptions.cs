using System;
using System.Collections.Generic;
using System.Text;

namespace Yort.Trashy
{
	/// <summary>
	/// Options for handling how objects are disposed.
	/// </summary>
	/// <seealso cref="Yort.Trashy.Extensions.DisposeExtensions"/>
	[Flags]
	public enum DisposeOptions
	{
		/// <summary>
		/// No special handling or options.
		/// </summary>
		None = 0,
		/// <summary>
		/// Any exception other than <see cref="OutOfMemoryException"/> will be suppressed.
		/// </summary>
		SuppressExceptions = 2
	}
}