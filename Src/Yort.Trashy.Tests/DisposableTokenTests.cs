using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yort.Trashy.Tests
{
	[TestClass]
	public class DisposableTokenTests
	{
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ThrowsOnNullDisposeAction()
		{
			var token = new DisposableToken(null);
		}
	}
}