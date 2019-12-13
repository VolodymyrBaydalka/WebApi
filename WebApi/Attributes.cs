using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DuncanApps.WebApi
{
    #region General
    [AttributeUsage(AttributeTargets.Interface)]
    public class WebApiAttribute : Attribute
    {
        /// <summary>
        /// Relative url to API
        /// </summary>
        public string Uri { get; set; }
        /// <summary>
        /// API default media type 
        /// </summary>
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

    /// <summary>
    /// Indicates that method will send PUT method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class GetAttribute : MethodAttribute
    {
        public GetAttribute()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uriTemplate">Uri template. Parts surrounded by { and } will be replace with corresponding parameters.</param>
        public GetAttribute(string uriTemplate)
        {
            this.UriTemplate = uriTemplate;
        }
    }

    /// <summary>
    /// Indicates that method will send PUT method
    /// </summary>
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

    /// <summary>
    /// Indicates that method will send DELETE method
    /// </summary>
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

    /// <summary>
    /// Indicates that parameter will be use as argument of path template
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class PathAttribute : ParameterAttribute
    {
        public PathAttribute() { }
        public PathAttribute(string alias) { this.Alias = alias; }
    }

    /// <summary>
    /// Indicates that parameter will be use as part of query
    /// </summary>
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

    /// <summary>
    /// Indicates that parameter will be use as body of post/put request 
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter)]
    public class BodyAttribute : ParameterAttribute
    {
        public BodyAttribute() { }
    }
    #endregion

    #region Etc
    /// <summary>
    /// Sets headers to api/method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true)]
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
