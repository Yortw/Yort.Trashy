using System;

namespace Yort.Trashy
{
	/// <summary>
	/// An interface for a disposable component that only disposes once all references are released.
	/// </summary>
	/// <remarks>
	/// <para>Types implementing this interface should start the reference count at one when a new instance is created.</para>
	/// </remarks>
	public interface IReferenceCountedDisposableBase : IDisposable
	{
		/// <summary>
		/// Increases the reference count by one. The instance will not be disposed until the count reaches zero (the count is decremented each time <see cref="IDisposable.Dispose"/> is called).
		/// </summary>
		void AddReference();
		/// <summary>
		/// Creates either a <see cref="DisposableToken"/> or <see cref="DisposableValueToken"/> that will call <see cref="IDisposable.Dispose"/> on this instance once, and only once, the first time the token is disposed.
		/// </summary>
		/// <returns>An instance of a token type implementing <see cref="IDisposable"/>.</returns>
		IDisposable CreateReferenceToken();
	}
}