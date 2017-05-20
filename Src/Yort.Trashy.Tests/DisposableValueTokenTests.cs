using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yort.Trashy.Tests
{
	[TestClass]
	public class DisposableValueTokenTests
	{
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ThrowsOnNullDisposeAction()
		{
			var token = new DisposableValueToken(null);
		}

		[TestMethod]
		public void CallsActionOnlyOnce()
		{
			int callCount = 0;
			var token = new DisposableValueToken(() => callCount++);
			token.Dispose();
			token.Dispose();
			Assert.AreEqual(1, callCount);
		}

		[TestMethod]
		public void InstanceEqualsItself()
		{
			int callCount = 0;
			var token = new DisposableValueToken(() => callCount++);
			var token2 = token;
			Assert.IsTrue(token == token2);
			Assert.IsTrue(token.Equals(token2));
		}

		[TestMethod]
		public void InstanceDoesNotEqualAnother()
		{
			int callCount = 0;
			var token = new DisposableValueToken(() => callCount++);
			var token2 = new DisposableValueToken(() => callCount++);
			Assert.IsTrue(token != token2);
			Assert.IsFalse(token.Equals(token2));
		}

	}
}