using Xunit;

namespace CoreEngine.UnitTests
{
    public class MathUtilsTests
    {
        [Theory]
        [InlineData(0.0f, 0.0f)]
        [InlineData(1.0f, 0.0175f)]
        [InlineData(45.0f, 0.7854f)]
        [InlineData(180.0f, 3.1416f)]
        public void DegreesToRad_Values_HasCorrectResult(float inputValue, float expectedValue)
        {
            // Act
            var output = MathUtils.DegreesToRad(inputValue);

            // Assert
            Assert.Equal(expectedValue, output, 4);
        }

        [Theory]
        [InlineData(0.0f, 0.0f)]
        [InlineData(1.0f, 57.2958f)]
        [InlineData(3.1416f, 180.0004f)]
        public void RadToDegrees_Values_HasCorrectResult(float inputValue, float expectedValue)
        {
            // Act
            var output = MathUtils.RadToDegrees(inputValue);

            // Assert
            Assert.Equal(expectedValue, output, 4);
        }
    }
}