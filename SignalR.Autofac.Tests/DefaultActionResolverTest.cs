using System.Collections.Generic;
using Xunit;
using Autofac;
using SignalR.Autofac;

namespace SignalR.Tests
{
    public class DefaultActionResolverTest
    {
        [Fact]
        public void GetService_WithSingleRegistration_ReturnsNotNull()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleType>().AsImplementedInterfaces();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            var entity = resolver.GetService(typeof(ISomeType));

            Assert.NotNull(entity);
        }

        [Fact]
        public void GetService_WithNoRegistration_ReturnsNull()
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            var entity = resolver.GetService(typeof(ISomeType));

            Assert.Null(entity);
        }
    }

    public interface ISomeType { }

    public class SimpleType : ISomeType { }
}
