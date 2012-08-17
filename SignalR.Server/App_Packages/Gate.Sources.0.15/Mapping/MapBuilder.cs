using System;
using System.Collections.Generic;
using Owin;

namespace Gate.Mapping
{
    class MapBuilder : IAppBuilder
    {
        readonly IAppBuilder _builder;
        readonly IDictionary<string, AppDelegate> _map;
        readonly Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> _mapper;

        public MapBuilder(IAppBuilder builder, Func<AppDelegate, IDictionary<string, AppDelegate>, AppDelegate> mapper)
        {
            _map = new Dictionary<string, AppDelegate>();
            _mapper = mapper;
            _builder = builder.UseFunc<AppDelegate>(a => _mapper(a, _map));
        }

        public void MapInternal(string path, AppDelegate app)
        {
            _map[path] = app;
        }
        
        public IAppBuilder Use(object middleware, params object[] args)
        {
            return _builder.Use(middleware, args);
        }

        public object Build(Type returnType)
        {
            return _builder.Build(returnType);
        }

        public IAppBuilder New()
        {
            return _builder.New();
        }

        public IAppBuilder AddSignatureConversion(Delegate conversion)
        {
            return _builder.AddSignatureConversion(conversion);
        }

        public IDictionary<string, object> Properties
        {
            get { return _builder.Properties; }
        }
    }
}
