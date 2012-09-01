using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Owin;

namespace Gate.Mapping
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    class MapBuilder : IAppBuilder
    {
        readonly IAppBuilder _builder;
        readonly IDictionary<string, AppFunc> _map;
        readonly Func<AppFunc, IDictionary<string, AppFunc>, AppFunc> _mapper;

        public MapBuilder(IAppBuilder builder, Func<AppFunc, IDictionary<string, AppFunc>, AppFunc> mapper)
        {
            _map = new Dictionary<string, AppFunc>();
            _mapper = mapper;
            _builder = builder.UseFunc<AppFunc>(a => _mapper(a, _map));
        }

        public void MapInternal(string path, AppFunc app)
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
