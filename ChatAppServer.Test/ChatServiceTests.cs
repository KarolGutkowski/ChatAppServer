using Xunit;
using ChatAppServer.src.ManageChat;
using System;
using System.Collections.Generic;
using System.Collections;

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
        [MemberData(nameof(LoginWithWhiteSpaceData))]
        public void GetLoginDataFromString_ThrowsOnWhiteSpaceEntries(string login, string password, string failingArgument)
        {
            string loginDataMessage = $"[login]{login}[password]{password}";

            Assert.Throws<ArgumentException>(failingArgument,
                () => ChatServiceManager.GetLoginDataFromString(loginDataMessage));
        }

        public static IEnumerable<object[]> LoginWithWhiteSpaceData()
        {
            yield return new object[] { " ", "password", "login" };
            yield return new object[] { "login", " ", "password" };
            yield return new object[] { " ", " ", "login" };
        }

        [Theory]
        [ClassData(typeof(LoginWithEmptyData))]
        public void GetLoginDataFromStringWithClassData(string login, string password)
        {
            string loginDataMessage = $"[login]{login}[password]{password}";

            Assert.Throws<ArgumentException>(() => ChatServiceManager.GetLoginDataFromString(loginDataMessage));
        }

        public class LoginWithEmptyData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "", "password" };
                yield return new object[] { "login", "" };
                yield return new object[] { "", "" };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

    }
}