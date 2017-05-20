using System;
using System.Collections.Generic;
using System.Text;

namespace Yort.Trashy
{
	/// <summary>
	/// A value-type token (struct) which calls a specified <see cref="Action"/> the first time it is disposed.
	/// </summary>
	/// <remarks>
	/// <para>Use this token type for best performance when you are only using the token within a single method and do not require finalization semantics.</para>
	/// </remarks>
	public struct DisposableValueToken : IDisposable
	{
		private Action _DisposeAction;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="disposeAction">The <see cref="Action"/> to call when this token is disposed.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="disposeAction"/> is null.</exception>
		public DisposableValueToken(Action disposeAction)
		{
			if (disposeAction == null) throw new ArgumentNullException(nameof(disposeAction));

			_DisposeAction = disposeAction;
		}

		/// <summary>
		/// Calls the <see cref="Action"/> passed into the constructor the first time this method is called. Subsequent/concurrent calls will do nothing.
		/// </summary>
		public void Dispose()
		{
			//Interface is implemented explicitly to avoid boxing the token.
			Action toRun = System.Threading.Interlocked.CompareExchange<Action>(ref _DisposeAction, null, _DisposeAction);
			toRun?.Invoke();
		}
	}
}
