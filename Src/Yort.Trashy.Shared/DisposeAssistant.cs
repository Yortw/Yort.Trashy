using System;
using System.Collections.Generic;
using System.Text;
using Yort.Trashy.Extensions;
using Yort.Trashy;

namespace Yort.Trashy
{
	/// <summary>
	/// Class providing static methods for assisting with implementing the dispose pattern from objects that can't inherit from <see cref="DisposableManagedOnlyBase"/> or <see cref="DisposableBase"/>.
	/// </summary>
	/// <remarks>
	/// See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for an example of using this class to implement the dispose pattern.
	/// </remarks>
	public static class DisposeAssistant
	{
		private const int UndisposedState = 0;
		private const int DisposingState = 1;
		private const int DisposedState = 2;

		/// <summary>
		/// This method should be called from the <see cref="IDisposable.Dispose"/> method of your class, to ensure correct, thread-safe implementation of the dispose pattern.
		/// </summary>
		/// <remarks>
		/// <example>
		/// <para>Your class needs to declare two integer fields, usually called DisposedState and BusyCount. These are used to track the state of each object instance with regards to the dispose pattern.</para>
		/// <para>The following represents an example of implemetning the dipose pattern &amp; dispose tracking using <see cref="DisposeAssistant"/>.</para>
		/// <code>
		/// class TestClass : IIsDisposed
		/// {
		///			private int _DisposedState;
		///			private int _BusyCount; 
		///			
		///			public TestClass()
		///			{
		///				DisposableTracker.Register(this); 
		///			}
		///			
		///			public bool IsDisposed { get { return DisposeAssistant.IsDisposed(_DisposedState); } }
		///			
		///     public void Dispose() 
		///     {
		///         DisposeAssistant.Dispose(this, ref _DisposedState, ref _BusyCount, () => this.Dispose(true));
		///     }
		///     
		///			public void Dispose(bool disposing)
		///			{
		///				try
		///				{
		///					if (disposing)
		///					{
		///						//TODO: Dispose any managed objects/resources here.
		///						//If your class is unsealed this should call a
		///						//virtual method that can be overridden by 
		///						//derived types to handle their own resources.
		///					}
		///					DisposableTracker.Unregister(this); 
		///				}
		///				finally
		///				{
		///					//TODO: Dispose any unmanaged resources here.
		///					//If your class is unsealed this should call
		///					//a virtual method to dispose unmanaged resources,
		///					//so derived types can override it to handle
		///					//their own resources.
		///				}
		///			}
		///			
		///		// Only required if you have or expect your derived types to have
		///		// unmanaged resources.
		/// 	~MyDisposable()
		/// 	{
		/// 		Dispose(false);
		/// 	}
		/// }
		/// </code>
		/// </example>
		/// </remarks>
		/// <param name="caller">A reference to the instance being dispsoed.</param>
		/// <param name="callerDisposeState">An integer, passed by reference, holding the dispose state of the instance.</param>
		/// <param name="busyCount">An integer, passed by reference, containing the number of threads currently acting on the instance.</param>
		/// <param name="innerDispose">An <see cref="Action{T}"/> taking a boolean that refers to the Dispose(bool) method of the instance being disposed.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="caller"/> or <paramref name="innerDispose"/> is null.</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1816:CallGCSuppressFinalizeCorrectly", Justification="Again, because we are using composed logic instead of inherited logic, we must call suppress finalize on something other than ourselves. CA docs say this is ok if we are explicitly managing lifetime of another object, which in this case we are.")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#", Justification="Ref is required to allow for thread safety, and because we must use composable code instead of inherited code there is not much choice in making this public parameter a ref")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "1#", Justification = "Ref is required to allow for thread safety, and because we must use composable code instead of inherited code there is not much choice in making this public parameter a ref")]
		public static void Dispose(IDisposable caller, ref int callerDisposeState, ref int busyCount, Action<bool> innerDispose)
		{
			if (caller == null) throw new ArgumentNullException(nameof(caller));
			if (innerDispose == null) throw new ArgumentNullException(nameof(innerDispose));
			if (callerDisposeState == DisposedState) return; //If we are already disposed, do nothing.

			//Wait until there is no work in progress on the object (via EnterBusy/ExitBusy)
			var spinner = new System.Threading.SpinWait();
			while (System.Threading.Interlocked.CompareExchange(ref busyCount, int.MinValue, 0) > 0)	{
				spinner.SpinOnce();
			}

			// Atomically set our state to 'disposing' but only if it's not already in that state.
			// If it is already in a 'disposing' state, wait for the dispose to complete before
			// returning to the caller.
			while (System.Threading.Interlocked.CompareExchange(ref callerDisposeState, DisposingState, UndisposedState) != UndisposedState)
			{
				if (callerDisposeState == DisposedState) return; //If someone else completed disposing the object, do nothing.

				spinner.SpinOnce();
			}

			//Need this check in case we're disposed after first check, but before while loop above,
			//in which case we don't enter the loop and the check inside it doesn't get executed.
			if (callerDisposeState == DisposedState) return; //If someone else completed disposing the object, do nothing.

			try
			{
				innerDispose(true);
			}
			finally
			{
				//Call SuppressFinalize in case a derived class adds a finaliser.
				//Ensure it is done in the finally block. We're unlikely to be disposed again,
				//but if we are and we encountered an error last time, we're likely to get the 
				//same error again. Worse, we may also get the error when disposed from the 
				//finalizer thread, and throwing exceptions there is a bad idea (https://ericlippert.com/2015/05/18/when-everything-you-know-is-wrong-part-one/).
				//Additionally, code in a finally block will also execute if a thread abort 
				//occurs on the executing thread.
				GC.SuppressFinalize(caller);

				//Now mark the object as disposed rather than disposing,
				System.Threading.Interlocked.Exchange(ref callerDisposeState, DisposedState);
			}

		}

