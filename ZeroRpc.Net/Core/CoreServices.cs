using System;
using System.Collections.Generic;
using System.Linq;

namespace ZeroRpc.Net.Core
{
    internal static class CoreServices
    {
        private static readonly Dictionary<string, Action<Server, object[], Server.ReplyCallback>> methods;

        static CoreServices()
        {
            methods = new Dictionary<string, Action<Server, object[], Server.ReplyCallback>>
            {
                {"_zerorpc_ping", (s, args, reply) => reply(null, new[] {"pong", s.Service.ServiceInfo.Name})},
                {"_zerorpc_inspect", (s, args, reply) => reply(null, s.Service.ServiceInfo)},
                {"_zerorpc_name", (s, args, reply) => reply(null, s.Service.ServiceInfo.Name)},
                {"_zerorpc_list", (s, args, reply) => reply(null, s.Service.ServiceInfo.Methods.Select(p => p.Key).ToList())},
                {"_zerorpc_help", (s, args, reply) => reply(null, s.Service.ServiceInfo.Methods[(string) args[0]].Documentation)},
                {"_zerorpc_args", (s, args, reply) => reply(null, s.Service.ServiceInfo.Methods[(string) args[0]].Arguments)}
            };
        }

        public static bool HasEvent(string name)
        {
            return methods.ContainsKey(name);
        }

        public static void Invoke(Server server, string eventName, object[] args, Server.ReplyCallback reply)
        {
            if (methods.TryGetValue(eventName, out var action))
                action(server, args, reply);
            else
                throw new NotImplementedException($"Core event {eventName} is not supported");
        }
    }
}