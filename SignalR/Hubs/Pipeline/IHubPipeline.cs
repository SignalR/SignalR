namespace SignalR.Hubs
{
    /// <summary>
    /// 
    /// </summary>
    public interface IHubPipeline
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="module"></param>
        /// <returns></returns>
        IHubPipeline AddModule(IHubPipelineModule module);
    }
}
