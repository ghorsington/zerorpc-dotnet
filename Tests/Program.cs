using System;
using System.Collections.Generic;
using System.Globalization;
using ZeroRpc.Net;
using ZeroRpc.Net.Data;
using ZeroRpc.Net.ServiceProviders;

namespace Tests
{
    public class TestService : IService
    {
        public TestService()
        {
            ServiceInfo = new ServiceInfo
            {
                Name = "Test Service",
                Methods = new Dictionary<string, MethodInfo>
                {
                    {"test", new MethodInfo {Documentation = "Test method", Arguments = new List<ArgumentInfo>()}}
                }
            };
        }

        public void Invoke(string eventName, object[] args, Server.ReplyCallback reply)
        {
            if (eventName != "test")
                throw new NotImplementedException();
            reply(null, "Hello, world!");
        }

        public ServiceInfo ServiceInfo { get; }
    }

    public class TestObject
    {
        public string Test2 { [MethodDocumentation("Gets a set test message!")] get; [MethodDocumentation("Sets a test message!")] set; }

        [MethodDocumentation("Returns 'Hello, world!' string")]
        public string Test()
        {
            return "Hello, world!";
        }

        [MethodDocumentation("Converts the provided object to string using server's culture info and returns the said string.")]
        public string Echo(object value)
        {
            return value.ToString();
        }

        [MethodDocumentation(
            "Converts the provided float to the specified format and culture.\n\nBy default, the format is 'N' and culture is 'current'.\nValid cultures are 'current' and 'invariant' and any kind of specific culture identifier.")]
        public string Echo(double value, string format = "N", string culture = "current")
        {
            IFormatProvider provider;
            switch (culture)
            {
                case "current":
                    provider = CultureInfo.CurrentCulture;
                    break;
                case "invariant":
                    provider = CultureInfo.InvariantCulture;
                    break;
                default:
                    provider = CultureInfo.CreateSpecificCulture(culture);
                    break;
            }

            return value.ToString(format, provider);
        }
    }

    public class Program
    {
        private static void Main(string[] args)
        {
            Server s = new Server(new SimpleWrapperService<TestObject>(new TestObject()));
            s.Bind("tcp://127.0.0.1:1234");
            Console.WriteLine("Server started");
            s.Error += (sender, errorArgs) => { Console.WriteLine($"Error: {errorArgs.Info}"); };
            Console.Read();
            Console.WriteLine("Closing");
            s.Dispose();
        }

        private static void TestServer()
        {
            Server s = new Server(new TestService(), Client.DefaultHeartbeat);
            s.Bind("tcp://127.0.0.1:1234");
            Console.WriteLine("Server started");
            s.Error += (sender, errorArgs) => { Console.WriteLine($"Error: {errorArgs.Info}"); };
            Console.Read();
            Console.WriteLine("Closing");
            s.Dispose();
        }


        private static void TestClient()
        {
            Client c = new Client(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(3));
            c.Error += (sender, errorArgs) => { Console.WriteLine($"Error: {errorArgs.Info}"); };
            c.Connect("tcp://127.0.0.1:1234");
            c.InvokeAsync("asd",
                          new object[] {"%Y/%m/%d"},
                          (error, result, stream) =>
                          {
                              Console.WriteLine($"Error: {error}, result: {result} (type: {result?.GetType()}), stream: {stream}");
                          });
            c.InvokeAsync("clock",
                          new object[0],
                          (e, r, s) => { Console.WriteLine($"Error: {e}, result: {r} (type: {r?.GetType()}), stream: {s}"); });
        }
    }
}