using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;

namespace Yort.Trashy.Extensions
{
	/// <summary>
	/// Provides extension methods for disposing objects.
	/// </summary>
	public static class DisposeExtensions
	{
		/// <summary>
		/// Disposes <paramref name="disposable"/> if it is not null, otherwise does nothing.
		/// </summary>
		/// <param name="disposable"></param>
		public static void TryDispose(this IDisposable disposable)
		{
			TryDispose(disposable, TryDisposeOptions.None);
		}

		/// <summary>
		/// Disposes the <paramref name="disposable"/> if it not null using the options specified.
		/// </summary>
		/// <param name="disposable">The item to dispose.</param>
		/// <param name="options">One or more values from <see cref="TryDisposeOptions"/> that specify details about how to perform the dispose.</param>
		public static void TryDispose(this IDisposable disposable, TryDisposeOptions options)
		{
			try
			{
				disposable?.Dispose();
			}
			catch (OutOfMemoryException) { throw; }
			catch
			{
				if ((options & TryDisposeOptions.SuppressExceptions) != TryDisposeOptions.SuppressExceptions) throw;
			}
		}

		/// <summary>
		/// Attempts to cast <paramref name="value"/> to <see cref="IDisposable"/> and if success disposes it using <see cref="TryDispose(IDisposable)"/>.
		/// </summary>
		/// <param name="value">The value to check &amp; dispose if it supports <see cref="IDisposable"/>.</param>
		public static void TryDispose(this object value)
		{
			TryDispose(value as IDisposable);
		}

		/// <summary>
		/// Attempts to cast <paramref name="value"/> to <see cref="IDisposable"/> and if success disposes it using <see cref="TryDispose(IDisposable, TryDisposeOptions)"/>.
		/// </summary>
		/// <param name="value">The value to check &amp; dispose if it supports <see cref="IDisposable"/>.</param>
		/// <param name="options">One or more values from <see cref="TryDisposeOptions"/> that specify details about how to perform the dispose.</param>
		public static void TryDispose(this object value, TryDisposeOptions options)
		{
			TryDispose(value as IDisposable, options);
		}

		/// <summary>
		/// Disposes all items in an <see cref="IEnumerable{T}"/> that implement <see cref="IDisposable"/> Additionally, if the <paramref name="value"/> also implements <see cref="IDisposable"/> it too will be disposed.
		/// </summary>
		/// <remarks>
		/// <para>Null values will be ignored.</para>
		/// </remarks>
		/// <typeparam name="T">The type of value contained in the <see cref="IEnumerable{T}"/>. T must implement <see cref="IDisposable"/>.</typeparam>
		/// <param name="value">An <see cref="IEnumerable{T}"/> containing the items to be disposed.</param>
		public static void TryDispose<T>(this IEnumerable<T> value) where T : IDisposable
		{
			TryDispose<T>(value, TryDisposeOptions.None);
		}

		/// <summary>
		/// Disposes all items in an <see cref="IEnumerable{T}"/> that implement <see cref="IDisposable"/> Additionally, if the <paramref name="value"/> also implements <see cref="IDisposable"/> it too will be disposed.
		/// </summary>
		/// <remarks>
		/// <para>Null values will be ignored.</para>
		/// </remarks>
		/// <typeparam name="T">The type of value contained in the <see cref="IEnumerable{T}"/>. T must implement <see cref="IDisposable"/>.</typeparam>
		/// <param name="value">An <see cref="IEnumerable{T}"/> containing the items to be disposed.</param>
		/// <param name="options">One or more values from the <see cref="TryDisposeOptions"/> enum that specify rules for the disposal.</param>
		public static void TryDispose<T>(this IEnumerable<T> value, TryDisposeOptions options) where T : IDisposable
		{
			if (value == null) return;

			foreach (var item in value.Reverse())
			{
				item.TryDispose(options);
			}

			//If the enumerable itself is also disposable,
			//dispose it too.
			var disposableEnumerable = value as IDisposable;
			disposableEnumerable?.Dispose();
		}

		/// <summary>
		/// Disposes all items in an <see cref="IEnumerable"/> that implement <see cref="IDisposable"/> Additionally, if the <paramref name="value"/> also implements <see cref="IDisposable"/> it too will be disposed.
		/// </summary>
		/// <remarks>
		/// <para>Null values will be ignored.</para>
		/// </remarks>
		/// <param name="value">An <see cref="IEnumerable"/> containing the items to be disposed.</param>
		public static void TryDispose(this IEnumerable value)
		{
			TryDispose(value, TryDisposeOptions.None);
		}

		/// <summary>
		/// Disposes all items in an <see cref="IEnumerable"/> that implement <see cref="IDisposable"/> Additionally, if the <paramref name="value"/> also implements <see cref="IDisposable"/> it too will be disposed.
		/// </summary>
		/// <remarks>
		/// <para>Null values will be ignored.</para>
		/// </remarks>
		/// <param name="value">An <see cref="IEnumerable"/> containing the items to be disposed.</param>
		/// <param name="options">One or more values from the <see cref="TryDisposeOptions"/> enum that specify rules for the disposal.</param>
		public static void TryDispose(this IEnumerable value, TryDisposeOptions options) 
		{
			if (value == null) return;

			foreach (var item in value)
			{
				item.TryDispose(options);
			}

			//If the enumerable itself is also disposable,
			//dispose it too.
			var disposableEnumerable = value as IDisposable;
			disposableEnumerable?.Dispose();
		}

	}

	/// <summary>
	/// Options for handling how objects are disposed.
	/// </summary>
	/// <seealso cref="DisposeExtensions"/>
	[Flags]
	public enum TryDisposeOptions
	{
		/// <summary>
		/// No special handling or options.
		/// </summary>
		None = 0,
		/// <summary>
		/// Any exception other than <see cref="OutOfMemoryException"/> will be suppressed.
		/// </summary>
		SuppressExceptions = 2
	}
}