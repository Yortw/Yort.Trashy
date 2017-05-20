using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace Yort.Trashy
{
	/// <summary>
	/// Keeps track of disposable objects and allows logging of undisposed objects.
	/// </summary>
	/// <remarks>
	/// <para>These methods are used automatically by the base types (<see cref="DisposableManagedOnlyBase"/>, <see cref="ReferenceCountedDisposableBase"/>, <see cref="DisposableBase"/>) to provide tracking services. If implementing your own types without these base classes you will need to call <see cref="RegisterInstance(Type, IDisposable)"/> from the constructor and <see cref="UnregisterInstance(Type, object)"/> from the dispose method.</para>
	/// <para>The <see cref="Enabled"/> can be used to enable to disable tracking, and is off by default to avoid the performance and memory overheads associated with tracking. The <see cref="RegisterTrackedType(Type)"/> and <see cref="UnregisterTrackedType(Type)"/> methods can be used to enable tracking for only specific types. If no types are registered (default state) then all types are tracked, if one or more types are added via <see cref="RegisterTrackedType(Type)"/> then only the types explicitly added are tracked.</para>
	/// </remarks>
	public static class DisposableTracker
	{

		#region Fields

		private static bool s_Enabled;

		private static Dictionary<Type, Dictionary<int, TrackedDisposable>> _TrackedInstances = new Dictionary<Type, Dictionary<int, TrackedDisposable>>();
		private static Dictionary<Type, Type> _TrackedTypes = new Dictionary<Type, Type>();

		#endregion

		#region Configuration Properties & Methods

		/// <summary>
		/// Specifies whether or not tracking is enabled. 
		/// </summary>
		/// <remarks>
		/// <para>Tracking is disabled by default as it incurs a certain performance overhead. Setting enabled to false after it has already been set as true will reset all internal state and release objects used for tracking so they can be garbage collected.</para>
		/// <para>Stacktraces are currently supported on</para>
		/// <list type="Bullet">
		/// <item>Full Desktop Framework (4+, not portable)</item>
		/// <item>Xamarin Android (not portable)</item>
		/// <item>Xamarin iOS (not portable)</item>
		/// </list>
		/// </remarks>
		public static bool Enabled
		{
			get { return s_Enabled; }
			set
			{
				s_Enabled = value;
				if (!value)
				{
					lock (_TrackedInstances)
					{
						_TrackedInstances.Clear();
					}
				}
			}
		}

		/// <summary>
		/// If true the stacktrace for creation of each item tracked will be captured and output. This will incur a more significant performance overhead in tracing.
		/// </summary>
		/// <remarks>
		/// <para>Stacktraces are not supported on every platform. On platforms where a stacktrace cannot be captured setting this property does nothng and the <see cref="TrackedDisposable.CreationStackTrace"/> value will be null.</para>
		/// </remarks>
		public static bool CaptureStackTraceAtCreation { get; set; }

		/// <summary>
		/// A delegate that is called each time an object instance is registered with the tracker.
		/// </summary>
		/// <remarks>
		/// <para>The default value is null which means no logging will be performed when instances are registered.</para>
		///	<para>Application code can provide a <see cref="Action{TrackedDisposable}"/> delegate that will be called when an object is successfully registered via <see cref="RegisterInstance(IDisposable)"/>.  This can be used with <see cref="DisposableUnregisteredLogger"/> and <see cref="EnumerateTrackedInstances(Action{TrackedDisposable})"/> to help track object lifetimes.</para>
		/// <para>Application code can set <see cref="TrackedDisposable.State"/> to store identifying properties or information about the object instance that are useful for tracking which instance it is when analysing logs.</para>
		/// </remarks>
		public static Action<TrackedDisposable> DisposableRegisteredLogger { get; set; }

		/// <summary>
		/// A delegate that is called each time an object instance is unregistered with the tracker.
		/// </summary>
		/// <remarks>
		/// <para>The default value is null which means no logging will be performed when instances are unregistered.</para>
		/// <para>Application code can provide a <see cref="Action{TrackedDisposable}"/> delegate which will be called from <see cref="UnregisterInstance(IDisposable)"/> when an instance is successfully unregistered. This can be used with <see cref="DisposableRegisteredLogger"/> and <see cref="EnumerateTrackedInstances(Action{TrackedDisposable})"/> to help track object lifetimes.</para>
		/// </remarks>
		public static Action<TrackedDisposable> DisposableUnregisteredLogger { get; set; }

		#endregion

		#region Type Registration Methods

		/// <summary>
		/// Adds a .Net type to track. The type must implement <see cref="IDisposable"/>.
		/// </summary>
		/// <remarks>
		/// <para>If not types are explicitly registered via this method then all participating types are tracked. If this method is used to add specific types to track then only those types are tracked.</para>
		/// <para>The <see cref="Enabled"/> property overrides type specific tracking, if it is false no tracking is performed regardless of whether types are registered or not.</para>
		/// </remarks>
		/// <seealso cref="UnregisterTrackedType(Type)"/>
		/// <see cref="Enabled"/>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
		public static void RegisterTrackedType(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			lock (_TrackedTypes)
			{
				_TrackedTypes[type] = type;
			}
		}

		/// <summary>
		/// Removes a type from the list of types being tracked. If no types are explicitly registered after this call then all participating types are tracked.
		/// </summary>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
		/// <seealso cref="Enabled"/>
		/// <seealso cref="RegisterTrackedType(Type)"/>
		public static void UnregisterTrackedType(Type type)
		{
			if (type == null) throw new ArgumentNullException(nameof(type));

			lock (_TrackedTypes)
			{
				_TrackedTypes.Remove(type);
			}

			lock (_TrackedInstances)
			{
				_TrackedInstances.Remove(type);
			}
		}

		#endregion

		#region Tracking Methods

		/// <summary>
		/// Registers an object instance for tracking.
		/// </summary>
		/// <param name="value">The instance to track.</param>
		/// <para>If you are calling this method from your own <see cref="IDisposable"/> implementation it should be called from the constructor.</para>
		/// <remarks>
		/// <para>Null values are ignored.</para>
		/// </remarks>
		public static void RegisterInstance(IDisposable value)
		{
			if (value == null) return;

			RegisterInstance(value.GetType(), value);
		}

		/// <summary>
		/// Unregisters an object instance for tracking.
		/// </summary>
		/// <remarks>
		/// <para>Null values are ignored.</para>
		/// <para>If you are calling this method from your own <see cref="System.IDisposable"/> implementation then it should be called from Dispose(bool) and only when the boolean argument is true. This enables tracking whether the object was correctly disposed, or (incorrectly) disposed via a finalizer.</para>
		/// </remarks>
		/// <param name="value">The instance to stop tracking.</param>
		public static void UnregisterInstance(IDisposable value)
		{
			if (value == null) return;
			if (!Enabled) return;

			UnregisterInstance(value.GetType(), value);
		}

		/// <summary>
		/// Calls the provided delegate (<paramref name="writeAction"/>) for every currently tracked object, allowing the current state to be logged and/or analysed.
		/// </summary>
		/// <remarks>
		/// <para>If <paramref name="writeAction"/> is null, then it is ignored and no exception is thrown.</para>
		/// <para>In addition to calling <paramref name="writeAction"/> (if it is not null) this method also removes any objects that are disposed but still tracked, or that have been finalized without being disposed (after the call to <paramref name="writeAction"/> which ensures these instances are still logged).</para>
		/// </remarks>
		/// <param name="writeAction">A <see cref="Action{TrackedDisposable}"/> delegate called once for every currently tracked object.</param>
		public static void EnumerateTrackedInstances(Action<TrackedDisposable> writeAction)
		{
			if (!Enabled) return;

			lock (_TrackedInstances)
			{
				foreach (var kvp in _TrackedInstances)
				{
					if (kvp.Value.Count == 0) continue;

					var keysToRemove = new List<int>();
					var instances = kvp.Value;
					lock (instances)
					{
						foreach (var instanceKvp in instances)
						{
							var item = instanceKvp.Value.Instance;
							var isDisposed = ((IIsDisposed)item)?.IsDisposed ?? false;

							if (isDisposed || !instanceKvp.Value.IsAlive)
								keysToRemove.Add(instanceKvp.Key);

							if (!isDisposed) // Only report undisposed instances (dead or alive)
								writeAction?.Invoke(instanceKvp.Value);	
						}

						foreach (var key in keysToRemove)
						{
							instances.Remove(key);
						}
					}
				}
			}
		}

		#endregion

		#region Private Methods

		private static void RegisterInstance(Type key, IDisposable value)
		{
			if (!Enabled) return;

			lock (_TrackedTypes)
			{
				if (_TrackedTypes.Count > 0 && !_TrackedTypes.ContainsKey(key)) return;
			}

			//Generate these outside of lock to minimize contention
			var trackedDisposable = new TrackedDisposable(value, key)
			{
				CreationStackTrace = (CaptureStackTraceAtCreation ? GetCreationStackTrace() : null)	
			};

			//Minimize contention by seperately locking tracked instances and the internal list
			Dictionary<int, TrackedDisposable> undisposedInstances = null;
			lock (_TrackedInstances)
			{
				if (!_TrackedInstances.TryGetValue(key, out undisposedInstances))
				{
					undisposedInstances = new Dictionary<int, TrackedDisposable>();
					_TrackedInstances.Add(key, undisposedInstances);
				}
			}

			var hash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
			lock (undisposedInstances)
			{
				undisposedInstances[hash] = trackedDisposable;
			}

			DisposableRegisteredLogger?.Invoke(trackedDisposable);
		}

		private static void UnregisterInstance(Type key, object value)
		{
			//Minimize contention by seperately locking tracked instances and the internal list
			Dictionary<int, TrackedDisposable> undisposedInstances = null;
			lock (_TrackedInstances)
			{
				_TrackedInstances.TryGetValue(key, out undisposedInstances);
			}

			if (undisposedInstances == null) return;
			var hash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);

			TrackedDisposable trackedItem = null;
			lock (undisposedInstances)
			{
				if (undisposedInstances.TryGetValue(hash, out trackedItem))
					undisposedInstances.Remove(hash);
			}

			if (trackedItem != null)
				DisposableUnregisteredLogger?.Invoke(trackedItem);
		}

		private static string GetCreationStackTrace()
		{
#if SUPPORTS_STACKTRACE
			try
			{
				var frames = new StackTrace(true).GetFrames();

				//Attempt to skip all stack frames up to the
				//first constructor call of the instance.
				//This leavse us just the stacktrace to the 
				//method where hte object was created.
				var framesUpToConstructor =
				(
					from frame
					in frames
					let method = frame.GetMethod()
					where method != null
						&& method.DeclaringType != typeof(DisposableTracker)
						&& !method.IsConstructor
					select frame
				);

				return String.Join(" at ", (from f in framesUpToConstructor select f.ToString()));
			}
			catch (Exception ex) when (ex is NotImplementedException || ex is NotSupportedException)
			{
				// If the platform lies about implementing Stacktrace() or other related methods,
				// then return null instead of crashing.
				return null;
			}
#else
			return null;
#endif
		}

#endregion
	}

	/// <summary>
	/// Struct passed to <see cref="DisposableTracker.DisposableUnregisteredLogger"/> when <see cref="DisposableTracker.UnregisterInstance(IDisposable)"/> is called because an object is being disposed, providing details of the object that was correctly disposed.
	/// </summary>
	public sealed class TrackedDisposable
	{

		private readonly WeakReference _WeakDisposable;
		private readonly Type _InstanceType;

		/// <summary>
		/// Main constructor.
		/// </summary>
		/// <param name="disposable">The object instance to track.</param>
		/// <param name="instanceType">The actual type of <paramref name="disposable"/>.</param>
		internal TrackedDisposable(IDisposable disposable, Type instanceType)
		{
			_WeakDisposable = new WeakReference(disposable);
			_InstanceType = instanceType;
		}

		/// <summary>
		/// The object instance that is/was being tracked. May return null if the object has been finalized without being disposed.
		/// </summary>
		/// <seealso cref="IsAlive"/>
		public object Instance
		{
			get
			{
				var retVal = _WeakDisposable.Target;
				if (!_WeakDisposable.IsAlive)
					retVal = null;

				return retVal;
			}
		}

		/// <summary>
		/// Returns true if the item being tracked still has references, otherwise returns false indicating the item is being finalized and has no references.
		/// </summary>
		public bool IsAlive
		{
			get { return _WeakDisposable.IsAlive; }
		}

		/// <summary>
		/// The stack trace at the time the object was created, if captured, otherwise null.
		/// </summary>
		public string CreationStackTrace { get; internal set; }

		/// <summary>
		/// The date and time the object was registered according to the local system clock.
		/// </summary>
		/// <remarks>
		/// <para>Normally this value roughly coincides with the time the object instance was created, as registration is normally performed by a constructor.</para>
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
		public DateTime RegisteredAt { get; private set; } = DateTime.Now;

		/// <summary>
		/// An string (or null) set by the <see cref="DisposableTracker.DisposableRegisteredLogger"/> delegate when the object was registered. This should contain information useful for identifying the object when it appears in logs as not having been disposed, but is already finalized and so the object itself cannot be examined.
		/// </summary>
		public string State { get; set; }

		/// <summary>
		/// Returns a <see cref="System.Type"/> representing the type of instance that was tracked by this object. Required in the case <see cref="Instance"/> is null because it has been finalized.
		/// </summary>
		public Type InstanceType { get { return _InstanceType; } }
	}

}