// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.md in the project root for license information.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.AspNet.SignalR.Client.Hubs
{
    public class HubProxy : IHubProxy
    {
        private readonly string _hubName;
        private readonly IHubConnection _connection;
        private readonly Dictionary<string, JToken> _state = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Subscription> _subscriptions = new Dictionary<string, Subscription>(StringComparer.OrdinalIgnoreCase);

        public HubProxy(IHubConnection connection, string hubName)
        {
            _connection = connection;
            _hubName = hubName;
        }

        public JToken this[string name]
        {
            get
            {
                lock (_state)
                {
                    JToken value;
                    _state.TryGetValue(name, out value);
                    return value;
                }
            }
            set
            {
                lock (_state)
                {
                    _state[name] = value;
                }
            }
        }

        public JsonSerializer JsonSerializer
        {
            get { return _connection.JsonSerializer; }
        }

        public Subscription Subscribe(string eventName)
        {
            if (eventName == null)
            {
                throw new ArgumentNullException("eventName");
            }

            Subscription subscription;
            if (!_subscriptions.TryGetValue(eventName, out subscription))
            {
                subscription = new Subscription();
                _subscriptions.Add(eventName, subscription);
            }

            return subscription;
        }

        public Task Invoke(string method, params object[] args)
        {
            return Invoke<object>(method, args);
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exceptions are flown to the caller")]
        public Task<T> Invoke<T>(string method, params object[] args)
        {
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }

            if (args == null)
            {
                throw new ArgumentNullException("args");
            }

            var tokenifiedArguments = new JToken[args.Length];
            for (int i = 0; i < tokenifiedArguments.Length; i++)
            {
                tokenifiedArguments[i] = JToken.FromObject(args[i], JsonSerializer);
            }

            var tcs = new TaskCompletionSource<T>();
            var callbackId = _connection.RegisterCallback(result =>
            {
                if (result != null)
                {
                    if (result.Error != null)
                    {
                        if (result.IsHubException.HasValue && result.IsHubException.Value)
                        {
                            // A HubException was thrown
                            tcs.TrySetException(new HubException(result.Error, result.ErrorData));
                        }
                        else
                        {
                            tcs.TrySetException(new InvalidOperationException(result.Error));
                        }
                    }
                    else
                    {
                        try
                        {
                            if (result.State != null)
                            {
                                foreach (var pair in result.State)
                                {
                                    this[pair.Key] = pair.Value;
                                }
                            }

                            if (result.Result != null)
                            {
                                tcs.TrySetResult(result.Result.ToObject<T>(JsonSerializer));
                            }
                            else
                            {
                                tcs.TrySetResult(default(T));
                            }
                        }
                        catch (Exception ex)
                        {
                            // If we failed to set the result for some reason or to update
                            // state then just fail the tcs.
                            tcs.TrySetUnwrappedException(ex);
                        }
                    }
                }
                else
                {
                    tcs.TrySetCanceled();
                }
            });

            var hubData = new HubInvocation
            {
                Hub = _hubName,
                Method = method,
                Args = tokenifiedArguments,
                CallbackId = callbackId
            };

            if (_state.Count != 0)
            {
                hubData.State = _state;
            }

            var value = _connection.JsonSerializeObject(hubData);

            _connection.Send(value).ContinueWith(task =>
            {
                if (task.IsCanceled)
                {
                    _connection.RemoveCallback(callbackId);
                    tcs.TrySetCanceled();
                }
                else if (task.IsFaulted)
                {
                    _connection.RemoveCallback(callbackId);
                    tcs.TrySetUnwrappedException(task.Exception);
                }
            },
            TaskContinuationOptions.NotOnRanToCompletion);

            return tcs.Task;
        }

        public void InvokeEvent(string eventName, IList<JToken> args)
        {
            Subscription subscription;
            if (_subscriptions.TryGetValue(eventName, out subscription))
            {
                subscription.OnReceived(args);
            }
        }
    }

    class TypedHubProxy<TServer> : IHubProxy<TServer>
    {
        private readonly IHubProxy _hubProxy;

        public TypedHubProxy(IHubProxy hubProxy)
        {
            _hubProxy = hubProxy;
        }

        public TServer Server
        {
            get { return TypedClientBuilder<TServer>.Build(_hubProxy); }
        }

        public JToken this[string name]
        {
            get
            {
                return _hubProxy[name];
            }
            set
            {
                _hubProxy[name] = value;
            }
        }

        public Task Invoke(string method, params object[] args)
        {
            return _hubProxy.Invoke(method, args);
        }

        public Task<TResult> Invoke<TResult>(string method, params object[] args)
        {
            return _hubProxy.Invoke<TResult>(method, args);
        }

        public Subscription Subscribe(string eventName)
        {
            return _hubProxy.Subscribe(eventName);
        }

        public JsonSerializer JsonSerializer
        {
            get { return _hubProxy.JsonSerializer; }
        }

        internal static class TypedClientBuilder<T>
        {
            private const string clientModule = "Microsoft.AspNet.SignalR.Tests.Server.Hubs.TypedClient";

            // There is one static instance of _builder per T
            private static Lazy<Func<IHubProxy, T>> _builder = new Lazy<Func<IHubProxy, T>>(() => GenerateClientBuilder());

            [SuppressMessage("Microsoft.Design", "CA1000:DoNotDeclareStaticMembersOnGenericTypes", Justification = "This is used internally and by tests.")]
            public static T Build(IHubProxy proxy)
            {
                return _builder.Value(proxy);
            }

            private static Func<IHubProxy, T> GenerateClientBuilder()
            {
                VerifyInterface();

                var assemblyName = new AssemblyName(clientModule);
                AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(clientModule);
                Type clientType = GenerateInterfaceImplementation(moduleBuilder);

                return proxy => (T)Activator.CreateInstance(clientType, proxy);
            }

            private static Type GenerateInterfaceImplementation(ModuleBuilder moduleBuilder)
            {
                TypeBuilder type = moduleBuilder.DefineType(typeof(T).Name + "Impl", TypeAttributes.Public,
                    typeof(Object), new Type[] { typeof(T) });

                FieldBuilder proxyField = type.DefineField("_proxy", typeof(IHubProxy), FieldAttributes.Private);

                BuildConstructor(type, proxyField);

                foreach (var method in typeof(T).GetMethods())
                {
                    BuildMethod(type, method, proxyField);
                }

                return type.CreateType();
            }

            private static void BuildConstructor(TypeBuilder type, FieldInfo proxyField)
            {
                MethodBuilder method = type.DefineMethod(".ctor", System.Reflection.MethodAttributes.Public | System.Reflection.MethodAttributes.HideBySig);

                ConstructorInfo ctor = typeof(object).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new Type[] { }, null);

                method.SetReturnType(typeof(void));
                method.SetParameters(typeof(IHubProxy));

                ILGenerator generator = method.GetILGenerator();

                // Call object constructor
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Call, ctor);

                // Assign constructor argument to the proxyField
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldarg_1);
                generator.Emit(OpCodes.Stfld, proxyField);
                generator.Emit(OpCodes.Ret);
            }

            private static void BuildMethod(TypeBuilder type, MethodInfo interfaceMethodInfo, FieldInfo proxyField)
            {
                MethodAttributes methodAttributes =
                      MethodAttributes.Public
                    | MethodAttributes.Virtual
                    | MethodAttributes.Final
                    | MethodAttributes.HideBySig
                    | MethodAttributes.NewSlot;

                ParameterInfo[] parameters = interfaceMethodInfo.GetParameters();
                Type[] paramTypes = parameters.Select(param => param.ParameterType).ToArray();

                MethodBuilder methodBuilder = type.DefineMethod(interfaceMethodInfo.Name, methodAttributes, typeof(void), paramTypes);

                MethodInfo invokeMethod = typeof(IHubProxy).GetMethods().Where(m => m.Name == "Invoke").FirstOrDefault();

                methodBuilder.SetReturnType(interfaceMethodInfo.ReturnType);
                methodBuilder.SetParameters(paramTypes);

                ILGenerator generator = methodBuilder.GetILGenerator();

                // Declare local variable to store the arguments to IClientProxy.Invoke
                generator.DeclareLocal(typeof(object[]));

                // Get IClientProxy
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, proxyField);

                // The first argument to IClientProxy.Invoke is this method's name
                generator.Emit(OpCodes.Ldstr, interfaceMethodInfo.Name);

                // Create an new object array to hold all the parameters to this method
                generator.Emit(OpCodes.Ldc_I4, parameters.Length);
                generator.Emit(OpCodes.Newarr, typeof(object));
                generator.Emit(OpCodes.Stloc_0);

                // Store each parameter in the object array
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    generator.Emit(OpCodes.Ldloc_0);
                    generator.Emit(OpCodes.Ldc_I4, i);
                    generator.Emit(OpCodes.Ldarg, i + 1);
                    generator.Emit(OpCodes.Box, paramTypes[i]);
                    generator.Emit(OpCodes.Stelem_Ref);
                }

                // Call IProxy.Invoke
                generator.Emit(OpCodes.Ldloc_0);
                generator.Emit(OpCodes.Callvirt, invokeMethod);

                if (interfaceMethodInfo.ReturnType == typeof(void))
                {
                    // void return
                    generator.Emit(OpCodes.Pop);
                }

                generator.Emit(OpCodes.Ret);
            }

            private static void VerifyInterface()
            {
                //var interfaceType = typeof(T);

                //if (!interfaceType.IsInterface)
                //{
                //    throw new InvalidOperationException(
                //        String.Format(CultureInfo.CurrentCulture, Resources.Error_TypeMustBeInterface, interfaceType.Name));
                //}

                //if (interfaceType.GetProperties().Length != 0)
                //{
                //    throw new InvalidOperationException(
                //        String.Format(CultureInfo.CurrentCulture, Resources.Error_TypeMustNotContainProperties, interfaceType.Name));
                //}

                //if (interfaceType.GetEvents().Length != 0)
                //{
                //    throw new InvalidOperationException(
                //        String.Format(CultureInfo.CurrentCulture, Resources.Error_TypeMustNotContainEvents, interfaceType.Name));
                //}

                //foreach (var method in interfaceType.GetMethods())
                //{
                //    VerifyMethod(interfaceType, method);
                //}
            }

            private static void VerifyMethod(Type interfaceType, MethodInfo interfaceMethod)
            {
                //if (interfaceMethod.ReturnType != typeof(void) && interfaceMethod.ReturnType != typeof(Task))
                //{
                //    throw new InvalidOperationException(
                //        String.Format(CultureInfo.CurrentCulture, Resources.Error_MethodMustReturnVoidOrTask,
                //            interfaceType.Name,
                //            interfaceMethod.Name));
                //}

                //foreach (var parameter in interfaceMethod.GetParameters())
                //{
                //    VerifyParameter(interfaceType, interfaceMethod, parameter);
                //}
            }

            private static void VerifyParameter(Type interfaceType, MethodInfo interfaceMethod, ParameterInfo parameter)
            {
                //if (parameter.IsOut)
                //{
                //    throw new InvalidOperationException(
                //        String.Format(CultureInfo.CurrentCulture, Resources.Error_MethodMustNotTakeOutParameter,
                //            parameter.Name,
                //            interfaceType.Name,
                //            interfaceMethod.Name));
                //}

                //if (parameter.ParameterType.IsByRef)
                //{
                //    throw new InvalidOperationException(
                //        String.Format(CultureInfo.CurrentCulture, Resources.Error_MethodMustNotTakeRefParameter,
                //            parameter.Name,
                //            interfaceType.Name,
                //            interfaceMethod.Name));
                //}
            }
        }
    }
}
