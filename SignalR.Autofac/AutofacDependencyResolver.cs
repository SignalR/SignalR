using System;
using System.Collections.Generic;
using Autofac;
using SignalR.Infrastructure;

namespace SignalR.Autofac {
    public class AutofacDependencyResolver : IDependencyResolver {
        private readonly IContainer _container;

        public AutofacDependencyResolver(IContainer container) {
            if (container == null)
                throw new ArgumentNullException("container");

            _container = container;
        }

        public object GetService(Type serviceType) {
            object instance = null;
            _container.TryResolve(serviceType, out instance);
            return instance;
        }

        public IEnumerable<object> GetServices(Type serviceType) {
            // TODO: i forget the trick to create the type i want
            return null;
        }

        public void Register(Type serviceType, IEnumerable<Func<object>> activators) {
            var builder = new ContainerBuilder();
            // TODO: get the rules right
            builder.Update(_container);
        }

        public void Register(Type serviceType, Func<object> activator) {
            var builder = new ContainerBuilder();
            // TODO: get the rules right
            builder.Update(_container);
        }
    }
}
