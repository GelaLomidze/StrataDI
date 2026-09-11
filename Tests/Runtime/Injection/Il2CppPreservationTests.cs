using NUnit.Framework;
using UnityEngine.Scripting;

namespace StrataDI.Tests
{
    public sealed class Il2CppPreservationTests
    {
        [Test]
        public void InjectAttribute_DerivesFromPreserveAttribute()
        {
            Assert.IsTrue(
                typeof(PreserveAttribute)
                    .IsAssignableFrom(typeof(InjectAttribute)));
        }
    }
}