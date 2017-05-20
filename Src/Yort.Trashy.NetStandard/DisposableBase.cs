using System;
using System.Collections.Generic;
using System.Text;

namespace Yort.Trashy
{
	/// <summary>
	/// A base class for implementing disposable types that do are are likely to contain unmanaged resources.
	/// </summary>
	/// <remarks>
	/// <para>This class provides the same functionality as <see cref="DisposableManagedOnlyBase"/> but adds a finalizer with the correct dispose call. This provides correct finalization behaviour when the derived type contains unmanaged resources, but the finalizer will reduce construction/garbage collection performance (as is true of all .Net types with a finalizer.</para>
	/// </remarks>
	/// <seealso cref="DisposableManagedOnlyBase"/>
	/// <seealso cref="System.IDisposable"/>
	public abstract class DisposableBase : DisposableManagedOnlyBase
	{
		/// <summary>
		/// Ensures unmanaged resources used by this type are disposed.
		/// </summary>
		~DisposableBase()
		{
			Dispose(false);
		}
	}
}