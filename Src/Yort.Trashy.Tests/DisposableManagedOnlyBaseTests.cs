using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Yort.Trashy.Tests
{
	[TestClass]
	public class DisposableManagedOnlyBaseTests
	{

		[TestMethod]
		public void Dispose_IgnoresMultipleCalls()
		{
			var t = new TestDisposable();
			t.Dispose();
			Assert.AreEqual(1, t.DisposeCount);
			t.Dispose();
			Assert.AreEqual(1, t.DisposeCount);
		}

		[TestMethod]
		public void Dispose_CallsDisposeManagedResources()
		{
			var t = new TestDisposable();
			t.Dispose();
			Assert.AreEqual(true, t.ManagedDisposeCalled);
		}

		[TestMethod]
		public void Dispose_CallsDisposeUnmanagedResources()
		{
			var t = new TestDisposable();
			t.Dispose();
			Assert.AreEqual(true, t.UnmanagedDisposeCalled);
		}

		[TestMethod]
		public void Dispose_SetsIsDisposed()
		{
			var t = new TestDisposable();
			Assert.AreEqual(false, t.IsDisposed);
			t.Dispose();
			Assert.AreEqual(true, t.IsDisposed);
		}

		[TestMethod]
		public void Dispose_IsDisposedReturnsTrueWhileDisposeInProgress()
		{
			var t = new TestDisposable();
			t.SleepMilliseconds = 1000;
			using (var signal = new System.Threading.ManualResetEvent(false))
			{
				System.Threading.ThreadPool.QueueUserWorkItem(
					(reserved) =>
					{
						signal.Set();
						t.Dispose();
					}
				);
				signal.WaitOne();
				System.Threading.Thread.Sleep(100);

				Assert.AreEqual(0, t.DisposeCount);
				Assert.IsTrue(t.IsDisposed);
			}
		}

		[TestMethod]
		public void Dispose_SimultaneousDisposeWaitsForOriginalDisposeToComplete()
		{
			int disposeCallCount = 0;

			var t = new TestDisposable();
			t.SleepMilliseconds = 1000;
			using (var signal = new System.Threading.ManualResetEvent(false))
			{
				System.Threading.ThreadPool.QueueUserWorkItem(
					(reserved) =>
					{
						signal.Set();
						t.Dispose();
					}
				);
				signal.WaitOne();
				System.Threading.Thread.Sleep(100);

				for (int cnt = 0; cnt < 1000; cnt++)
				{
					disposeCallCount++;
					t.Dispose();
					if (t.IsDisposed) break;
				}
			}

			//disposeCallCount should be 1. We are calling t.Dispose on a single thread
			//and the first call inside the loop should block until the call first made
			//outside the loop has finished. The following IsDisposed check should
			//then break out of the loop. If disposeCallCount is ever greater than 1
			//then we failed to wait for the original dispose to complete, and this is a bug.
			Assert.AreEqual(1, disposeCallCount, "Called dispose multiple times in loop");
			Assert.AreEqual(1, t.DisposeCount, "Object was disposed multiple times");
		}

		[TestMethod]
		public void Dispose_BlockedWhileObjectBusyToken()
		{
			int doWorkDelayTimeInMs = 1000;
			var t = new TestDisposable();
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			using (var signal = new System.Threading.ManualResetEvent(false))
			{
				System.Threading.ThreadPool.QueueUserWorkItem(
					(reserved) =>
					{
						signal.Set();
						t.DoWorkWithBusyToken(doWorkDelayTimeInMs);
					}
				);
				signal.WaitOne();
				System.Threading.Thread.Sleep(16);

				t.Dispose();
			}
			sw.Stop();

			//If the test takes less time than the amout of time DoWork delayed for,
			//then dispose didn't stop/wait for the busy flag and this represents a bug.
			Assert.IsTrue(sw.Elapsed.TotalMilliseconds >= doWorkDelayTimeInMs);
		}

		[TestMethod]
		public void Dispose_BlockedWhileObjectBusyCalls()
		{
			int doWorkDelayTimeInMs = 1000;
			var t = new TestDisposable();
			var sw = new System.Diagnostics.Stopwatch();
			sw.Start();
			using (var signal = new System.Threading.ManualResetEvent(false))
			{
				System.Threading.ThreadPool.QueueUserWorkItem(
					(reserved) =>
					{
						signal.Set();
						t.DoWorkWithBusyCalls(doWorkDelayTimeInMs);
					}
				);
				signal.WaitOne();
				System.Threading.Thread.Sleep(16);

				t.Dispose();
			}
			sw.Stop();

			//If the test takes less time than the amout of time DoWork delayed for,
			//then dispose didn't stop/wait for the busy flag and this represents a bug.
			Assert.IsTrue(sw.Elapsed.TotalMilliseconds >= doWorkDelayTimeInMs);
		}

		[ExpectedException(typeof(ObjectDisposedException))]
		[TestMethod]
		public void Dispose_EnterBusyThrowsObjectDisposedExceptionIfDisposed()
		{
			var t = new TestDisposable();
			t.Dispose();
			t.DoWorkWithBusyToken(1000);
		}

		[ExpectedException(typeof(ObjectDisposedException))]
		[TestMethod]
		public void Dispose_ThrowIfDisposedThrowsObjectDisposedExceptionIfDisposed()
		{
			var t = new TestDisposable();
			t.Dispose();
			t.DoWork(1000);
		}

	}

	public class TestDisposable : DisposableBase
	{
		private int _DisposeCount;

		public int DisposeCount { get { return _DisposeCount; } }

		public int SleepMilliseconds { get; set; }

		public bool ManagedDisposeCalled { get; private set; }
		public bool UnmanagedDisposeCalled { get; private set; }
		public bool ErrorOnDispose { get; internal set; }
		public bool UseOutOfMemoryException { get; internal set; }

		public TestDisposable() : base() { }

		protected override void DisposeManagedResources()
		{
			if (ErrorOnDispose)
			{
				if (UseOutOfMemoryException)
					throw new OutOfMemoryException();
				else
					throw new UnauthorizedAccessException();
			}

			if (SleepMilliseconds > 0)
				System.Threading.Thread.Sleep(SleepMilliseconds);

			ManagedDisposeCalled = true;
			System.Threading.Interlocked.Increment(ref _DisposeCount);

			base.DisposeManagedResources();
		}

		protected override void DisposeUnmanagedResources()
		{
			UnmanagedDisposeCalled = true;

			base.DisposeUnmanagedResources();
		}

		public void DoWorkWithBusyToken(int delayInMilliseconds)
		{
			using (var token = ObtainBusyToken())
			{
				System.Threading.Thread.Sleep(delayInMilliseconds);
			}
		}

		public void DoWorkWithBusyCalls(int delayInMilliseconds)
		{
			EnterBusy();
			try
			{ 
				System.Threading.Thread.Sleep(delayInMilliseconds);
			}
			finally
			{
				ExitBusy();
			}
		}

		public void DoWork(int delayInMilliseconds)
		{
			ThrowIfDisposed();
			System.Threading.Thread.Sleep(delayInMilliseconds);
		}
	}
}
