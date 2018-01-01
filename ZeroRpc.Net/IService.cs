using ZeroRpc.Net.Data;

namespace ZeroRpc.Net
{
    /// <summary>
    ///     A ZeroService that can be provided by <see cref="Server" />.
    /// </summary>
    /// <remarks>
    ///     A ZeroService is a service that is usually provided by a ZeroRPC server.
    ///     Essentially, a ZeroService exposes a range of methods that can be invoked through an RPC protocol.
    ///     A ZeroService also provides information about itself: the name of the service, the methods it provides and
    ///     the documentation of said methods.
    ///     ZeroRpc.Net provides some default implementations for this interface.
    ///     You can find them in the <see cref="ZeroRpc.Net.ServiceProviders" /> namespace.
    /// </remarks>
    public interface IService
    {
        /// <summary>
        ///     Infomation about this service.
        ///     Used by <see cref="Server" /> to provide built-in ZeroRPC inspector and documentation.
        /// </summary>
        ServiceInfo ServiceInfo { get; }

        /// <summary>
        ///     Process an invocation request for a ZeroService method.
        /// </summary>
        /// <param name="methodName">The name of the method to invoke.</param>
        /// <param name="args">A list of argument provided along the invokation request.</param>
        /// <param name="reply">A reply callback. Use this to reply to the invocation request.</param>
        void Invoke(string methodName, object[] args, Server.ReplyCallback reply);
    }
}