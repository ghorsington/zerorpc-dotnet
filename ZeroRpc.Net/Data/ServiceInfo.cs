using System.Collections.Generic;
using MsgPack.Serialization;

namespace ZeroRpc.Net.Data
{
    /// <summary>
    ///     Container of the information about a ZeroService.
    /// </summary>
    public class ServiceInfo
    {
        /// <summary>
        ///     A map of methods that the ZeroSevice provides.
        /// </summary>
        [MessagePackMember(1, Name = "methods")]
        public Dictionary<string, MethodInfo> Methods { get; set; }

        /// <summary>
        ///     Name of the ZeroService.
        /// </summary>
        [MessagePackMember(0, Name = "name")]
        public string Name { get; set; }
    }

    /// <summary>
    ///     Information about method arguments.
    /// </summary>
    public class ArgumentInfo
    {
        /// <summary>
        ///     Name of the argument.
        /// </summary>
        [MessagePackMember(0, Name = "name")]
        public string Name { get; set; }
    }

    /// <summary>
    ///     Information about a method provided by a ZeroService.
    /// </summary>
    public class MethodInfo
    {
        /// <summary>
        ///     A list of arguments.
        /// </summary>
        [MessagePackMember(0, Name = "args")]
        public List<ArgumentInfo> Arguments { get; set; }

        /// <summary>
        ///     Optional documentation that describes the parameters and the functionality of the method.
        /// </summary>
        [MessagePackMember(1, Name = "doc")]
        public string Documentation { get; set; }
    }
}