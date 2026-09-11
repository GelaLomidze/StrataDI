using System;
using NUnit.Framework;

namespace StrataDI.Tests
{
    public sealed class ConstructorInjectionTests
    {
        [Test]
        public void Create_InjectsConstructorDependencies()
        {
            DependencyContainer container = new();

            ConstructorTestService service = new();

            container.Bind<IConstructorTestService>(
                service);

            ConstructorInjectionTarget target =
                container.Create<ConstructorInjectionTarget>();

            Assert.AreSame(
                service,
                target.Service);
        }

        [Test]
        public void Create_UsesInjectConstructorWhenMultipleConstructorsExist()
        {
            DependencyContainer container = new();

            ConstructorTestService service = new();

            container.Bind<IConstructorTestService>(
                service);

            MultipleConstructorTarget target =
                container.Create<MultipleConstructorTarget>();

            Assert.AreSame(
                service,
                target.Service);
        }

        [Test]
        public void Create_UsesPrivateInjectConstructor()
        {
            DependencyContainer container = new();

            ConstructorTestService service = new();

            container.Bind<IConstructorTestService>(
                service);

            PrivateConstructorTarget target =
                container.Create<PrivateConstructorTarget>();

            Assert.AreSame(
                service,
                target.Service);
        }

        [Test]
        public void Create_ThrowsWhenConstructorHasNoInjectAttribute()
        {
            DependencyContainer container = new();

            ConstructorTestService service = new();

            container.Bind<IConstructorTestService>(
                service);

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () =>
                        container.Create<
                            SingleConstructorTarget>());

            StringAssert.Contains(
                nameof(InjectAttribute),
                exception.Message);
        }

        [Test]
        public void Create_ThrowsWhenConstructorDependencyIsMissing()
        {
            DependencyContainer container = new();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () =>
                        container.Create<
                            ConstructorInjectionTarget>());

            StringAssert.Contains(
                nameof(IConstructorTestService),
                exception.Message);
        }

        [Test]
        public void Create_ThrowsWhenNoConstructorHasInjectAttribute()
        {
            DependencyContainer container = new();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () =>
                        container.Create<
                            MultipleUnannotatedConstructorsTarget>());

            StringAssert.Contains(
                nameof(InjectAttribute),
                exception.Message);
        }

        [Test]
        public void Create_ThrowsWhenMultipleConstructorsHaveInjectAttribute()
        {
            DependencyContainer container = new();

            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(
                    () =>
                        container.Create<
                            MultipleInjectConstructorTarget>());

            StringAssert.Contains(
                "multiple constructors marked",
                exception.Message);
        }

        [Test]
        public void Create_ResolvesConstructorDependencyFromParentContainer()
        {
            DependencyContainer parent = new();

            ConstructorTestService service = new();

            parent.Bind<IConstructorTestService>(
                service);

            DependencyContainer child =
                new(parent);

            ConstructorInjectionTarget target =
                child.Create<ConstructorInjectionTarget>();

            Assert.AreSame(
                service,
                target.Service);
        }
    }

    internal interface IConstructorTestService
    {
    }

    internal sealed class ConstructorTestService
        : IConstructorTestService
    {
    }

    internal sealed class ConstructorInjectionTarget
    {
        public IConstructorTestService Service
        {
            get;
        }

        [Inject]
        public ConstructorInjectionTarget(
            IConstructorTestService service)
        {
            Service = service;
        }
    }

    internal sealed class MultipleConstructorTarget
    {
        public IConstructorTestService Service
        {
            get;
        }

        public MultipleConstructorTarget()
        {
        }

        [Inject]
        public MultipleConstructorTarget(
            IConstructorTestService service)
        {
            Service = service;
        }
    }

    internal sealed class PrivateConstructorTarget
    {
        public IConstructorTestService Service
        {
            get;
        }

        [Inject]
        private PrivateConstructorTarget(
            IConstructorTestService service)
        {
            Service = service;
        }
    }

    internal sealed class SingleConstructorTarget
    {
        public IConstructorTestService Service
        {
            get;
        }

        public SingleConstructorTarget(
            IConstructorTestService service)
        {
            Service = service;
        }
    }

    internal sealed class MultipleUnannotatedConstructorsTarget
    {
        public MultipleUnannotatedConstructorsTarget()
        {
        }

        public MultipleUnannotatedConstructorsTarget(
            IConstructorTestService service)
        {
        }
    }

    internal sealed class MultipleInjectConstructorTarget
    {
        [Inject]
        public MultipleInjectConstructorTarget()
        {
        }

        [Inject]
        public MultipleInjectConstructorTarget(
            IConstructorTestService service)
        {
        }
    }
}