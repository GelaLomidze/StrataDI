using NUnit.Framework;
using UnityEngine;

namespace StrataDI.Tests
{
    public sealed class PerformanceOptimizationTests
    {
        private GameObject _targetObject;
        private GameObject _contextObject;
        private GameObject _hierarchyObject;

        [TearDown]
        public void TearDown()
        {
            if (_hierarchyObject != null)
            {
                Object.DestroyImmediate(_hierarchyObject);
            }

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
        public void ObjectArrayPool_ReusesReturnedArrayOfSameLength()
        {
            ObjectArrayPool pool = new();

            object[] first = pool.Rent(2);

            pool.Return(first);

            object[] second = pool.Rent(2);

            Assert.AreSame(first, second);
        }

        [Test]
        public void ObjectArrayPool_ClearsReferencesBeforeReuse()
        {
            ObjectArrayPool pool = new();

            object dependencyA = new();
            object dependencyB = new();

            object[] first = pool.Rent(2);
            first[0] = dependencyA;
            first[1] = dependencyB;

            pool.Return(first);

            object[] second = pool.Rent(2);

            Assert.IsNull(second[0]);
            Assert.IsNull(second[1]);
        }

        [Test]
        public void ObjectArrayPool_UsesSeparatePoolsForDifferentLengths()
        {
            ObjectArrayPool pool = new();

            object[] lengthOne = pool.Rent(1);
            object[] lengthTwo = pool.Rent(2);

            pool.Return(lengthOne);
            pool.Return(lengthTwo);

            object[] reusedOne = pool.Rent(1);
            object[] reusedTwo = pool.Rent(2);

            Assert.AreSame(lengthOne, reusedOne);
            Assert.AreSame(lengthTwo, reusedTwo);
            Assert.AreEqual(1, reusedOne.Length);
            Assert.AreEqual(2, reusedTwo.Length);
        }

        [Test]
        public void ObjectArrayPool_ZeroLengthUsesEmptyArray()
        {
            ObjectArrayPool pool = new();

            object[] first = pool.Rent(0);
            object[] second = pool.Rent(0);

            Assert.AreSame(first, second);
            Assert.AreEqual(0, first.Length);
        }

        [Test]
        public void RetryPendingObjects_InjectsAfterDependencyBecomesAvailable()
        {
            DependencyContainer container = new();
            DependencyInjector injector = new(container);

            _targetObject =
                new GameObject("PendingInjectionTarget");

            PendingInjectionTarget target =
                _targetObject.AddComponent<PendingInjectionTarget>();

            injector.TryInjectAndNotify(target);

            Assert.IsNull(target.Service);

            PerformanceTestService service = new();

            container.Bind<IPerformanceTestService>(service);

            injector.RetryPendingObjects();

            Assert.AreSame(service, target.Service);
        }

        [Test]
        public void InjectionCallback_IsInvokedOnlyOnce()
        {
            DependencyContainer container = new();
            DependencyInjector injector = new(container);

            PerformanceTestService service = new();

            container.Bind<IPerformanceTestService>(service);

            _targetObject =
                new GameObject("CallbackTarget");

            CallbackInjectionTarget target =
                _targetObject.AddComponent<CallbackInjectionTarget>();

            injector.TryInjectAndNotify(target);
            injector.TryInjectAndNotify(target);

            Assert.AreSame(service, target.Service);
            Assert.AreEqual(1, target.CallbackCount);
        }

        [Test]
        public void MethodInjection_WorksWithPooledArgumentArray()
        {
            DependencyContainer container = new();
            DependencyInjector injector = new(container);

            PerformanceTestService service = new();

            container.Bind<IPerformanceTestService>(service);

            _targetObject =
                new GameObject("MethodInjectionTarget");

            MethodInjectionTarget target =
                _targetObject.AddComponent<MethodInjectionTarget>();

            injector.TryInjectAndNotify(target);

            Assert.AreSame(service, target.Service);
            Assert.AreEqual(1, target.InjectionCount);
        }

        [Test]
        public void BindAndInjectGameObject_BindsAndInjectsHierarchy()
        {
            _contextObject =
                new GameObject("DependencyContext");

            DependencyContext context =
                _contextObject.AddComponent<DependencyContext>();

            _hierarchyObject =
                new GameObject("HierarchyRoot");

            HierarchyInjectionTarget target =
                _hierarchyObject.AddComponent<HierarchyInjectionTarget>();

            GameObject child =
                new GameObject("HierarchyService");

            child.transform.SetParent(
                _hierarchyObject.transform);

            HierarchyTestService service =
                child.AddComponent<HierarchyTestService>();

            context.BindAndInjectGameObject(
                _hierarchyObject);

            Assert.AreSame(service, target.Service);
        }
    }

    internal interface IPerformanceTestService
    {
    }

    internal sealed class PerformanceTestService
        : IPerformanceTestService
    {
    }

    internal sealed class PendingInjectionTarget : MonoBehaviour
    {
        [Inject]
        public IPerformanceTestService Service { get; private set; }
    }

    internal sealed class CallbackInjectionTarget :
        MonoBehaviour,
        IInjectionCallback
    {
        [Inject]
        public IPerformanceTestService Service { get; private set; }

        public int CallbackCount { get; private set; }

        public void OnInjected()
        {
            CallbackCount++;
        }
    }

    internal sealed class MethodInjectionTarget : MonoBehaviour
    {
        public IPerformanceTestService Service { get; private set; }
        public int InjectionCount { get; private set; }

        [Inject]
        private void Initialize(
            IPerformanceTestService service)
        {
            Service = service;
            InjectionCount++;
        }
    }

    internal sealed class HierarchyTestService : MonoBehaviour
    {
    }

    internal sealed class HierarchyInjectionTarget : MonoBehaviour
    {
        [Inject]
        public HierarchyTestService Service { get; private set; }
    }
}