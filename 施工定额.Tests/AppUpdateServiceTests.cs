using 施工定额.Service;
using Xunit;

namespace 施工定额.Tests
{
    public class AppUpdateServiceTests
    {
        [Theory]
        [InlineData("2.0.0", "1.0.0", true)]
        [InlineData("1.0.1", "1.0.0", true)]
        [InlineData("1.0.0", "1.0.0", false)]
        [InlineData("0.9.9", "1.0.0", false)]
        [InlineData("", "1.0.0", false)]
        [InlineData(null, "1.0.0", false)]
        [InlineData("not-a-version", "1.0.0", false)]
        public void IsNewerThanCurrent_ComparesVersions(string? remote, string currentText, bool expected)
        {
            var current = Version.Parse(currentText);
            Assert.Equal(expected, AppUpdateService.IsNewerThanCurrent(remote, current));
        }
    }
}
