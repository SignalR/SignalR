#if !PACKAGE_BUILD
using Autofac;
using SignalR.Infrastructure;

[assembly: WebActivator.PreApplicationStartMethod(typeof(SignalR.Autofac.App_Start.AutofacSignalR), "Start")]

namespace SignalR.Autofac.App_Start {
    public static class AutofacSignalR {
        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() {
            IContainer kernel = CreateKernel();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(kernel));
        }

        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IContainer CreateKernel()
        {
            var builder = new ContainerBuilder();
            RegisterServices(builder);
            return builder.Build();
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(ContainerBuilder builder) {
        }
    }
}
#endif