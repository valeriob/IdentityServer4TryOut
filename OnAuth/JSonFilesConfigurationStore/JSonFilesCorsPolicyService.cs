﻿using IdentityServer4.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OnAuth.JSonFilesConfigurationStore
{
    public class JSonFilesCorsPolicyService : ICorsPolicyService
    {
        readonly IHttpContextAccessor _context;
        readonly ILogger<JSonFilesCorsPolicyService> _logger;

        public JSonFilesCorsPolicyService(IHttpContextAccessor context, ILogger<JSonFilesCorsPolicyService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        /// <summary>
        /// Determines whether origin is allowed.
        /// </summary>
        /// <param name="origin">The origin.</param>
        /// <returns></returns>
        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            // doing this here and not in the ctor because: https://github.com/aspnet/CORS/issues/105
            //var dbContext = _context.HttpContext.RequestServices.GetRequiredService<IConfigurationDbContext>();

            //var origins = dbContext.Clients.SelectMany(x => x.AllowedCorsOrigins.Select(y => y.Origin)).ToList();

            //var distinctOrigins = origins.Where(x => x != null).Distinct();

            //var isAllowed = distinctOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);

            //_logger.LogDebug("Origin {origin} is allowed: {originAllowed}", origin, isAllowed);

            //return Task.FromResult(isAllowed);
            throw new NotImplementedException();
        }
    }
}
