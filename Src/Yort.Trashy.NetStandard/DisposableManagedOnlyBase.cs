using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//TODO: Dispose Tracking

namespace Yort.Trashy
{
	/// <summary>
	/// A base class for implementing disposable types that are known to only contain managed disposable resources.
	/// </summary>
	/// <remarks>
	/// <para>Use this base class if your disposable type is not known or expected to need a finalizer, i.e if you do not have any unmanaged resources in your type and do not generally expect derived types to have any either.</para>
	/// <para>Derived types can still add a finalizer (which should call Dispose(false)) if they unexpectedly have unmanaged resources, but no finalizer is provided by default for performance reasons. If a derived type adds a finalizer, this call will correctly call <see cref="GC.SuppressFinalize(object)"/> when required, no additional logic is required by the dervied type.</para>
	/// <para>If your type or it's expected derivatives have or are likely to have unmanaged resources then use <see cref="DisposableBase"/>.</para>
	/// <para>Methods provided by this base class are thread-safe.</para>
	/// <para>Calling <see cref="Dispose()"/> more than once has no effect and is 'safe'.</para>
	/// </remarks>
	public abstract class DisposableManagedOnlyBase : IIsDisposed
	{

		#region Fields

		private volatile int _DisposedState;
		private volatile int _BusyCount;

		private const int UndisposedState = 0;
		private const int DisposingState = 1;
		private const int DisposedState = 2;

		#endregion

		#region Constructors

		/// <summary>
		/// Base constructor.
		/// </summary>
		public DisposableManagedOnlyBase()
		{
			DisposableTracker.Register(this);
		}

		#endregion

		#region IIsDisposed Members

		/// <summary>
		/// Returns true if the <see cref="Dispose()"/> method has been called, otherwise false.
		/// </summary>
		/// <remarks>
		/// This method will return true if the dispose method has been called and completed, or is in the process of being called on another thread.
		/// </remarks>
		public bool IsDisposed
		{
			get
			{
				return DisposeAssistant.IsDisposed(_DisposedState);
			}
		}

		#endregion

		#region IDispose Members

		/// <summary>
		/// Disposes this object and all internal resources.
		/// </summary>
		public virtual void Dispose()
		{
#pragma warning disable 420
			DisposeAssistant.Dispose(this, ref _DisposedState, ref _BusyCount, (disposing) => this.Dispose(disposing));
#pragma warning restore 420

			//int priorState = _DisposedState;
			//if (priorState == DisposedState) return; //If we are already disposed, do nothing.

			////Wait until there is no work in progress on the object (via EnterBusy/ExitBusy)
			//System.Threading.SpinWait.SpinUntil
			//(
			//	() => (System.Threading.Interlocked.CompareExchange(ref _BusyCount, int.MinValue, 0)) <= 0
			//);

			//// Atomically set our state to 'disposing' but only if it's not already in that state.
			//// If it is already in a 'disposing' state, wait for the dispose to complete before
			//// returning to the caller.
			//System.Threading.SpinWait.SpinUntil
			//(
			//	() => (priorState = System.Threading.Interlocked.CompareExchange(ref _DisposedState, DisposingState, UndisposedState)) != DisposingState
			//);

			//if (_DisposedState == DisposedState) return; //If someone else completed disposing the object, do nothing.

			//try
			//{
			//	Dispose(true);
			//}
			//finally
			//{
			//	//Call SuppressFinalize in case a derived class adds a finaliser.
			//	//Ensure it is done in the finally block. We're unlikely to be disposed again,
			//	//but if we are and we encountered an error last time, we're likely to get the 
			//	//same error again. Worse, we may also get the error when disposed from the 
			//	//finalizer thread, and throwing exceptions there is a bad idea (https://ericlippert.com/2015/05/18/when-everything-you-know-is-wrong-part-one/).
			//	//Additionally, code in a finally block will also execute if a thread abort 
			//	//occurs on the executing thread.
			//	GC.SuppressFinalize(this);

			//	//Now mark the object as disposed rather than disposing,
			//	System.Threading.Interlocked.Exchange(ref _DisposedState, DisposedState);
			//}
		}

		#endregion

		#region Public Methods

