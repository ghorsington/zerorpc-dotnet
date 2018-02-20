using System;

namespace ZeroRpc.Net
{
    public class RemoteException : Exception
    {
        public string RemoteStackTrace { get; }
        
        public string ErrorName { get; }

        public RemoteException(string name, string message, string stackTrace) : base(message)
        {
            ErrorName = name;
            RemoteStackTrace = stackTrace;
        }
    }
}
