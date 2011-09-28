using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using SignalR.Autofac;
using Xunit;

namespace SignalR.Tests
{
    public class DefaultActionResolverTest
    {
        [Fact]
        public void GetService_WithNoRegistrations_ReturnsDefaultValue() {
            // arrange
            var builder = new ContainerBuilder();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);

            // act
            var entity = resolver.GetService(typeof(ISomeType));

            Assert.Equal(default(ISomeType), entity);
        }

        [Fact]
        public void GetService_WithSingleRegistration_ReturnsNotNull() {
            // arrange
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleType>().AsImplementedInterfaces();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);

            // act
            var entity = resolver.GetService(typeof(ISomeType));

            Assert.NotNull(entity);
        }

        [Fact]
        public void GetServices_WithUnrelatedFuncs_ReturnsEmptyList() {
            // arrange
            var builder = new ContainerBuilder();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);

            var registrations = new List<Func<object>>();
            registrations.Add(() => new SimpleType());
            registrations.Add(() => new AnotherType());
            resolver.Register(typeof(ISomeType), registrations);

            // act
            var result = resolver.GetServices(typeof(INotUsed));

            Assert.False(result.Any());
        }

        [Fact]
        public void GetService_WithNoRegistration_ReturnsNull() {
            // arrange
            var builder = new ContainerBuilder();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);

            // act
            var entity = resolver.GetService(typeof(ISomeType));

            Assert.Null(entity);
        }

        [Fact]
        public void GetServices_WithTwoRegistration_ReturnsTwoEntities() {
            // arrange
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleType>().AsImplementedInterfaces();
            builder.RegisterType<AnotherType>().AsImplementedInterfaces();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);

            // act
            var result = resolver.GetServices(typeof(ISomeType));

            Assert.True(result.Count() == 2);
        }

        [Fact]
        public void GetServices_WithOneRegistration_ReturnsOneEntities() {
            // arrange
            var builder = new ContainerBuilder();
            builder.RegisterType<SimpleType>().AsImplementedInterfaces();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);

            // act
            var result = resolver.GetServices(typeof(ISomeType));

            Assert.True(result.Count() == 1);
        }

        [Fact]
        public void GetServices_WithNoRegistrations_ReturnsEmptyList() {
            // arrange
            var builder = new ContainerBuilder();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);
            
            // act 
            var result = resolver.GetServices(typeof(ISomeType));

            Assert.False(result.Any());
        }

        [Fact]
        public void Register_WithOneFunc_ReturnsNotNullResult() {
            // arrange
            var builder = new ContainerBuilder();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);

            // act
            resolver.Register(typeof(ISomeType), () => new SimpleType());
            var result = resolver.GetService(typeof(ISomeType));

            Assert.NotNull(result);
        }

        [Fact]
        public void Register_WithOneFunc_ReturnsDistinctItemsEachTime() {
            // arrange
            var builder = new ContainerBuilder();
            var container = builder.Build();
            var resolver = new AutofacDependencyResolver(container);
            
            // act
            resolver.Register(typeof(ISomeType), () => new SimpleType());
            var firstSet = resolver.GetService(typeof(ISomeType));
            var secondSet = resolver.GetService(typeof(ISomeType));

            Assert.NotSame(firstSet, secondSet);
        }
    }

    public interface INotUsed { }

    public interface ISomeType { }

    public class SimpleType : ISomeType { }

    public class AnotherType : ISomeType { }
}
