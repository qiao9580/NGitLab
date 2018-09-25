using System;
using System.Linq;
using NGitLab.Models;
using NUnit.Framework;

namespace NGitLab.Tests
{
    public class UsersTests
    {
        private readonly IUserClient _users;

        public UsersTests()
        {
            _users = Initialize.GitLabClient.Users;
        }


        [Test]
        public void GetUsers()
        {
            var users = _users.All.ToArray();
            CollectionAssert.IsNotEmpty(users);
        }

        [Test]
        public void GetUser()
        {
            var user = _users[_users.Current.Id];
            Assert.IsNotNull(user);
            Assert.That(user.Username, Is.EqualTo(_users.Current.Username));
        }

        [Test]
        public void CreateUpdateDelete()
        {
            if (!Initialize.IsAdmin)
            {
                Assert.Inconclusive("Cannot test the creation of users since the current user is not admin");
            }

            var userUpsert = new UserUpsert
            {
                Email = "test@test.pl",
                Bio = "bio",
                CanCreateGroup = true,
                IsAdmin = true,
                Linkedin = null,
                Name = "sadfasdf",
                Password = "!@#$QWDRQW@",
                ProjectsLimit = 1000,
                Provider = "provider",
                Skype = "skype",
                Twitter = "twitter",
                Username = "username",
                WebsiteURL = "wp.pl"
            };

            var addedUser = _users.Create(userUpsert);
            Assert.That(addedUser.Bio, Is.EqualTo(userUpsert.Bio));

            userUpsert.Bio = "Bio2";
            userUpsert.Email = "test@test.pl";

            var updatedUser = _users.Update(addedUser.Id, userUpsert);
            Assert.That(updatedUser.Bio, Is.EqualTo(userUpsert.Bio));

            _users.Delete(addedUser.Id);

            TestCurrent(userUpsert);
        }

        public void TestCurrent(UserUpsert user)
        {
            var client = new GitLabClient(Initialize.GitLabHost, user.Username, user.Password).Users;

            var session = client.Current;
            Assert.That(session, Is.Not.Null);
            Assert.That(session.CreatedAt.Date, Is.EqualTo(DateTime.Now.Date));
            Assert.That(session.Email, Is.EqualTo(user.Email));
            Assert.That(session.Name, Is.EqualTo(user.Name));
            Assert.That(session.PrivateToken, Is.Not.Null);
        }

        [Test]
        public void Test_can_add_an_ssh_key_to_the_gitlab_profile()
        {
            var users = _users;
            var keys = users.CurrentUserSShKeys;
            var keysBefore = keys.All.ToArray();
            var keyNumber = keysBefore.Length;
            var newKey = "ssh-rsa dummy mytestkey@mycurrentpc";

            var currentKey = keysBefore.FirstOrDefault(k => k.Key.Equals(newKey));
            if (currentKey != null)
            {
                keys.Remove(currentKey.Id);
                keyNumber -= 1;
            }

            var result = keys.Add(new SshKeyCreate
            {
                Key = newKey,
                Title = "test key",
            });

            Assert.IsNotNull(result);
            Assert.AreEqual(keyNumber + 1, keys.All.ToArray().Length);

            keys.Remove(result.Id);
            Assert.AreEqual(keyNumber, keys.All.ToArray().Length);
        }

        [Test]
        public void CreateTokenAsAdmin_ReturnsUserToken()
        {
            if (!Initialize.IsAdmin)
            {
                Assert.Inconclusive("Cannot test the creation of users since the current user is not admin");
            }

            var tokenRequest = new UserTokenCreate
            {
                UserId = _users.Current.Id,
                Name = $"Test_Create_{DateTime.Now.ToString("yyyyMMddHHmmss")}",
                ExpiresAt = DateTime.Now.AddDays(1),
                Scopes = new[] { "api", "read_user" }
            };

            var tokenResult = _users.CreateToken(tokenRequest);

            Assert.IsNotEmpty(tokenResult.Token);
            Assert.AreEqual(tokenRequest.Name, tokenResult.Name);
        }
    }
}