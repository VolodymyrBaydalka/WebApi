using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZV.WebApi
{
    #region General
    [AttributeUsage(AttributeTargets.Interface)]
    public class WebApiAttribute : Attribute
    {
        public string Uri { get; set; }
        public MediaType MediaType { get; set; }
    }
    #endregion

    #region Methods
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class MethodAttribute : Attribute
    {
        public string UriTemplate { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute : MethodAttribute
    {
        public PostAttribute()
        {
        }

        public PostAttribute(string uriTemplate)
        {
            this.UriTemplate = uriTemplate;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class GetAttribute : MethodAttribute
    {
        public GetAttribute()
        {
        }

        public GetAttribute(string uriTemplate)
        {
            this.UriTemplate = uriTemplate;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute : MethodAttribute
    {
        public PutAttribute()
        {
        }

        public PutAttribute(string uriTemplate)
        {
            this.UriTemplate = uriTemplate;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DeleteAttribute : MethodAttribute
    {
        public DeleteAttribute()
        {
        }

        public DeleteAttribute(string uriTemplate)
        {
            this.UriTemplate = uriTemplate;
        }
    }
    #endregion

    #region Parameters
    [AttributeUsage(AttributeTargets.Parameter)]
    public abstract class ParameterAttribute : Attribute
    {
        public string Alias { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class PathAttribute : ParameterAttribute
    {
        public PathAttribute() { }
        public PathAttribute(string alias) { this.Alias = alias; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class QueryAttribute : ParameterAttribute
    {
        public QueryAttribute() { }
        public QueryAttribute(string alias) { this.Alias = alias; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class FieldAttribute : ParameterAttribute
    {
        public FieldAttribute() { }
        public FieldAttribute(string alias) { this.Alias = alias; }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public class BodyAttribute : ParameterAttribute
    {
        public BodyAttribute() { }
    }
    #endregion

    #region Etc
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface)]
    public class HeaderAttribute : Attribute {
        public string Key { get; set; }
        public string[] Values { get; set; }
        public HeaderAttribute(string key, params string[] values) {

            this.Key = key;
            this.Values = values;
        }
    }
    #endregion
}