		/// <summary>
		/// Internally marks the object as busy, preventing the <see cref="Dispose()" /> method from executing until <see cref="ExitBusy"/> has been called.
		/// </summary>
		/// <remarks>
		/// <para><see cref="EnterBusy"/> and <see cref="ExitBusy"/> are a reference counting system. Each call to EnterBusy should have a matching ExitBusy call or the object will never be diposable. Either use a try/finally or <see cref="ObtainBusyToken()"/> in a using block to ensure correct handling of the busy state. Also do not call <see cref="ExitBusy"/> more often than <see cref="EnterBusy"/>.</para>
		/// <para>Using <see cref="ObtainBusyToken()"/> with a using block is the safest way of managing busy state.</para>
		/// </remarks>
		/// <exception cref="System.ObjectDisposedException">Thrown if the object is disposed or being disposed.</exception>
		/// <seealso cref="ExitBusy"/>
		/// <seealso cref="ObtainBusyToken"/>
		protected void EnterBusy()
		{
#pragma warning disable 420
			DisposeAssistant.EnterBusy(this, _DisposedState, ref _BusyCount);
#pragma warning restore 420
		}

		/// <summary>
		/// Reduces the count of current operations on the object, returning it to a non-busy state if no other work is in progress thereby allowing calls to <see cref="Dispose()"/> to complete.
		/// </summary>
		/// <seealso cref="EnterBusy"/>
		/// <seealso cref="ObtainBusyToken"/>
		/// <exception cref="System.ObjectDisposedException">Thrown if this object is already disposed or being disposed.</exception>
		protected void ExitBusy()
		{
#pragma warning disable 420
			DisposeAssistant.ExitBusy(this, _DisposedState, ref _BusyCount);
#pragma warning restore 420
		}

		/// <summary>
		/// Throws an <see cref="System.ObjectDisposedException"/> if the object is currently disposed, otherwise does nothing.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Deliberately thrown if this object has been or is being disposed.</exception>
		protected void ThrowIfDisposed()
		{
			DisposeAssistant.ThrowIfDisposed(this, _DisposedState, _BusyCount);
		}

		/// <summary>
		/// Calls <see cref="EnterBusy"/> then returns an instance of <see cref="DisposableValueToken"/> that will call <see cref="ExitBusy"/> when disposed.
		/// </summary>
		/// <remarks>
		/// <para>The returned token can be used in a using block to ensure correct enter/exit behaviour.</para>
		/// </remarks>
		/// <returns>A <see cref="DisposableValueToken"/> instance.</returns>
		/// <exception cref="System.ObjectDisposedException">Thrown if the object is disposed or being disposed.</exception>
		protected DisposableValueToken ObtainBusyToken()
		{
#pragma warning disable 420
			return DisposeAssistant.ObtainBusyToken(this, _DisposedState, ref _BusyCount, this.ExitBusy);
#pragma warning restore 420
		}

		/// <summary>
		/// Called from the <see cref="Dispose()" /> method to perform the actual dispose logic.
		/// </summary>
		/// <param name="disposing">Should be false if called from a finalizer, otherwise true. If true the <see cref="DisposeManagedResources"/> method will be called in addition to <see cref="DisposeUnmanagedResources"/>.</param>
		protected void Dispose(bool disposing)
		{
			try
			{
				if (disposing)
				{
					DisposeManagedResources();
					DisposableTracker.Unregister(this); // Only unregister if we were explicitly disposed. Otherwise we want to continue tracking so we can report on the failure to dispose correctly even if the object is finalised.
				}
			}
			finally
			{
				// Always attempt to dispose unmanaged resources, even in the event of an
				// exception disposing the managed ones. If an unexpected error occurred, then 
				// it is highly likely to occur again (or worse, a different error from the partial
				// disposed state) if another dispose call is attempted. Of course most code should
				// not dispose objects more than once, so it's most likely we'll never get called again
				// anyway. In either case, the unmanaged resources woud be leaked if this is not called
				// from a finally block. Additionally, code in a finally block will also execute
				// if a thread abort occurs on the executing thread.
				DisposeUnmanagedResources();
			}
		}

		/// <summary>
		/// Derived types should override this method to dispose other managed objects used internally.
		/// </summary>
		protected virtual void DisposeManagedResources() { }

		/// <summary>
		/// Derived types should override this method to dispose any (and only) unmanaged resources used internally, such as OS handles, unmanaged memory buffers etc.
		/// </summary>
		protected virtual void DisposeUnmanagedResources() { }

		#endregion

	}
}