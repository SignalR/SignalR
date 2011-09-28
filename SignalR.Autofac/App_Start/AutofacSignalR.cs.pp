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
            IContainer container = CreateContainer();
            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }

        /// <summary>
        /// Creates the container that will manage your application.
        /// </summary>
        /// <returns>The created container.</returns>
        private static IContainer CreateContainer() {
            var builder = new ContainerBuilder();
            RegisterServices(builder);
            return builder.Build();
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The container builder.</param>
        private static void RegisterServices(ContainerBuilder builder) {
        }
    }
}