using Autofac;
using SignalR.Infrastructure;
using SignalR.Autofac;

[assembly: WebActivator.PreApplicationStartMethod(typeof($rootnamespace$.App_Start.AutofacSignalR), "Start")]

namespace $rootnamespace$.App_Start {
    public static class AutofacSignalR {
        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() {
            IKernel kernel = CreateKernel();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(kernel));
        }

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel() {
            var kernel = new StandardKernel();
            RegisterServices(kernel);
            return kernel;
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel) {
        }
    }
}