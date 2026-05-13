using AbilityKit.Core.Math;
using AbilityKit.Demo.Moba.Console.Sync;

namespace AbilityKit.Demo.Moba.Console.AutoTest
{
    /// <summary>
    /// SampleBuffer 插值测试
    /// </summary>
    public static class SampleBufferTests
    {
        public static TestResult Run()
        {
            var result = new TestResult { Name = "SampleBuffer" };

            try
            {
                result = TestEmptyBuffer();
                if (!result.Passed) return result;

                result = TestSingleSample();
                if (!result.Passed) return result;

                result = TestLinearInterpolation();
                if (!result.Passed) return result;

                result = TestExtrapolation();
                if (!result.Passed) return result;

                result = TestDeduplication();
                if (!result.Passed) return result;

                result = TestClear();
                if (!result.Passed) return result;

                result.Pass();
            }
            catch (Exception ex)
            {
                result.Fail($"Exception: {ex.Message}");
            }

            return result;
        }

        private static TestResult TestEmptyBuffer()
        {
            var result = new TestResult { Name = "SampleBuffer.EmptyBuffer" };
            var buffer = new SampleBuffer();

            if (buffer.TryEvaluate(0, out var pos))
            {
                result.Fail("Empty buffer should return false");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestSingleSample()
        {
            var result = new TestResult { Name = "SampleBuffer.SingleSample" };
            var buffer = new SampleBuffer();
            var pos = new Vec3(1, 2, 3);

            buffer.Add(1.0, in pos);

            if (!buffer.TryEvaluate(0.5, out var evalPos))
            {
                result.Fail("Single sample should return true");
                return result;
            }

            if (evalPos.X != 1 || evalPos.Y != 2 || evalPos.Z != 3)
            {
                result.Fail($"Expected (1,2,3), got ({evalPos.X},{evalPos.Y},{evalPos.Z})");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestLinearInterpolation()
        {
            var result = new TestResult { Name = "SampleBuffer.LinearInterpolation" };
            var buffer = new SampleBuffer();

            buffer.Add(0.0, new Vec3(0, 0, 0));
            buffer.Add(1.0, new Vec3(10, 0, 0));

            if (!buffer.TryEvaluate(0.5, out var pos))
            {
                result.Fail("Should return true for interpolation");
                return result;
            }

            if (pos.X < 4.9 || pos.X > 5.1)
            {
                result.Fail($"Expected ~5.0 at t=0.5, got {pos.X}");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestExtrapolation()
        {
            var result = new TestResult { Name = "SampleBuffer.Extrapolation" };
            var buffer = new SampleBuffer();

            buffer.Add(0.0, new Vec3(0, 0, 0));
            buffer.Add(1.0, new Vec3(10, 0, 0));

            if (!buffer.TryEvaluate(2.0, out var pos))
            {
                result.Fail("Should return true for extrapolation");
                return result;
            }

            if (pos.X < 19.9 || pos.X > 20.1)
            {
                result.Fail($"Expected ~20.0 at t=2.0, got {pos.X}");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestDeduplication()
        {
            var result = new TestResult { Name = "SampleBuffer.Deduplication" };
            var buffer = new SampleBuffer();

            buffer.Add(1.0, new Vec3(1, 1, 1));
            buffer.Add(1.0, new Vec3(2, 2, 2));

            if (buffer.Count != 1)
            {
                result.Fail($"Expected 1 sample after deduplication, got {buffer.Count}");
                return result;
            }

            if (!buffer.TryEvaluate(1.0, out var pos))
            {
                result.Fail("Should return true");
                return result;
            }

            if (pos.X < 1.9 || pos.X > 2.1)
            {
                result.Fail($"Expected replaced value (2,2,2), got ({pos.X},{pos.Y},{pos.Z})");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestClear()
        {
            var result = new TestResult { Name = "SampleBuffer.Clear" };
            var buffer = new SampleBuffer();

            buffer.Add(1.0, new Vec3(1, 1, 1));
            buffer.Clear();

            if (buffer.Count != 0)
            {
                result.Fail($"Expected 0 after clear, got {buffer.Count}");
                return result;
            }

            result.Pass();
            return result;
        }
    }
}