		/// <summary>
		/// Throws an <see cref="System.ObjectDisposedException"/> with the full type name of <paramref name="caller"/> if the object is currently disposed, otherwise does nothing.
		/// </summary>
		/// <param name="caller">A reference to the calling (disposable) object.</param>
		/// <param name="busyCount">An integer belonging to the caller that indicates how many threads are busy operating on the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <param name="disposedState">An integer belonging to the caller that indicates the current disposed state of the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <exception cref="ObjectDisposedException">Deliberately thrown if this object has been or is being disposed.</exception>
		/// <example>
		/// <code>
		/// public void DoWork()
		/// {
		///		DisposeAssistant.ThrowIfDisposed(this, _DisposedState, _BusyCount);
		///		// Your actual method logic.
		/// }
		/// </code>
		/// </example>
		public static void ThrowIfDisposed(IDisposable caller, int disposedState, int busyCount)
		{
			if (caller == null) throw new ArgumentNullException(nameof(caller));

			if (disposedState > UndisposedState || busyCount < 0) throw new ObjectDisposedException(caller.GetType().FullName);
		}

		/// <summary>
		/// Records a busy state for an object, preventing the <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})" /> method from executing until <see cref="ExitBusy(IDisposable, int, ref int)"/> has been called.
		/// </summary>
		/// <remarks>
		/// <para><see cref="EnterBusy(IDisposable, int, ref int)"/> and <see cref="ExitBusy(IDisposable, int, ref int)"/> are a reference counting system. Each call to EnterBusy should have a matching ExitBusy call or the object will never be disposable. Either use a try/finally or <see cref="ObtainBusyToken(IDisposable, int, ref int, Action)"/> in a using block to ensure correct handling of the busy state. Also do not call <see cref="ExitBusy(IDisposable, int, ref int)"/> more often than <see cref="EnterBusy(IDisposable, int, ref int)"/>.</para>
		/// <para
		/// >Using <see cref="ObtainBusyToken(IDisposable, int, ref int, Action)"/> with a using block is the safest way of managing busy state.</para>
		/// </remarks>
		/// <exception cref="ObjectDisposedException">Thrown if the object is disposed or being disposed when this method is called.</exception>
		/// <param name="caller">A reference to the calling (disposable) object.</param>
		/// <param name="busyCount">An integer belonging to the caller that indicates how many threads are busy operating on the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <param name="disposedState">An integer belonging to the caller that indicates the current disposed state of the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <seealso cref="ExitBusy(IDisposable, int, ref int)"/>
		/// <seealso cref="ObtainBusyToken(IDisposable, int, ref int, Action)"/>
		/// <example>
		/// public void DoWork()
		/// {
		///		DisposeAssistant.EnterBusy(this, ref _DisposedState, ref _BusyCount);
		///		try
		///		{
		///			// Your method logic here. Calls to DisposeAssistant.Dispose will wait 
		///			// for methods using obtainbusytoken/enter/exitbusy to complete before actually
		///			// disposing the object.
		///		}
		///		finally
		///		{
		///			DisposeAssistant.ExitBusy(this, _DisposedState, ref _BusyCount);
		///		}
		/// }
		/// </example>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#", Justification = "Again, because we are using composed logic instead of inherited logic, we must call suppress finalize on something other than ourselves. CA docs say this is ok if we are explicitly managing lifetime of another object, which in this case we are.")]
		public static void EnterBusy(IDisposable caller, int disposedState, ref int busyCount)
		{
			ThrowIfDisposed(caller, disposedState, busyCount);
			System.Threading.Interlocked.Increment(ref busyCount);
			//Check disposed again, in case the object was disposed before or 
			//after our call to increment, in that case the busy count will now
			//be a very large negative number, so ThrowIfDisposed will still catch
			//it even with our increment having succeeded, and even if the disposed
			//state hasn't quite changed yet.
			ThrowIfDisposed(caller, disposedState, busyCount);
		}

