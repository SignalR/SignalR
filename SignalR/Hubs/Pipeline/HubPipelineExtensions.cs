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
            pipeline.AddModule(new AttributeAuthModule(new AuthorizeAttribute()));
        }
    }
}
