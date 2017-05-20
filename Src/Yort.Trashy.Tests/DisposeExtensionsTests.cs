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
	public class DisposeExtensionsTests
	{

		[TestMethod]
		public void TryDispose_WithNonDisposableObject_DoesNothing()
		{
			//This should do nothing/not error
			var o = new object();
			o.TryDispose();
		}

		[TestMethod]
		public void TryDispose_WithNonDisposableObjectAndOptions_DoesNothing()
		{
			//This should do nothing/not error
			var o = new object();
			o.TryDispose(DisposeOptions.SuppressExceptions);
		}

		[TestMethod]
		public void TryDispose_WithNullReference_DoesNothing()
		{
			//This should do nothing/not error
			object o = null;
			o.TryDispose();
		}

		[TestMethod]
		[ExpectedException(typeof(OutOfMemoryException))]
		public void TryDispose_Rethrows_OutOfMemory()
		{
			//This should do nothing/not error
			TestDisposable o = new TestDisposable()
			{
				ErrorOnDispose = true,
				UseOutOfMemoryException = true
			};
			o.TryDispose();
		}

		[TestMethod]
		public void TryDispose_WithNullObjectReference_DoesNothing()
		{
			//This should do nothing/not error
			Object o = null;
			o.TryDispose();
		}

		[TestMethod]
		public void TryDispose_WithNullObjectReferenceAndOptions_DoesNothing()
		{
			//This should do nothing/not error
			Object o = null;
			o.TryDispose(DisposeOptions.SuppressExceptions);
		}

		[TestMethod]
		public void TryDispose_WithNullDisposableReference_DoesNothing()
		{
			//This should do nothing/not error
			IDisposable d = null;
			d.TryDispose();
		}

		[TestMethod]
		public void TryDispose_DisposesDisposable()
		{
			//This should do nothing/not error
			var t = new TestDisposable();
			t.TryDispose();
		}

		[ExpectedException(typeof(UnauthorizedAccessException))]
		[TestMethod]
		public void TryDispose_RethrowsErrorWithNoneOption()
		{
			//This should do nothing/not error
			var t = new TestDisposable();
			t.ErrorOnDispose = true;
			t.TryDispose(DisposeOptions.None);
		}

		[TestMethod]
		public void TryDispose_SupressesExceptionWithSuppressOption()
		{
			//This should do nothing/not error
			var t = new TestDisposable();
			t.ErrorOnDispose = true;
			t.TryDispose(DisposeOptions.SuppressExceptions);
		}

		[TestMethod]
		public void IEnumerableOfT_TryDispose_DisposesAllITems()
		{
			var list = new List<TestDisposable>(5);
			for (int cnt = 0; cnt < 5; cnt++)
			{
				list.Add(new TestDisposable());
			}

			list.TryDispose(DisposeOptions.None);

			foreach (var item in list)
			{
				Assert.IsTrue(item.IsDisposed);
			}
		}

		[TestMethod]
		public void IEnumerable_TryDispose_DisposesAllITems()
		{
			var list = new System.Collections.ArrayList();
			for (int cnt = 0; cnt < 5; cnt++)
			{
				list.Add(new TestDisposable());
			}

			list.TryDispose(DisposeOptions.None);

			foreach (var item in list)
			{
				Assert.IsTrue((((IIsDisposed)item).IsDisposed));
			}
		}

		[TestMethod]
		public void IEnumerable_TryDispose_IgnoresNullEnumerable()
		{
			List<IDisposable> list = null;

			list.TryDispose(DisposeOptions.None);
		}

		[TestMethod]
		public void IEnumerable_TryDispose_DisposesChildrenAndEnumerableIfDisposable()
		{
			var list = new DisposableEnumerable();
			var t1 = new TestDisposable();
			var t2 = new TestDisposable();
			list.Add(t1);
			list.Add(t2);

			((IEnumerable<IDisposable>)list).TryDispose(DisposeOptions.None);

			Assert.IsTrue(list.IsDisposed);
			Assert.IsTrue(t1.IsDisposed);
			Assert.IsTrue(t2.IsDisposed);
		}

		[TestMethod]
		public void TryDispose_DisposesItemsInNonGenericEnumerable()
		{
			var list = new System.Collections.ArrayList();
			var t = new TestDisposable();
			var t1 = new TestDisposable();
			list.Add(t);
			list.Add(t1);

			list.TryDispose();

			Assert.IsTrue(t.IsDisposed);
			Assert.IsTrue(t1.IsDisposed);
		}

		[TestMethod]
		public void TryDispose_IgnoresNullNonGenericEnumerable()
		{
			System.Collections.ArrayList list = null;

			list.TryDispose();
		}

		[TestMethod]
		public void TryDispose_DisposesNonGenericEnumerable()
		{
			DisposableEnumerable list = new DisposableEnumerable();

			((System.Collections.IEnumerable)list).TryDispose();
		}

		[TestMethod]
		public void TryDispose_DisposesItemsInGenericEnumerable()
		{
			var list = new List<TestDisposable>();
			var t = new TestDisposable();
			var t1 = new TestDisposable();
			list.Add(t);
			list.Add(t1);

			list.TryDispose();

			Assert.IsTrue(t.IsDisposed);
			Assert.IsTrue(t1.IsDisposed);
		}

	}

	public class DisposableEnumerable : List<IDisposable>, IIsDisposed
	{

		public bool _IsDisposed;

		public bool IsDisposed => _IsDisposed;

		public void Dispose()
		{
			_IsDisposed = true;
		}
	}

}