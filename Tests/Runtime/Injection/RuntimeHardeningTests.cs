using System;
using NUnit.Framework;
using UnityEngine;

using Object = UnityEngine.Object;

namespace StrataDI.Tests
{
    public sealed class RuntimeHardeningTests
    {
        private GameObject _targetObject;
        private GameObject _reentrantFirstObject;
        private GameObject _reentrantSecondObject;

        [TearDown]
        public void TearDown()
        {
            if (_reentrantSecondObject != null)
            {
                Object.DestroyImmediate(_reentrantSecondObject);
            }

            if (_reentrantFirstObject != null)
            {
                Object.DestroyImmediate(_reentrantFirstObject);
            }

            if (_targetObject != null)
            {
                Object.DestroyImmediate(_targetObject);
            }
        }

        [Test]
        public void MethodInjection_OverriddenInheritedInjectMethod_IsInvokedOnce()
        {
            DependencyContainer container = new();

            RuntimeHardeningTestService service = new();

            container.Bind<IRuntimeHardeningTestService>(
                service);

            DependencyInjector injector =
                new DependencyInjector(container);

            _targetObject =
                new GameObject("InheritedInjectionTarget");

            DerivedInheritedInjectionTarget target =
                _targetObject.AddComponent<
                    DerivedInheritedInjectionTarget>();

            injector.TryInjectAndNotify(target);

            Assert.AreSame(
                service,
                target.Service);

            Assert.AreEqual(
                1,
                target.InjectionCount,
                "An inherited [Inject] virtual method must be invoked only once.");
        }

        [Test]
        public void RetryPendingObjects_WhenCallbackTriggersNestedRetry_DoesNotCorruptIteration()
        {
            DependencyContainer container = new();

            DependencyInjector injector =
                new DependencyInjector(container);

            _reentrantFirstObject =
                new GameObject("FirstPendingTarget");

            ReentrantPendingTarget firstTarget =
                _reentrantFirstObject.AddComponent<
                    ReentrantPendingTarget>();

            _reentrantSecondObject =
                new GameObject("CallbackPendingTarget");

            ReentrantCallbackTarget callbackTarget =
                _reentrantSecondObject.AddComponent<
                    ReentrantCallbackTarget>();

            callbackTarget.OnInjectedAction =
                injector.RetryPendingObjects;

            // Both targets become pending because the
            // dependency is not registered yet.
            injector.TryInjectAndNotify(firstTarget);
            injector.TryInjectAndNotify(callbackTarget);

            Assert.IsNull(firstTarget.Service);
            Assert.IsNull(callbackTarget.Service);

            RuntimeHardeningTestService service = new();

            container.Bind<IRuntimeHardeningTestService>(
                service);

            Assert.DoesNotThrow(
                injector.RetryPendingObjects);

            Assert.AreSame(
                service,
                firstTarget.Service);

            Assert.AreSame(
                service,
                callbackTarget.Service);

            Assert.AreEqual(
                1,
                callbackTarget.CallbackCount);
        }
    }

    internal interface IRuntimeHardeningTestService
    {
    }

    internal sealed class RuntimeHardeningTestService
        : IRuntimeHardeningTestService
    {
    }

    internal class BaseInheritedInjectionTarget
        : MonoBehaviour
    {
        public IRuntimeHardeningTestService Service
        {
            get;
            private set;
        }

        public int InjectionCount
        {
            get;
            private set;
        }

        [Inject]
        protected virtual void Initialize(
            IRuntimeHardeningTestService service)
        {
            Service = service;
            InjectionCount++;
        }
    }

    internal sealed class DerivedInheritedInjectionTarget
        : BaseInheritedInjectionTarget
    {
        protected override void Initialize(
            IRuntimeHardeningTestService service)
        {
            base.Initialize(service);
        }
    }

    internal sealed class ReentrantPendingTarget
        : MonoBehaviour
    {
        [Inject]
        public IRuntimeHardeningTestService Service
        {
            get;
            private set;
        }
    }

    internal sealed class ReentrantCallbackTarget
        : MonoBehaviour,
          IInjectionCallback
    {
        [Inject]
        public IRuntimeHardeningTestService Service
        {
            get;
            private set;
        }

        public Action OnInjectedAction
        {
            get;
            set;
        }

        public int CallbackCount
        {
            get;
            private set;
        }

        public void OnInjected()
        {
            CallbackCount++;

            OnInjectedAction?.Invoke();
        }
    }
}