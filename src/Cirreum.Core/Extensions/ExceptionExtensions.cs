namespace System;

/// <summary>
/// Extension method for <see cref="Exception"/>
/// </summary>
public static class ExceptionExtensions {

	extension<TException>(TException ex) where TException : Exception {

		/// <summary>
		/// Determines if this exception is considered "fatal"
		/// </summary>
		/// <returns><see langword="true"/> if the exception is <see cref="OutOfMemoryException"/>
		/// or <see cref="StackOverflowException"/> or <see cref="ThreadAbortException"/></returns>
		internal bool IsFatal() =>
			ex is OutOfMemoryException or
			StackOverflowException or
			ThreadAbortException;
	}

}