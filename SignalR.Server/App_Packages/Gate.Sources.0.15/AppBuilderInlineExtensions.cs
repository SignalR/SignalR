using System;
using System.Threading.Tasks;
using Owin;

namespace Gate
{
    internal static class AppBuilderInlineExtensions
    {
        public static IAppBuilder MapDirect(this IAppBuilder builder, string path, Func<Request, Response, Task> app)
        {
            return builder.Map(path, map => map.UseDirect(app));
        }

        public static IAppBuilder UseDirect(this IAppBuilder builder, Func<Request, Response, Task> app)
        {
            return builder.UseFunc<AppDelegate>(next => call =>
            {
                var req = new Request(call);
                var resp = new Response
                {
                    Next = () => next(call)
                };

                app.Invoke(req, resp)
                    .Catch(caught =>
                    {
                        resp.Error(caught.Exception);
                        return caught.Handled();
                    });
                return resp.ResultTask;
            });
        }
    }
}
