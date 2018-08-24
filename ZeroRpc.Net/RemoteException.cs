using System;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     An exception that occurred on the server during and RPC call.
    /// </summary>
    public class RemoteException : Exception
    {
        /// <summary>
        ///     Constructs a new exception
        /// </summary>
        /// <param name="name">Name of the error set by the server</param>
        /// <param name="message">Message assigned to the error</param>
        /// <param name="stackTrace">An optional stack trace for the error</param>
        public RemoteException(string name, string message, string stackTrace = "") : base(message)
        {
            ErrorName = name;
            RemoteStackTrace = stackTrace;
        }

        /// <summary>
        ///     Error name set by the server
        /// </summary>
        public string ErrorName { get; }

        /// <summary>
        ///     Stack trace from the server
        /// </summary>
        public string RemoteStackTrace { get; }
    }
}