using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZeroRpc.Net.Data;
using MethodInfo = System.Reflection.MethodInfo;

namespace ZeroRpc.Net.ServiceProviders
{
    /// <summary>
    ///     Specifies documentation for the method.
    ///     Used by <see cref="SimpleWrapperService{T}" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MethodDocumentationAttribute : Attribute
    {
        internal string Documentation;

        /// <summary>
        ///     Initializes the attribute.
        /// </summary>
        /// <param name="documentation">Method documentation.</param>
        public MethodDocumentationAttribute(string documentation)
        {
            Documentation = documentation;
        }
    }

    /// <summary>
    ///     A basic service provider exposes public methods of an object.
    /// </summary>
    /// <typeparam name="T">Type of the object that is exposed.</typeparam>
    /// <remarks>
    ///     The simple wrapper service is able to expose certain public methods found in <typeparamref name="T" />.
    ///     More precisely, public methods with no generic parameters and out/ref variables can be exposed.
    ///     Moreover, public properties (get/set) are exposed as well.
    ///     Exposed methods can be documented within the class that specifies them through the
    ///     <see cref="MethodDocumentationAttribute" />.
    /// </remarks>
    public class SimpleWrapperService<T> : IService
    {
        /// <summary>
        ///     Initializes an object wrapper service.
        /// </summary>
        /// <param name="obj">An instance of the object that will be used by this service.</param>
        public SimpleWrapperService(T obj)
        {
            ServiceImplementor = obj;
            ServiceInfo = new ServiceInfo {Methods = new Dictionary<string, Data.MethodInfo>()};
            Methods = new Dictionary<string, MethodData>();
            InitService();
        }

        /// <summary>
        ///     An instance that implements the exposed methods.
        /// </summary>
        public T ServiceImplementor { get; }

        private Dictionary<string, MethodData> Methods { get; }


        /// <inheritdoc />
        public void Invoke(string eventName, object[] args, Server.ReplyCallback reply)
        {
            if (Methods.TryGetValue(eventName, out MethodData data))
            {
                if (args.Length < data.NeededParameterCount || args.Length > data.parameters.Length)
                {
                    string argsString = data.NeededParameterCount == data.parameters.Length
                                            ? data.NeededParameterCount.ToString()
                                            : $"{data.NeededParameterCount} to {data.parameters.Length}";
                    reply(new ErrorInformation("TypeError", $"{eventName}() can take only {argsString} arguments"));
                    return;
                }

                for (int i = 0; i < args.Length; i++)
                {
                    Type t1 = args[i].GetType();
                    Type t2 = data.parameters[i].ParameterType;
                    if (!t2.IsAssignableFrom(t1))
                    {
                        reply(new ErrorInformation("NameError", $"Argument #{i} is of type {t1.FullName} but should be {t2.FullName}"));
                        return;
                    }
                }

                object[] passArgs = args;
                if (args.Length < data.parameters.Length)
                {
                    passArgs = new object[data.parameters.Length];
                    for (int i = 0; i < passArgs.Length; i++)
                        passArgs[i] = i < args.Length ? args[i] : Type.Missing;
                }

                object instance = data.method.IsStatic ? null : (object) ServiceImplementor;
                try
                {
                    object result = data.method.Invoke(instance, passArgs);
                    if (data.method.ReturnType == typeof(void))
                        result = true; // Return true to specify that the side-effect was completed nicely
                    reply(null, result);
                    return;
                }
                catch (TargetInvocationException te)
                {
                    reply(new ErrorInformation(te.InnerException.GetType().FullName,
                                               te.InnerException.Message,
                                               te.InnerException.StackTrace));
                }
                catch (Exception e)
                {
                    reply(new ErrorInformation("RemoteError", e.Message, e.StackTrace));
                }
            }
            reply(new ErrorInformation("NameError", $"The service does not provide {eventName}()."));
        }

        /// <inheritdoc />
        public ServiceInfo ServiceInfo { get; }

        protected virtual void AddMethod(MethodInfo methodInfo)
        {
            if (methodInfo.ContainsGenericParameters)
                    // Currently don't support generic parameters to keep it simple for now
                return;

            MethodData data = new MethodData {method = methodInfo, parameters = methodInfo.GetParameters()};
            data.NeededParameterCount = data.parameters.Count(p => !p.IsOptional);

            int i = 0;
            string name;
            do
            {
                name = $"{methodInfo.Name}{(i == 0 ? string.Empty : $"_{i}")}";
                i++;
            } while (Methods.ContainsKey(name));
            Methods.Add(name, data);
            RegisterMethod(name, ref data);
        }

        private void InitService()
        {
            Type objectType = typeof(T);
            ServiceInfo.Name = $"{objectType.FullName}";

            MethodInfo[] publicMethods = objectType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            foreach (MethodInfo methodInfo in publicMethods.Where(m => m.DeclaringType != typeof(object)))
                AddMethod(methodInfo);
        }

        private void RegisterMethod(string name, ref MethodData data)
        {
            ServiceInfo.Methods.Add(name,
                                    new Data.MethodInfo
                                    {
                                        Arguments = data.parameters.Select(p => new ArgumentInfo {Name = $"{p.Name}"}).ToList(),
                                        Documentation =
                                                $"{name}({string.Join(",", data.parameters.Select(p => $"{(p.IsOptional ? "[" : string.Empty)}{p.ParameterType.FullName}{(p.IsOptional ? "]" : string.Empty)}").ToArray())}) -> {data.method.ReturnType.FullName}{GetDocs(data.method)}"
                                    });
        }

        private string GetDocs(MethodInfo method)
        {
            object[] attrs = method.GetCustomAttributes(typeof(MethodDocumentationAttribute), true);
            return attrs.Length > 0 ? $"\n\n{((MethodDocumentationAttribute) attrs[0]).Documentation}" : string.Empty;
        }

        private struct MethodData
        {
            public MethodInfo method;
            public ParameterInfo[] parameters;
            public int NeededParameterCount;
        }
    }
}