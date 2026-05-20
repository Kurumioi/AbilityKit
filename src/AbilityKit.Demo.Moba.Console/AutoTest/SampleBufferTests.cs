using AbilityKit.Demo.Moba.Console.Battle.Sync.View;

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

            if (buffer.TryEvaluate(0, out var x, out var y, out var z))
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

            buffer.Add(1.0, 1, 2, 3);

            if (!buffer.TryEvaluate(0.5, out var x, out var y, out var z))
            {
                result.Fail("Single sample should return true");
                return result;
            }

            if (x != 1 || y != 2 || z != 3)
            {
                result.Fail($"Expected (1,2,3), got ({x},{y},{z})");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestLinearInterpolation()
        {
            var result = new TestResult { Name = "SampleBuffer.LinearInterpolation" };
            var buffer = new SampleBuffer();

            buffer.Add(0.0, 0, 0, 0);
            buffer.Add(1.0, 10, 0, 0);

            if (!buffer.TryEvaluate(0.5, out var x, out var y, out var z))
            {
                result.Fail("Should return true for interpolation");
                return result;
            }

            if (x < 4.9 || x > 5.1)
            {
                result.Fail($"Expected ~5.0 at t=0.5, got {x}");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestExtrapolation()
        {
            var result = new TestResult { Name = "SampleBuffer.Extrapolation" };
            var buffer = new SampleBuffer();

            buffer.Add(0.0, 0, 0, 0);
            buffer.Add(1.0, 10, 0, 0);

            if (!buffer.TryEvaluate(2.0, out var x, out var y, out var z))
            {
                result.Fail("Should return true for extrapolation");
                return result;
            }

            if (x < 19.9 || x > 20.1)
            {
                result.Fail($"Expected ~20.0 at t=2.0, got {x}");
                return result;
            }

            result.Pass();
            return result;
        }

        private static TestResult TestClear()
        {
            var result = new TestResult { Name = "SampleBuffer.Clear" };
            var buffer = new SampleBuffer();

            buffer.Add(1.0, 1, 1, 1);
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
