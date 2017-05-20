using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Yort.Trashy
{
	/// <summary>
	/// An interface that allows external checking of the disposed state of an object.
	/// </summary>
	/// <seealso cref="IDisposable"/>
	public interface IIsDisposed : IDisposable
	{
		/// <summary>
		/// Returns true if the object is disposed (or being currently being disposed).
		/// </summary>
		bool IsDisposed { get; }
	}
}