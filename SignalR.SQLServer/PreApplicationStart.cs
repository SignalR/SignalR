using System.Web;
using System.Web.Script.Serialization;
using SignalR.Infrastructure;
using SignalR.SQL;

[assembly: PreApplicationStartMethod(typeof(PreApplicationStart), "Start")]

namespace SignalR.SQL {
    public static class PreApplicationStart {

        public static void Start() {
            var serializer = new JavaScriptSerializerAdapter(new JavaScriptSerializer {
                MaxJsonLength = 30 * 1024 * 1024
            });
            DependencyResolver.Register(typeof(IJsonSerializer), () => serializer);
        }

    }
}