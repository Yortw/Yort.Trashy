using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yort.Trashy.Extensions;

namespace Yort.Trashy.Tests
{
	[TestClass]
	public class DisposeAssistantTests
	{

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ThrowIfDisposedThrowsOnArgumentNull()
		{
			int disposedState = 0;
			int busyCount = 0;
			DisposeAssistant.ThrowIfDisposed(null, disposedState, busyCount);
		}

		[TestMethod]
		public void DisposeAll_DisposesEachitem()
		{
			var t = new TestDisposable();
			var t1 = new TestDisposable();
			var t2 = new TestDisposable();
			var t3 = new TestDisposable();

			DisposeAssistant.DisposeAll(t, t1, t2, t3);

			Assert.IsTrue(t.IsDisposed);
			Assert.IsTrue(t1.IsDisposed);
			Assert.IsTrue(t2.IsDisposed);
			Assert.IsTrue(t3.IsDisposed);
		}

		[TestMethod]
		public void DisposeAll_IgnoresNulls()
		{
			var t = new TestDisposable();
			var t1 = new TestDisposable();
			var t2 = new TestDisposable();
			var t3 = new TestDisposable();

			DisposeAssistant.DisposeAll(null, t, null, t1, t2, t3, null);

			Assert.IsTrue(t.IsDisposed);
			Assert.IsTrue(t1.IsDisposed);
			Assert.IsTrue(t2.IsDisposed);
			Assert.IsTrue(t3.IsDisposed);
		}

		[TestMethod]
		public void DisposeAll_ThrowsAggregateExceptionWhenNotSuppressed()
		{
			var t = new TestDisposable();
			var t1 = new TestDisposable();
			t1.ErrorOnDispose = true;
			var t2 = new TestDisposable();
			t2.ErrorOnDispose = true;
			var t3 = new TestDisposable();

			try
			{
				DisposeAssistant.DisposeAll(t, t1, t2, t3);
				Assert.Fail("No aggregate exception thronw");
			}
			catch (AggregateException ae)
			{
				Assert.AreEqual(2, ae.InnerExceptions.Count);

				Assert.IsTrue(t.IsDisposed);
				Assert.IsTrue(t1.IsDisposed);
				Assert.IsTrue(t2.IsDisposed);
				Assert.IsTrue(t3.IsDisposed);
			}
		}

		[TestMethod]
		public void DisposeAll_IgnoresExceptionsWithSuppressOptionSet()
		{
			var t = new TestDisposable();
			var t1 = new TestDisposable();
			t1.ErrorOnDispose = true;
			var t2 = new TestDisposable();
			var t3 = new TestDisposable();

			DisposeAssistant.DisposeAll(DisposeOptions.SuppressExceptions, t, t1, t2, t3);

			Assert.IsTrue(t.IsDisposed);
			Assert.IsTrue(t1.IsDisposed);
			Assert.IsTrue(t2.IsDisposed);
			Assert.IsTrue(t3.IsDisposed);
		}

		[TestMethod]
		public void DisposeAll_IgnoresNullEnumerableWithOptions()
		{
			DisposeAssistant.DisposeAll(DisposeOptions.SuppressExceptions, (object[])null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Dispose_ThrowsOnNullCaller()
		{
			int disposedState = 0, busyCount = 0;
			DisposeAssistant.Dispose(null, ref disposedState, ref busyCount, new Action<bool>((b)  => System.Diagnostics.Trace.WriteLine(b.ToString())));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Dispose_ThrowsOnNullInnerDisposeDelegate()
		{
			int disposedState = 0, busyCount = 0;
			DisposeAssistant.Dispose(new TestDisposable(), ref disposedState, ref busyCount, null);
		}

	}
}