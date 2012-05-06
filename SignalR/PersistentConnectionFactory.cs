using System;

namespace SignalR
{
    /// <summary>
    /// Responsible for creating <see cref="PersistentConnection"/> instances.
    /// </summary>
    public class PersistentConnectionFactory
    {
        private readonly IDependencyResolver _resolver;

        /// <summary>
        /// Creates a new instance of the <see cref="PersistentConnectionFactory"/> class.
        /// </summary>
        /// <param name="resolver">The dependency resolver to use for when creating the <see cref="PersistentConnection"/>.</param>
        public PersistentConnectionFactory(IDependencyResolver resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException("resolver");
            }

            _resolver = resolver;
        }

        /// <summary>
        /// Creates an instance of the specified type using the dependency resolver or the type's default constructor.
        /// </summary>
        /// <param name="connectionType">The type of <see cref="PersistentConnection"/> to create.</param>
        /// <returns>An instance of a <see cref="PersistentConnection"/>. </returns>
        public PersistentConnection CreateInstance(Type connectionType)
        {
            if (connectionType == null)
            {
                throw new ArgumentNullException("connectionType");
            }

            var connection = (_resolver.Resolve(connectionType) ??
                              Activator.CreateInstance(connectionType)) as PersistentConnection;

            if (connection == null)
            {
                throw new InvalidOperationException(String.Format("'{0}' is not a {1}.", connectionType.FullName, typeof(PersistentConnection).FullName));
            }

            return connection;
        }
    }
}
