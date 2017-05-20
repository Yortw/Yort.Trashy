using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yort.Trashy.Tests
{
	[TestClass]
	public class ReferenceCountedDisposableBaseTests
	{

		[TestMethod]
		public void ActuallyDisposesWithOnlyOneReference()
		{
			var disposable = new ReferenceCounted();
			disposable.Dispose();
			Assert.IsTrue(disposable.IsDisposed);
		}

		[TestMethod]
		public void DoesNotDisposeUntilCountReachesZero()
		{
			var disposable = new ReferenceCounted();
			for (int cnt = 0; cnt < 100; cnt++)
			{
				disposable.AddReference();
			}

			for (int cnt = 0; cnt < 100; cnt++)
			{
				disposable.Dispose();
				Assert.IsFalse(disposable.IsDisposed);
			}

			disposable.Dispose();
			Assert.IsTrue(disposable.IsDisposed);
		}

		[TestMethod]
		public void DisposeTokenDisposesWhenCountReachesZero()
		{
			var disposable = new ReferenceCounted();
			var token = disposable.CreateReferenceToken();
			disposable.Dispose();

			Assert.IsFalse(disposable.IsDisposed);
			token.Dispose();
			Assert.IsTrue(disposable.IsDisposed);
		}

		[TestMethod]
		public void DisposeTokenDoesntDecrementCountMuiltipleTimes()
		{
			var disposable = new ReferenceCounted();
			var token = disposable.CreateReferenceToken();
			
			token.Dispose();
			Assert.IsFalse(disposable.IsDisposed);
			token.Dispose();
			Assert.IsFalse(disposable.IsDisposed);
			token.Dispose();
			Assert.IsFalse(disposable.IsDisposed);
			token.Dispose();
			Assert.IsFalse(disposable.IsDisposed);

			disposable.Dispose();
			Assert.IsTrue(disposable.IsDisposed);
		}

		[TestMethod]
		public void OnlyDisposesOnce()
		{
			var disposable = new ReferenceCounted();
			disposable.Dispose();
			disposable.Dispose();
			Assert.IsTrue(disposable.IsDisposed);
		}

	}

	public class ReferenceCounted : ReferenceCountedDisposableBase
	{
		private int _DisposedCount;

		public int DisposedCount { get => _DisposedCount; }

		protected override void DisposeManagedResources()
		{
			_DisposedCount++;
		}
	}
}