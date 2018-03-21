using System.Threading;
using ZeroRpc.Net.Data;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     Helper methods for invoking remote procedures
    /// </summary>
    public static class InvokeMethods
    {
        /// <summary>
        ///     A wrapper for a synchronously called remote method.
        /// </summary>
        /// <typeparam name="T">Type of the return value.</typeparam>
        /// <param name="args">Arguments of the method, if any.</param>
        /// <returns>The result of the call.</returns>
        public delegate T RemoteMethod<out T>(params object[] args);

        /// <summary>
        ///     A wrapper for an asynchronously called remote method.
        /// </summary>
        /// <param name="callback">Completion callback.</param>
        /// <param name="args">Parameters of the method.</param>
        public delegate void RemoteMethodAsync(Client.InvokeCallback callback, params object[] args);


        /// <summary>
        ///     Invokes a remote method synchronously.
        /// </summary>
        /// <typeparam name="T">Type of the return value.</typeparam>
        /// <param name="client">Client that will invoke the methpd.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Arguments of the method.</param>
        /// <returns>Remote call's return value.</returns>
        public static T Invoke<T>(this Client client, string method, params object[] args)
        {
            ManualResetEvent mre = new ManualResetEvent(false);

            ErrorInformation errorInfo = null;
            object resultObj = null;

            void Callback(ErrorInformation error, object result, bool stream)
            {
                errorInfo = error;
                resultObj = result;
                mre.Set();
            }

            client.InvokeAsync(method, args, Callback);
            mre.WaitOne();

            if (errorInfo != null)
                throw new RemoteException(errorInfo.Name, errorInfo.Message, errorInfo.StackTrace);

            if (resultObj is T resultVal)
                return resultVal;
            return default(T);
        }

        /// <summary>
        ///     A handy method for calling <see cref="Invoke{T}" /> with return type <see cref="bool" />.
        ///     <p>
        ///         Use to call methods that return a value that indicates success -- that is, a boolean.
        ///     </p>
        /// </summary>
        /// <param name="client">Client that will invoke the method.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Arguments of the method.</param>
        public static void Invoke(this Client client, string method, params object[] args)
        {
            Invoke<bool>(client, method, args);
        }

        /// <summary>
        ///     A handy method for calling <see cref="Client.InvokeAsync"/> without a callback handler.
        ///     <p>
        ///         Use it when you don't need to know whether an async call succeeded.
        ///     </p>
        /// </summary>
        /// <param name="client">Client that will invoke the method.</param>
        /// <param name="method">Method name.</param>
        /// <param name="args">Arguments of the method.</param>
        public static void InvokeAsync(this Client client, string method, params object[] args)
        {
            client.InvokeAsync(method, args, null);
        }

        /// <summary>
        ///     Create a delegate for a remote method.
        /// </summary>
        /// <typeparam name="T">Type of the return value.</typeparam>
        /// <param name="client">Client that will invoke the method.</param>
        /// <param name="method">Method name.</param>
        /// <returns>A <see cref="RemoteMethod{T}" /> that will call <see cref="Invoke{T}" /> on the provided arguments.</returns>
        public static RemoteMethod<T> CreateDelegate<T>(this Client client, string method)
        {
            T Result(object[] args) => Invoke<T>(client, method, args);
            return Result;
        }

        /// <summary>
        ///     Creates a delegate for an asynchronously called remote method.
        /// </summary>
        /// <param name="client">Client that will invoke the method.</param>
        /// <param name="method">Method name.</param>
        /// <returns>
        ///     A <see cref="RemoteMethodAsync" /> delegate that will call <see cref="Client.InvokeAsync" /> on the provided
        ///     arguments.
        /// </returns>
        public static RemoteMethodAsync CreateAsyncDelegate(this Client client, string method)
        {
            return (callback, args) => client.InvokeAsync(method, args, callback);
        }
    }
}