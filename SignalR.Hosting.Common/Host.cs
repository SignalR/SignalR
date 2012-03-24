namespace SignalR.Hosting.Common
{
    public class Host
    {
        private readonly IDependencyResolver _resolver;

        public Host()
            : this(Global.DependencyResolver)
        {

        }

        public Host(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public IDependencyResolver DependencyResolver
        {
            get
            {
                return _resolver;
            }
        }

        public IConnectionManager ConnectionManager
        {
            get
            {
                return DependencyResolver.Resolve<IConnectionManager>();
            }
        }

        public IConfigurationManager Configuration
        {
            get
            {
                return DependencyResolver.Resolve<IConfigurationManager>();
            }
        }
    }
}
