namespace SignalR.Hubs
{
    public static class HubPipelineExtensions
    {
        public static void EnableAutoRejoiningGroups(this IHubPipeline pipeline)
        {
            pipeline.AddModule(new AutoRejoiningGroupsModule());
        }

        public static void RequireAuthentication(this IHubPipeline pipeline)
        {
            var authorizer = new AuthorizeAttribute();
            pipeline.AddModule(new AuthorizeModule(globalConnectionAuthorizer: authorizer, globalInvocationAuthorizer: authorizer));
        }
    }
}
