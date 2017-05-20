using System;
using System.Collections.Generic;
using System.Text;

namespace Yort.Trashy
{
	/// <summary>
	/// A reference-type token which calls a specified <see cref="Action"/> the first time it is disposed.
	/// </summary>
	/// <remarks>
	/// <para>Use this token if you want to ensure the <see cref="Dispose"/> if you want to pass the token around by reference by default.</para>
	/// </remarks>
	/// <seealso cref="DisposableValueToken"/>
	public sealed class DisposableToken : IDisposable
	{
		private Action _DisposeAction;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="disposeAction">The <see cref="Action"/> to call when this token is disposed.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="disposeAction"/> is null.</exception>
		/// <seealso cref="Dispose"/>
		public DisposableToken(Action disposeAction)
		{
			disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));

			_DisposeAction = disposeAction;
		}

		/// <summary>
		/// Calls the <see cref="Action"/> passed into the constructor the first time this method is called. Subsequent/concurrent calls will do nothing.
		/// </summary>
		public void Dispose()
		{
			Action toRun = System.Threading.Interlocked.CompareExchange<Action>(ref _DisposeAction, null, _DisposeAction);
			toRun?.Invoke();
		}
	}
}