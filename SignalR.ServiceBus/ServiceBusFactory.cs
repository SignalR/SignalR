namespace SignalR.ServiceBus
{
    using System;
    using System.Data.Common;
    using System.Globalization;
    using System.Net;
    using Microsoft.ServiceBus;
    using Microsoft.ServiceBus.Messaging;

    sealed class ServiceBusFactory
    {
        readonly TokenProvider tokenProvider;
        readonly Uri endpoint;
        readonly int managementPort;
        readonly int runtimePort;

        public ServiceBusFactory(string connectionString)
        {
            DbConnectionStringBuilder builder = new DbConnectionStringBuilder 
            { 
                ConnectionString = connectionString 
            };

            string endpointString;
            if (!builder.TryGetStringValue("Endpoint", out endpointString))
            {
                throw new ArgumentException("Endpoint is not specified.", "connectionString");
            }

            if (!Uri.TryCreate(endpointString, UriKind.Absolute, out this.endpoint))
            {
                throw new ArgumentException(
                    string.Format(CultureInfo.CurrentCulture, "Endpoint is not valid. {0}", endpointString),
                    "connectionString");
            }

            string managementPortString;
            if (builder.TryGetStringValue("ManagementPort", out managementPortString))
            {
                if (!int.TryParse(managementPortString, out this.managementPort))
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, "Malformed ManagementPort. {0}", managementPortString),
                        "connectionString");
                }
            }
            else
            {
                this.managementPort = -1;
            }

            string runtimePortString;
            if (builder.TryGetStringValue("RuntimePort", out runtimePortString))
            {
                if (!int.TryParse(runtimePortString, out this.runtimePort))
                {
                    throw new ArgumentException(
                        string.Format(CultureInfo.CurrentCulture, "Malformed RuntimePort. {0}", runtimePortString),
                        "connectionString");
                }
            }
            else
            {
                this.runtimePort = -1;
            }

            TokenProviderFactory factory = CreateTokenProviderFactory(builder);
            this.tokenProvider = factory.Create();
        }

        public TokenProvider TokenProvider
        {
            get { return this.tokenProvider; }
        }

        public IAsyncResult BeginCreateMessagingFactory(AsyncCallback callback, object state)
        {
            UriBuilder uriBuilder = new UriBuilder(this.endpoint)
            {
                Scheme = "sb://",
                Port = this.runtimePort
            };

            return MessagingFactory.BeginCreate(uriBuilder.Uri, this.tokenProvider, callback, state);
        }

        public MessagingFactory EndCreateMessagingFactory(IAsyncResult asyncResult)
        {
            return MessagingFactory.EndCreate(asyncResult);
        }

        public NamespaceManager CreateNamespaceManager()
        {
            UriBuilder uriBuilder = new UriBuilder(this.endpoint)
            {
                Scheme = Uri.UriSchemeHttps,
                Port = this.managementPort
            };

            return new NamespaceManager(uriBuilder.Uri, this.tokenProvider);
        }

        static TokenProviderFactory CreateTokenProviderFactory(DbConnectionStringBuilder builder)
        {
            object provider;
            if (!builder.TryGetValue("provider", out provider))
            {
                throw new ArgumentException("'Provider' was not supplied.", "builder");
            }

            string providerString = (string)provider;
            
            if (string.Equals(providerString, "SharedSecret", StringComparison.OrdinalIgnoreCase))
            {
                return new SharedSecretTokenProviderFactory(builder);
            }

            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, "Unrecognized provider. {0}", providerString), 
                "builder");
        }
        
        abstract class TokenProviderFactory
        {
            public abstract TokenProvider Create();
        }

        sealed class SharedSecretTokenProviderFactory : TokenProviderFactory
        {
            readonly string issuerName;
            readonly string issuerKey;
            readonly Uri stsUri;

            public SharedSecretTokenProviderFactory(DbConnectionStringBuilder builder)
            {
                if (!builder.TryGetStringValue("SharedSecretIssuer", out this.issuerName))
                {
                    throw new ArgumentException("SharedSecretIssuer is required.", "builder");
                }

                if (!builder.TryGetStringValue("SharedSecretValue", out this.issuerKey))
                {
                    throw new ArgumentException("SharedSecretValue is required.", "builder");
                }

                string stsUriAddress;
                if (builder.TryGetStringValue("StsEndpoint", out stsUriAddress))
                {
                    if (!Uri.TryCreate(stsUriAddress, UriKind.Absolute, out stsUri))
                    {
                        throw new ArgumentException("StsEndpoint is not a valid address.", "builder");
                    }
                }
            }

            public override TokenProvider Create()
            {
                if (stsUri != null)
                {
                    return TokenProvider.CreateSharedSecretTokenProvider(this.issuerName, this.issuerKey, stsUri);
                }
                else
                {
                    return TokenProvider.CreateSharedSecretTokenProvider(this.issuerName, this.issuerKey);
                }
            }
        }
    }
}
