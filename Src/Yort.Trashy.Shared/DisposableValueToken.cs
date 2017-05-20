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
		private Guid _Id;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="disposeAction">The <see cref="Action"/> to call when this token is disposed.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="disposeAction"/> is null.</exception>
		public DisposableValueToken(Action disposeAction)
		{
			disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));

			_Id = System.Guid.NewGuid();
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

		/// <summary>
		/// Compares this object with another and returns true if they are the same.
		/// </summary>
		/// <param name="obj">The value to compare this instance with.</param>
		/// <returns>True if the values are the same.</returns>
		public override bool Equals(object obj)
		{
			if (obj == null) return false;
			if (!(obj is DisposableValueToken)) return false;

			return Equals((DisposableValueToken)obj);
		}
		/// <summary>
		/// Compares this token with another and returns true if they are the same.
		/// </summary>
		/// <param name="other">The token to compare to.</param>
		/// <returns>True if the tokens are the same, otherwise false.</returns>
		public bool Equals(DisposableValueToken other)
		{
			return _Id == other._Id;
		}

		/// <summary>
		/// Returns a hash code for this token.
		/// </summary>
		/// <returns>A integer containing the hash code for this token.</returns>
		public override int GetHashCode()
		{
			return _Id.GetHashCode();
		}

		/// <summary>
		/// Compares two tokens and returns true if they are the same.
		/// </summary>
		/// <param name="a">The first token to compare.</param>
		/// <param name="b">The second token to compare.</param>
		/// <returns>True if a and b are the same.</returns>
		public static bool operator ==(DisposableValueToken a, DisposableValueToken b)
		{
			// If both are null, or both are same instance, return true.
			if (System.Object.ReferenceEquals(a, b))
				return true;

			// If one is null, but not both, return false.
			if (((object)a == null) || ((object)b == null))
				return false;

			// Return true if the fields match:
			return a._Id == b._Id;
		}

		/// <summary>
		/// Compares two tokens and returns true if they are NOT the same.
		/// </summary>
		/// <param name="a">The first token to compare.</param>
		/// <param name="b">The second token to compare.</param>
		/// <returns>True if a and b are the NOT same.</returns>
		public static bool operator !=(DisposableValueToken a, DisposableValueToken b)
		{
			return !(a == b);
		}
	}
}