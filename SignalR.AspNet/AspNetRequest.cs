﻿using System;
using System.Collections.Specialized;
using System.Web;
using SignalR.Abstractions;
using Microsoft.Web.Infrastructure.DynamicValidationHelper;

namespace SignalR.AspNet
{
    public class AspNetRequest : IRequest
    {
        private readonly HttpRequestBase _request;
        private NameValueCollection _form;
        private NameValueCollection _queryString;

        public AspNetRequest(HttpRequestBase request)
        {
            _request = request;
            Cookies = new NameValueCollection();
            foreach (string key in request.Cookies)
            {
                Cookies.Add(key, request.Cookies[key].Value);
            }

            // Since the ValidationUtility has a dependency on HttpContext (not HttpContextBase) we
            // need to check if we're out of HttpContext to preserve testability.
            if (HttpContext.Current == null)
            {
                _form = _request.Form;
                _queryString = _request.QueryString;
            }
            else
            {
                Func<NameValueCollection> formGetter, queryGetter;
                ValidationUtility.GetUnvalidatedCollections(HttpContext.Current, out formGetter, out queryGetter);
                _form = formGetter();
                _queryString = queryGetter();
            }
        }

        public Uri Url
        {
            get
            {
                return _request.Url;
            }
        }

        public NameValueCollection QueryString
        {
            get
            {
                return _queryString;
            }
        }

        public NameValueCollection Headers
        {
            get
            {
                return _request.Headers;
            }
        }

        public NameValueCollection Form
        {
            get
            {
                return _form;
            }
        }

        public NameValueCollection Cookies
        {
            get;
            private set;
        }
    }
}
