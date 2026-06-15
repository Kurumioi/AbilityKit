using System;
using System.Collections.Generic;
using AbilityKit.Game.Flow;
using Xunit;

namespace AbilityKit.Game.View.Runtime.Tests
{
    /// <summary>
    /// Tests that <see cref="IFeatureBinder"/> consumers can be satisfied by a simple mock,
    /// validating the interface contract is sufficient for testability.
    /// </summary>
    public class IFeatureBinderContractTests
    {
        [Fact]
        public void IFeatureBinder_Mock_AttachFeature_Called()
        {
            var mock = new FakeFeatureBinder();
            mock.AttachFeature("test-feature");

            Assert.Single(mock.AttachCalls);
            Assert.Same("test-feature", mock.AttachCalls[0]);
        }

        [Fact]
        public void IFeatureBinder_Mock_DetachFeature_Called()
        {
            var mock = new FakeFeatureBinder();
            mock.DetachFeature("test-feature");

            Assert.Single(mock.DetachCalls);
            Assert.Same("test-feature", mock.DetachCalls[0]);
        }

        [Fact]
        public void IFeatureBinder_Mock_MultipleOperations()
        {
            var mock = new FakeFeatureBinder();
            var feature1 = new object();
            var feature2 = new object();

            mock.AttachFeature(feature1);
            mock.AttachFeature(feature2);
            mock.DetachFeature(feature1);

            Assert.Equal(2, mock.AttachCalls.Count);
            Assert.Single(mock.DetachCalls);
            Assert.Same(feature1, mock.AttachCalls[0]);
            Assert.Same(feature2, mock.AttachCalls[1]);
            Assert.Same(feature1, mock.DetachCalls[0]);
        }

        /// <summary>
        /// Simple recording mock for <see cref="IFeatureBinder"/>.
        /// Demonstrates that the interface is mockable without Unity/ECS dependencies.
        /// </summary>
        private sealed class FakeFeatureBinder : IFeatureBinder
        {
            public List<object> AttachCalls { get; } = new List<object>();
            public List<object> DetachCalls { get; } = new List<object>();

            public void AttachFeature(object feature) => AttachCalls.Add(feature);
            public void DetachFeature(object feature) => DetachCalls.Add(feature);
        }
    }
}
