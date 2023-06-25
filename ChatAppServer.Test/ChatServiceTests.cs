using Xunit;
using ChatAppServer.src.ManageChat;
using System;

namespace ChatAppServer.Test
{
    public class ChatServiceManagerTests
    {
        [Fact]
        public void GetLoginDataFromString_ShouldProcessStandardInput()
        {
            string testLogin = "login";
            string testPassword = "password";
            string loginDataMessage = $"[login]{testLogin}[password]{testPassword}";

            (string resultLogin, string resultPassword) = ChatServiceManager.GetLoginDataFromString(loginDataMessage);

            Assert.Equal(testLogin, resultLogin);
            Assert.Equal(testPassword, resultPassword);
        }

        [Theory]
        [InlineData("", "password")]
        [InlineData("login", "")]
        [InlineData("", "")]
        public void GetLoginDataFromString_ThrowsOnEmptyStringInput(string login, string password)
        {
            string loginDataMessage = $"[login]{login}[password]{password}";

            Assert.Throws<ArgumentException>(() => ChatServiceManager.GetLoginDataFromString(loginDataMessage));
        }

        [Theory]
        [InlineData(" ", "password", "login")]
        [InlineData("login", " ",  "password")]
        [InlineData(" ", " ", "login")]
        public void GetLoginDataFromString_ThrowsOnWhiteSpaceEntries(string login, string password, string failingArgument)
        {
            string loginDataMessage = $"[login]{login}[password]{password}";

            Assert.Throws<ArgumentException>(failingArgument,
                () => ChatServiceManager.GetLoginDataFromString(loginDataMessage));
        }
    }
}