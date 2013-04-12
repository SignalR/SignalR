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

                Connection.PopulateResponseState(response, groupSet, serializer, protectedData.Object);

                Assert.Null(response.GroupsToken);
            }

            [Fact]
            public void GroupTokenIsNullWhenNoNewGroups()
            {
                var response = new PersistentResponse();
                var groupSet = new DiffSet<string>(new string[] { "a", "b", "c" });

                // Get the first diff
                groupSet.GetDiff();

                var serializer = new JsonNetSerializer();
                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => value);

                Connection.PopulateResponseState(response, groupSet, serializer, protectedData.Object);

                Assert.Null(response.GroupsToken);
            }

            [Fact]
            public void GroupTokenIsNotNullWhenGroupsChange()
            {
                var response = new PersistentResponse();
                var groupSet = new DiffSet<string>(new string[] { "a", "b", "c", "d" });

                groupSet.GetDiff();

                groupSet.Add("g");

                var serializer = new Mock<IJsonSerializer>();
                HashSet<string> results = null;
                serializer.Setup(m => m.Serialize(It.IsAny<object>(), It.IsAny<TextWriter>()))
                          .Callback<object, TextWriter>((obj, tw) => {
                              results = new HashSet<string>((IEnumerable<string>)obj);
                              var jsonNet = new JsonNetSerializer();
                              jsonNet.Serialize(obj, tw);
                          });
                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => value);

                Connection.PopulateResponseState(response, groupSet, serializer.Object, protectedData.Object);

                Assert.NotNull(response.GroupsToken);
                Assert.True(results.Contains("a"));
                Assert.True(results.Contains("b"));
                Assert.True(results.Contains("c"));
                Assert.True(results.Contains("d"));
                Assert.True(results.Contains("g"));
            }
        }
    }
}