		/// <summary>
		/// Reduces the count of current operations for an object, returning it to a non-busy state if no other work is in progress thereby allowing calls to <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> to complete.
		/// </summary>
		/// <param name="caller">A reference to the calling (disposable) object.</param>
		/// <param name="busyCount">An integer belonging to the caller that indicates how many threads are busy operating on the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <param name="disposedState">An integer belonging to the caller that indicates the current disposed state of the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <seealso cref="EnterBusy(IDisposable, int, ref int)"/>
		/// <seealso cref="ObtainBusyToken(IDisposable, int, ref int, Action)"/>
		/// <exception cref="ObjectDisposedException">Thrown if this object is already disposed or being disposed.</exception>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#", Justification = "Again, because we are using composed logic instead of inherited logic, we must call suppress finalize on something other than ourselves. CA docs say this is ok if we are explicitly managing lifetime of another object, which in this case we are.")]
		public static void ExitBusy(IDisposable caller, int disposedState, ref int busyCount)
		{
			ThrowIfDisposed(caller, disposedState, busyCount);
			System.Threading.Interlocked.Decrement(ref busyCount);
		}

		/// <summary>
		/// Calls <see cref="EnterBusy"/> then returns an instance of <see cref="DisposableValueToken"/> that will call <see cref="ExitBusy"/> when disposed.
		/// </summary>
		/// <param name="caller">A reference to the calling (disposable) object.</param>
		/// <param name="busyCount">An integer belonging to the caller that indicates how many threads are busy operating on the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <param name="disposedState">An integer belonging to the caller that indicates the current disposed state of the object. See <see cref="Dispose(IDisposable, ref int, ref int, Action{bool})"/> for how to implement this field.</param>
		/// <param name="exitBusy">A delegate calling the <see cref="ExitBusy(IDisposable, int, ref int)"/> method passing the internal state of the calling object.</param>
		/// <remarks>
		/// <para>The returned token can be used in a using block to ensure correct enter/exit behaviour.</para>
		/// </remarks>
		/// <returns>A <see cref="DisposableValueToken"/> instance.</returns>
		/// <exception cref="ObjectDisposedException">Thrown if the object is disposed or being disposed.</exception>
		/// <example>
		/// public void DoWork()
		/// {
		///		using (var busyToken = DisposeAssistant.ObtainBusyToken(this, _DisposedState, ref _BusyCount, ExitBusy)
		///		{
		///			//TODO: Your actual method logic here. Calls to DisposeAssistant.Dispose
		///			//will wait for all methods using obtainbusytoken/enterbusy/exitbusy 
		///			//to complete before actually disposing the object.
		///		}
		/// }
		/// 
		/// private void ExitBusy()
		/// {
		///		DisposeAssistant.ExitBusy(this, _DisposedState, ref _BusyCount);
		/// }
		/// </example>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference", MessageId = "2#")]
		public static DisposableValueToken ObtainBusyToken(IDisposable caller, int disposedState, ref int busyCount, Action exitBusy)
		{
			//Enter busy will check disposed, no need to recheck here.
			EnterBusy(caller, disposedState, ref busyCount);
			return new DisposableValueToken(exitBusy);
		}

		/// <summary>
		/// Returns true if the specified <paramref name="disposedState"/> indicates the object is disposed, or is actively being disposed.
		/// </summary>
		/// <param name="disposedState">An integer containing the disposed state of the object.</param>
		/// <returns>True if the specified state indicates the object is disposed or disposing.</returns>
		public static bool IsDisposed(int disposedState)
		{
			return disposedState > UndisposedState;
		}

		/// <summary>
		/// Calls <see cref="DisposeExtensions.TryDispose(IDisposable)"/> for each value provided in <paramref name="disposables"/>.
		/// </summary>
		/// <remarks>
		/// <para>If <paramref name="disposables"/> is null the method returns without doing anything.</para>
		/// </remarks>
		/// <param name="disposables">An array of objects to attempt disposable of.</param>
		/// <seealso cref="DisposeAll(DisposeOptions, object[])"/>
		/// <seealso cref="DisposeExtensions.TryDispose(IDisposable)"/>
		public static void DisposeAll(params object[] disposables)
		{
			DisposeAll(DisposeOptions.None, disposables);
		}

		/// <summary>
		/// Calls <see cref="DisposeExtensions.TryDispose(IDisposable)"/> for each value provided in <paramref name="disposables"/>.
		/// </summary>
		/// <remarks>
		/// <para>If <paramref name="disposables"/> is null the method returns without doing anything.</para>
		/// </remarks>
		/// <param name="options">One or more values from <see cref="DisposeOptions"/> controlling how disposal is performed.</param>
		/// <param name="disposables">An array of objects to attempt disposable of.</param>
		/// <seealso cref="DisposeAll(object[])"/>
		/// <seealso cref="DisposeExtensions.TryDispose(IDisposable, DisposeOptions)"/>
		public static void DisposeAll(DisposeOptions options, params object[] disposables)
		{
			if (disposables == null) return;

			List<Exception> exceptions = null;
			foreach (var item in disposables)
			{
				try
				{
					DisposeExtensions.TryDispose(item, options);
				}
				catch (Exception ex) when (!(ex is OutOfMemoryException))
				{
					if ((options & DisposeOptions.SuppressExceptions) != DisposeOptions.SuppressExceptions)
					{
						exceptions = exceptions ?? new List<Exception>();
						exceptions.Add(ex);
					}
				}
			}

			if (exceptions != null)
				throw new AggregateException(exceptions);
		}

	}
}