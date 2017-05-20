using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yort.Trashy.Tests
{
	[TestClass]
	public class DisposeTrackingTests
	{

		[TestMethod]
		public void OutputsUndisposedObject()
		{
			DisposableTracker.Enabled = false;
			DisposableTracker.Enabled = true;
			DisposableTracker.CaptureStackTraceAtCreation = false;

			var disposable = new TestDisposable();
			var sb = new StringBuilder();

			DisposableTracker.EnumerateTrackedInstances((td) => sb.Append(td.InstanceType.FullName + " created at " + td.CreationStackTrace + " Alive = " + td.IsAlive));
			Assert.IsTrue(sb.Length > 0);
			System.Diagnostics.Trace.WriteLine(sb.ToString());
		}

		[TestMethod]
		public void OutputsStackTraceWhenCaptured()
		{
			DisposableTracker.Enabled = false;
			DisposableTracker.Enabled = true;
			DisposableTracker.CaptureStackTraceAtCreation = true;

			var disposable = new TestDisposable();
			var sb = new StringBuilder();

			DisposableTracker.EnumerateTrackedInstances((td) => sb.Append(td.CreationStackTrace));
			Assert.IsTrue(sb.Length > 0);
			System.Diagnostics.Trace.WriteLine(sb.ToString());
		}

		[TestMethod]
		public void OutputsDoesNotOutputUnregisteredObject()
		{
			DisposableTracker.Enabled = false;
			DisposableTracker.Enabled = true;
			var sb = new StringBuilder();
			//DisposableTracker.CaptureStackTraceAtCreation = true;

			var disposable = new TestDisposable();
			disposable.Dispose();

			DisposableTracker.EnumerateTrackedInstances((str) => sb.Append(str));
			System.Diagnostics.Trace.WriteLine(sb.ToString());
			Assert.IsTrue(sb.Length == 0);
		}

		[TestMethod]
		public void OutputsFinalizedUndisposedObject()
		{
			DisposableTracker.Enabled = false;
			DisposableTracker.Enabled = true;
			var sb = new StringBuilder();
			//DisposableTracker.CaptureStackTraceAtCreation = true;

			var disposable = new TestDisposable();
			disposable = null;
			GC.Collect();
			GC.WaitForPendingFinalizers();

			DisposableTracker.EnumerateTrackedInstances((td) => sb.Append(td.InstanceType.FullName + " " + (td.IsAlive ? "Alive" : "Dead")));
			System.Diagnostics.Trace.WriteLine(sb.ToString());
			Assert.IsTrue(sb.Length > 0);
			Assert.IsTrue(sb.ToString().Contains("Dead"));
		}

		[TestMethod]
		public void DisposableRegistrationIsLogged()
		{
			DisposableTracker.Enabled = false;
			DisposableTracker.Enabled = true;
			var sb = new StringBuilder();
			//DisposableTracker.CaptureStackTraceAtCreation = true;
			DisposableTracker.DisposableRegisteredLogger = (a) => sb.AppendLine("Created " + a.InstanceType.FullName + " at " + DateTime.Now);

			try
			{
				var disposable = new TestDisposable();
				disposable.Dispose();

				System.Diagnostics.Trace.WriteLine(sb.ToString());
				Assert.IsTrue(sb.Length > 0);
				Assert.IsTrue(sb.ToString().StartsWith("Created " + typeof(TestDisposable).FullName));
			}
			catch (InvalidOperationException ioe)
			{
				var x = ioe;
			}
		}

		[TestMethod]
		public void DisposableDeregistrationIsLogged()
		{
			DisposableTracker.Enabled = false;
			DisposableTracker.Enabled = true;
			var sb = new StringBuilder();
			//DisposableTracker.CaptureStackTraceAtCreation = true;

			DisposableTracker.DisposableUnregisteredLogger = (a) =>
			{
				sb.AppendLine("Disposed " + a.Instance.GetType().FullName + " at " + a.CreationStackTrace + " at " + a.RegisteredAt + Environment.NewLine + a.State);
			};

			var disposable = new TestDisposable();
			disposable.Dispose();

			System.Diagnostics.Trace.WriteLine(sb.ToString());
			Assert.IsTrue(sb.Length > 0);
			Assert.IsTrue(sb.ToString().StartsWith("Disposed " + typeof(TestDisposable).FullName));
		}

		[TestMethod]
		public void RegisterNullDoesNothing()
		{
			DisposableTracker.RegisterInstance(null);
		}

		[TestMethod]
		public void UnregisterNullDoesNothing()
		{
			DisposableTracker.UnregisterInstance(null);
		}

		[TestMethod]
		public void TracksOnlyRegisteredTypes()
		{
			DisposableTracker.RegisterTrackedType(typeof(TestDisposable));
			using (var t = new TestDisposable())
			using (var t2 = new ReferenceCounted())
			{
				int trackedItems = 0;
				DisposableTracker.EnumerateTrackedInstances((td) => trackedItems++);

				Assert.AreEqual(1, trackedItems);
			}
		}


		[TestMethod]
		public void UnregisterTrackedTypeStopsTracking()
		{
			DisposableTracker.RegisterTrackedType(typeof(TestDisposable));
			DisposableTracker.RegisterTrackedType(typeof(ReferenceCounted));
			using (var t = new TestDisposable())
			using (var t2 = new ReferenceCounted())
			{
				//Confirm both items tracked
				int trackedItems = 0;
				DisposableTracker.EnumerateTrackedInstances((td) => trackedItems++);
				Assert.AreEqual(2, trackedItems);
				trackedItems = 0;

				//Remove one type, and check only one instance being tracked
				DisposableTracker.UnregisterTrackedType(typeof(TestDisposable));
				DisposableTracker.EnumerateTrackedInstances((td) => trackedItems++);
				Assert.AreEqual(1, trackedItems);
				trackedItems = 0;

				//Create another instance of the type NOT being tracked
				//and ensure still only one tracked instance.
				using (var t3 = new TestDisposable())
				{
					DisposableTracker.EnumerateTrackedInstances((td) => trackedItems++);
					Assert.AreEqual(1, trackedItems);
				}
			}
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RegisterTrackedTypeThrowsOnNull()
		{
			DisposableTracker.RegisterTrackedType(null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void UnregisterTrackedTypeThrowsOnNull()
		{
			DisposableTracker.UnregisterTrackedType(null);
		}

	}
}
