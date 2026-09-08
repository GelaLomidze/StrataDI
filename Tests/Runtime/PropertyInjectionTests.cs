using NUnit.Framework;
using UnityEngine;

namespace StrataDI.Tests
{
    public sealed class PropertyInjectionTests
    {
        private GameObject _contextObject;
        private GameObject _targetObject;

        [TearDown]
        public void TearDown()
        {
            if (_targetObject != null)
            {
                Object.DestroyImmediate(_targetObject);
            }

            if (_contextObject != null)
            {
                Object.DestroyImmediate(_contextObject);
            }
        }

        [Test]
        public void InjectsPropertyWithPrivateSetter()
        {
            _contextObject = new GameObject("DependencyContext");

            DependencyContext context =
                _contextObject.AddComponent<DependencyContext>();

            PropertyInjectionTestService service =
                new PropertyInjectionTestService();

            context.Container.Bind<IPropertyInjectionTestService>(service);

            _targetObject = new GameObject("PropertyInjectionTarget");

            PropertyInjectionTarget target =
                _targetObject.AddComponent<PropertyInjectionTarget>();

            context.BindAndInjectComponent(target);

            Assert.AreSame(service, target.Service);
        }
    }

    internal interface IPropertyInjectionTestService
    {
    }

    internal sealed class PropertyInjectionTestService
        : IPropertyInjectionTestService
    {
    }

    internal sealed class PropertyInjectionTarget : MonoBehaviour
    {
        [Inject]
        public IPropertyInjectionTestService Service { get; private set; }
    }
}