using System.Linq;
using System.Collections.Generic;
using Xunit;
using Autofac;
using SignalR.Autofac;
using System;

namespace SignalR.Tests
{
    public class DefaultActionResolverTest
    {
        [Fact]
        public void GetService_WithNoRegistrations_ReturnsDefaultValue()
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            var entity = resolver.GetService(typeof(ISomeType));

            Assert.Equal(default(ISomeType), entity);
        }

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

        [Fact]
        public void GetServices_WithTwoRegistration_ReturnsTwoEntities()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleType>().AsImplementedInterfaces();
            builder.RegisterType<AnotherType>().AsImplementedInterfaces();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            var result = resolver.GetServices(typeof(ISomeType));

            Assert.True(result.Count() == 2);
        }

        [Fact]
        public void GetServices_WithOneRegistration_ReturnsOneEntities()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleType>().AsImplementedInterfaces();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            var result = resolver.GetServices(typeof(ISomeType));

            Assert.True(result.Count() == 1);
        }

        [Fact]
        public void GetServices_WithNoRegistration_ReturnsEmptyList()
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            var result = resolver.GetServices(typeof(ISomeType));

            Assert.False(result.Any());
        }


        [Fact]
        public void Register_WithOneRegistration_ReturnsNotNullResult()
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            resolver.Register(typeof(ISomeType), () => new SimpleType());
            var result = resolver.GetService(typeof(ISomeType));

            Assert.NotNull(result);
        }

        [Fact]
        public void Register_WithOneRegistration_ReturnsDistinctItemsEachTime()
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();

            var resolver = new AutofacDependencyResolver(container);
            resolver.Register(typeof(ISomeType), () => new SimpleType());
            var firstSet = resolver.GetService(typeof(ISomeType));
            var secondSet = resolver.GetService(typeof(ISomeType));

            Assert.NotSame(firstSet, secondSet);
        }

        [Fact]
        public void Register_WithTwoRegistrations_ReturnsTwoEntities()
        {
            var builder = new ContainerBuilder();
            var container = builder.Build();

            var registrations = new List<Func<object>>();
            registrations.Add(() => new SimpleType());
            registrations.Add(() => new AnotherType());

            var resolver = new AutofacDependencyResolver(container);
            resolver.Register(typeof(ISomeType), registrations);
            var result = resolver.GetServices(typeof(ISomeType));

            Assert.True(result.Count() == 2);
        }

    }

    public interface ISomeType { }

    public class SimpleType : ISomeType { }

    public class AnotherType : ISomeType { }
}
