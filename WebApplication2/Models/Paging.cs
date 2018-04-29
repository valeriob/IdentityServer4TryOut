using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.AspNetCore.Routing;
using R4Mvc.ModelUnbinders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace WebApplication2.Models
{
    //[TypeScriptModule("Server")]
    public abstract class PagingViewModel
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public string OrderBy { get; set; }
        public bool OrderByDescending { get; set; }

        public PagingViewModel()
        {
            Page = 1;
            PageSize = 25;
        }


        public int TotalPages()
        {
            return (int)Math.Max(1, Math.Ceiling((double)TotalItems / PageSize));

        }
        public string NextPageUrl(IUrlHelper url, IActionResult route)
        {
            var idx = route.GetRouteValueDictionary();
            idx["Page"] = Math.Min(TotalPages(), Page + 1);

            return url.Action(route);
        }


        public string PrevPageUrl(IUrlHelper url, IActionResult route)
        {
            var idx = route.GetRouteValueDictionary();
            idx["Page"] = Math.Max(1, Page - 1);

            return url.Action(route);
        }

        public string OrderbyUrl(IUrlHelper url, LambdaExpression expression, IActionResult route)
        {
            var propertyName = ExpressionHelper.GetExpressionText(expression);
            var idx = route.GetRouteValueDictionary();

            if (OrderBy == propertyName)
            {
                idx["OrderByDescending"] = !OrderByDescending;
            }
            else
            {
                idx["OrderBy"] = propertyName;
                idx["OrderByDescending"] = false;
            }

            return url.Action(route);
        }

        public string OrderbyCss<TModel, TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            var na = "fa fa-sort";
            var down = "fa fa-sort-desc";
            var up = "fa fa-sort-asc";
            var propertyName = ExpressionHelper.GetExpressionText(expression);
            if (OrderBy == propertyName)
            {
                if (OrderByDescending)
                    return down;
                else
                    return up;
            }
            else
                return na;
        }

        public abstract string PrevPageUrl(IUrlHelper url);
        public abstract string NextPageUrl(IUrlHelper url);
    }

    public class SimplePropertyModelUnbinder : IModelUnbinder
    {
        public virtual void UnbindModel(RouteValueDictionary routeValueDictionary, string routeName, object routeValue)
        {
            var dict = new RouteValueDictionary(routeValue);
            foreach (var entry in dict)
            {
                var name = entry.Key;

                if (!(entry.Value is string) && (entry.Value is System.Collections.IEnumerable))
                {
                    if (IncludeProperty(routeValue, entry))
                    {
                        var enumerableValue = (System.Collections.IEnumerable)entry.Value;
                        var i = 0;
                        foreach (var enumerableElement in enumerableValue)
                        {
                            ModelUnbinderHelpers.AddRouteValues(routeValueDictionary, string.Format("{0}", name), enumerableElement);
                            i++;
                        }
                    }
                }
                else
                {
                    ModelUnbinderHelpers.AddRouteValues(routeValueDictionary, name, entry.Value);
                }
            }
        }

        bool IncludeProperty(object routeValue, KeyValuePair<string, object> entry)
        {
            var includeAttributes = routeValue.GetType().GetProperty(entry.Key).GetCustomAttributes(typeof(IncludeAttribute), true);
            return includeAttributes.Any();
        }
    }

    public class IncludeAttribute : Attribute
    {

    }


    public static class PagingExtensions
    {
        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> dbQuery, Paging paging, Expression<Func<T, object>> orderByDefault)
        {
            if (string.IsNullOrEmpty(paging.OrderBy))
            {
                if (paging.OrderByDescending)
                    dbQuery = dbQuery.OrderByDescending(orderByDefault);
                else
                    dbQuery = dbQuery.OrderBy(orderByDefault);
            }
            else
            {
                dbQuery = dbQuery.OrderByName(paging.OrderBy, paging.OrderByDescending);
            }

            dbQuery = dbQuery.Skip((paging.Page - 1) * paging.PageSize).Take(paging.PageSize);

            return dbQuery;
        }

        public static IQueryable<T> OrderByName<T>(this IQueryable<T> source, string propertyName, bool isDescending)
        {

            if (source == null) throw new ArgumentNullException("source");
            if (propertyName == null) throw new ArgumentNullException("propertyName");

            var type = typeof(T);
            var arg = Expression.Parameter(type, "x");

            var pi = type.GetProperty(propertyName);
            Expression expr = Expression.Property(arg, pi);
            type = pi.PropertyType;

            var delegateType = typeof(Func<,>).MakeGenericType(typeof(T), type);
            var lambda = Expression.Lambda(delegateType, expr, arg);

            String methodName = isDescending ? "OrderByDescending" : "OrderBy";
            object result = typeof(Queryable).GetMethods().Single(
                method => method.Name == methodName
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), type)
                .Invoke(null, new object[] { source, lambda });
            return (IQueryable<T>)result;
        }
    }

    public class Paging
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public bool OrderByDescending { get; set; }

        public Paging()
        {
            Page = 1;
            PageSize = 128;
        }
    }
}