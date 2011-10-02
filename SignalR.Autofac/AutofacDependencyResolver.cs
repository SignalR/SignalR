using System;
using System.Collections.Generic;
using Autofac;
using Autofac.Core;
using SignalR.Infrastructure;

namespace SignalR.Autofac {
    public class AutofacDependencyResolver : IDependencyResolver {
        private readonly IContainer _container;

        public AutofacDependencyResolver(IContainer container) {
            if (container == null) {
                throw new ArgumentNullException("container");
            }
            _container = container;
        }

        public object GetService(Type serviceType) {
            object instance = null;
            _container.TryResolve(serviceType, out instance);
            return instance;
        }

        public IEnumerable<object> GetServices(Type serviceType) {
            Type genericType = typeof(IEnumerable<>);
            Type specialisedType = genericType.MakeGenericType(serviceType);
            return _container.Resolve(specialisedType) as IEnumerable<object>;
        }

        public void Register(Type serviceType, IEnumerable<Func<object>> activators) {
            var builder = new ContainerBuilder();

            foreach (var a in activators) {
                builder.Register(c => a())
                   .As(new TypedService(serviceType));
            }
            
            builder.Update(_container);
        }

        public void Register(Type serviceType, Func<object> activator) {
            var builder = new ContainerBuilder();

            builder.Register(c => activator())
                   .As(new TypedService(serviceType));
            
            builder.Update(_container);
        }
    }
}
