using System.Collections.Generic;
using System.IO;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Transports;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Tests
{
    public class ServerConnectionFacts
    {
        public class PopulateResponseState
        {
            [Fact]
            public void GroupTokenIsNullWhenNoGroups()
            {
                var response = new PersistentResponse();
                var groupSet = new DiffSet<string>(new string[] { });
                var serializer = new JsonNetSerializer();
                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => value);

                Connection.PopulateResponseState(response, groupSet, serializer, protectedData.Object, connectionId: null);

                Assert.Null(response.GroupsToken);
            }

            [Fact]
            public void GroupTokenIsNullWhenNoNewGroups()
            {
                var response = new PersistentResponse();
                var groupSet = new DiffSet<string>(new string[] { "a", "b", "c" });

                // Get the first diff
                groupSet.DetectChanges();

                var serializer = new JsonNetSerializer();
                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => value);

                Connection.PopulateResponseState(response, groupSet, serializer, protectedData.Object, connectionId: null);

                Assert.Null(response.GroupsToken);
            }

            [Fact]
            public void GroupTokenIsNotNullWhenGroupsChange()
            {
                var response = new PersistentResponse();
                var groupSet = new DiffSet<string>(new string[] { "a:1", "b:2", "c", "d" });

                groupSet.DetectChanges();

                groupSet.Add("g");

                var serializer = new Mock<IJsonSerializer>();
                HashSet<string> results = null;
                serializer.Setup(m => m.Serialize(It.IsAny<object>(), It.IsAny<TextWriter>()))
                          .Callback<object, TextWriter>((obj, tw) =>
                          {
                              results = new HashSet<string>((IEnumerable<string>)obj);
                              var jsonNet = new JsonNetSerializer();
                              jsonNet.Serialize(obj, tw);
                          });
                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => value);

                Connection.PopulateResponseState(response, groupSet, serializer.Object, protectedData.Object, connectionId: "myconnection");

                Assert.NotNull(response.GroupsToken);
                var parts = response.GroupsToken.Split(new[] { ':' }, 2);
                Assert.Equal(2, parts.Length);
                Assert.Equal("myconnection", parts[0]);
                Assert.True(results.Contains("a:1"));
                Assert.True(results.Contains("b:2"));
                Assert.True(results.Contains("c"));
                Assert.True(results.Contains("d"));
                Assert.True(results.Contains("g"));
            }

            [Fact]
            public void GroupTokenIsNotNullWhenGroupsChangeToEmpty()
            {
                var response = new PersistentResponse();
                var groupSet = new DiffSet<string>(new string[] { "b", "d" });

                groupSet.DetectChanges();

                groupSet.Remove("b");
                groupSet.Remove("d");

                var serializer = new Mock<IJsonSerializer>();
                HashSet<string> results = null;
                serializer.Setup(m => m.Serialize(It.IsAny<object>(), It.IsAny<TextWriter>()))
                          .Callback<object, TextWriter>((obj, tw) =>
                          {
                              results = new HashSet<string>((IEnumerable<string>)obj);
                              var jsonNet = new JsonNetSerializer();
                              jsonNet.Serialize(obj, tw);
                          });
                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => value);

                Connection.PopulateResponseState(response, groupSet, serializer.Object, protectedData.Object, connectionId: "myconnection");

                Assert.NotNull(response.GroupsToken);
                var parts = response.GroupsToken.Split(new[] { ':' }, 2);
                Assert.Equal(2, parts.Length);
                Assert.Equal("myconnection", parts[0]);
                Assert.Equal(0, results.Count);
            }
        }
    }
}
