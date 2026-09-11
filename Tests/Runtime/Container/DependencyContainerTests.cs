using NUnit.Framework;

namespace StrataDI.Tests
{
    public sealed class DependencyContainerTests
    {
        [Test]
        public void Resolve_ReturnsLocalBinding()
        {
            DependencyContainer container = new();
            Service service = new();

            container.Bind(service);

            Assert.AreSame(service, container.Resolve<Service>());
        }

        [Test]
        public void Resolve_FallsBackToParent()
        {
            DependencyContainer parent = new();
            DependencyContainer child = new(parent);
            Service service = new();

            parent.Bind(service);

            Assert.AreSame(service, child.Resolve<Service>());
        }

        [Test]
        public void Resolve_PrefersLocalBindingOverParent()
        {
            DependencyContainer parent = new();
            DependencyContainer child = new(parent);
            Service parentService = new();
            Service childService = new();

            parent.Bind(parentService);
            child.Bind(childService);

            Assert.AreSame(childService, child.Resolve<Service>());
        }

        [Test]
        public void Bind_CanRegisterImplementationAsInterface()
        {
            DependencyContainer container = new();
            IService service = new Service();

            container.Bind<IService>(service);

            Assert.AreSame(service, container.Resolve<IService>());
        }

        private interface IService
        {
        }

        private sealed class Service : IService
        {
        }
    }
}
