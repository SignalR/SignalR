using System;
using System.Collections.Generic;
using Ninject;
using SignalR.Infrastructure;

namespace SignalR.Ninject {
    public class NinjectDependencyResolver : IDependencyResolver {
        private readonly IKernel _kernel;

        public NinjectDependencyResolver(IKernel kernel) {
            if (kernel == null) {
                throw new ArgumentNullException("kernel");
            }
            _kernel = kernel;
        }

        public object GetService(Type serviceType) {
            return _kernel.TryGet(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType) {
            return _kernel.GetAll(serviceType);
        }

        public void Register(Type serviceType, IEnumerable<Func<object>> activators) {
            // REVIEW: Does ninject support this?
        }

        public void Register(Type serviceType, Func<object> activator) {
            _kernel.Bind(serviceType).ToMethod(_ => activator());
        }
    }
}
