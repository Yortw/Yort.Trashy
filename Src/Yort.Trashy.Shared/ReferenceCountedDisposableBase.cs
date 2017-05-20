using System;
using System.Collections.Generic;
using System.Text;

namespace Yort.Trashy
{
	/// <summary>
	/// Represents a disposable object where a reference count controls if and when the <see cref="Dispose()"/> method actually disposes the object.
	/// </summary>
	/// <remarks>
	/// <para>Code can call <see cref="AddReference()"/> or <see cref="CreateReferenceToken()"/> to increase the reference count. Each call to <see cref="Dispose"/> on this instance will decrement the reference count. The first call to <see cref="Dispose"/> on each token returned from <see cref="CreateReferenceToken"/> will also decrement the reference count. When the reference count reaches zero, the object will actually be disposed.</para>
	/// <para>This base class does not implement a finalizer by default. If your derived types have unmanaged resources, add a finalizer that calls <see cref="DisposableManagedOnlyBase.Dispose(bool)"/> passing false.</para>
	/// </remarks>
	/// <seealso cref="DisposableManagedOnlyBase"/>
	/// <seealso cref="IDisposable"/>
	public abstract class ReferenceCountedDisposableBase : DisposableManagedOnlyBase, IReferenceCountedDisposableBase
	{
		private int _ReferenceCount = 1;

		/// <summary>
		/// Increases the number of times <see cref="Dispose()"/> must be called before the object is really disposed.
		/// </summary>
		/// <remarks>
		/// <para>The initial reference count is 1, so the code instantiating the object does not need to call this method unless it needs to add references on behalf of other components that do not understand reference counting.</para>
		/// </remarks>
		/// <seealso cref="CreateReferenceToken"/>
		/// <seealso cref="Dispose"/>
		/// <exception cref="System.ObjectDisposedException">Thrown if this object is already disposed or being disposed.</exception>
		public void AddReference()
		{
			ThrowIfDisposed();

			System.Threading.Interlocked.Increment(ref _ReferenceCount);

			ThrowIfDisposed();
		}

		/// <summary>
		/// Returns a token implementing <see cref="IDisposable"/> that decrements the reference count for this object. If the count falls to zero, this object is actually disposed.
		/// </summary>
		/// <remarks>
		/// <para>The token returned is idempotent, in that any number of calls (concurrent or not) can be made to it's <see cref="IDisposable.Dispose()"/> method, but the reference count will only be decremented once. This is different from the <see cref="Dispose()"/> on this instance, which decrements the count once for each call made.</para>
		/// </remarks>
		/// <returns>A token implementing <see cref="IDisposable"/> that can be used as an idempotent proxy for <see cref="Dispose()"/> on this object.</returns>
		/// <seealso cref="AddReference"/>
		/// <seealso cref="Dispose"/>
		/// <exception cref="System.ObjectDisposedException">Thrown if this object is already disposed or being disposed.</exception>
		public IDisposable CreateReferenceToken()
		{
			AddReference();
			return new DisposableToken(Dispose);
		}

		/// <summary>
		/// Decreases the number of references by one, if the reference count reaches zero then actually disposes the object.
		/// </summary>
		/// <remarks>
		/// <para>This method is *not* idempotent in terms of reference counting. It should be called exactly one more time than the number of calls to <see cref="AddReference"/>. If the number of calls between <see cref="Dispose()"/> and <see cref="AddReference"/> are mismatched the object may be disposed too early, or not at all. Use <see cref="CreateReferenceToken"/> if you want an idempotent <see cref="Dispose()"/> call.</para>
		/// <para>This method *is* idempotent in terms of disposing the object. Once actually disposed, the object will not be disposed again and the method will do nothing. If multiple concurrent calls are made to <see cref="Dispose()"/> then only one will dispose the object and the others will wait for that call to finish before returning.</para>
		/// </remarks>
		/// <seealso cref="AddReference()"/>
		/// <seealso cref="CreateReferenceToken()"/>
		public override void Dispose()
		{
			if (IsDisposed) return;

			if (System.Threading.Interlocked.Decrement(ref _ReferenceCount) <= 0)
				base.Dispose();
		}

	}
}