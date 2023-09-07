// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Execution;
using Aksio.IngressMiddleware.BearerTokens;
using Aksio.IngressMiddleware.Configuration;
using Aksio.IngressMiddleware.Identities;
using Aksio.IngressMiddleware.Impersonation;
using Aksio.IngressMiddleware.MutualTLS;
using Aksio.IngressMiddleware.Tenancy;

namespace Aksio.IngressMiddleware;

/// <summary>
/// Represents the root route for the ingress middleware.
/// </summary>
/// <remarks>
/// This is the default route used for authorizing and identity requests.
/// </remarks>
[Route("/")]
public class RequestAugmenter : Controller
{
    readonly Config _config;
    readonly IIdentityDetailsResolver _identityDetailsResolver;
    readonly IImpersonationFlow _impersonationFlow;
    readonly ITenantResolver _tenantResolver;
    readonly IOAuthBearerTokens _bearerTokens;
    readonly IMutualTLS _mutualTls;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestAugmenter"/> class.
    /// </summary>
    /// <param name="identityDetailsResolver"><see cref="IIdentityDetailsResolver"/> to use.</param>
    /// <param name="impersonationFlow"><see cref="IImpersonationFlow"/> to use for the impersonation process.</param>
    /// <param name="tenantResolver"><see cref="ITenantResolver"/> to use.</param>
    /// <param name="bearerTokens"><see cref="IOAuthBearerTokens"/> to use.</param>
    /// <param name="mutualTls"><see cref="IMutualTLS"/> to use.</param>
    /// <param name="config">The instance configuration.</param>
    public RequestAugmenter(
        IIdentityDetailsResolver identityDetailsResolver,
        IImpersonationFlow impersonationFlow,
        ITenantResolver tenantResolver,
        IOAuthBearerTokens bearerTokens,
        IMutualTLS mutualTls,
        Config config)
    {
        _identityDetailsResolver = identityDetailsResolver;
        _impersonationFlow = impersonationFlow;
        _tenantResolver = tenantResolver;
        _bearerTokens = bearerTokens;
        _mutualTls = mutualTls;
        _config = config;
    }

    /// <summary>
    /// Handles GET requests to the root route.
    /// </summary>
    /// <returns><see cref="IActionResult"/>.</returns>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        // First determine the tenant id, it is required for all strategies except "None".
        var tenantId = await ResolveTenantId();
        if (tenantId == null)
        {
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        // Handle mTLS, if enabled.
        if (_mutualTls.IsEnabled())
        {
            return _mutualTls.Handle(Request);
        }

        // Handle impersonation, if appropriate.
        if (!_impersonationFlow.HandleImpersonatedPrincipal(Request, Response) &&
            _impersonationFlow.ShouldImpersonate(Request))
        {
            Response.Headers[Headers.ImpersonationRedirect] = WellKnownPaths.Impersonation;
            return StatusCode(StatusCodes.Status401Unauthorized);
        }

        if (_impersonationFlow.IsImpersonateRoute(Request))
        {
            return Ok();
        }

        // Use the identity details resolver, if configured.
        if (!await _identityDetailsResolver.Resolve(Request, Response, tenantId))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        // Handle OAuth bearer tokens, if enabled.
        if (_bearerTokens.IsEnabled())
        {
            return await _bearerTokens.Handle(Request, Response, tenantId);
        }

        return Ok();
    }

    /// <summary>
    /// Attempts to resolve the tenant id.
    /// Will return null if it fails to determine one, and the configured strategy is not None.
    /// </summary>
    /// <returns>The resolved tenant. TenantId.NotSet if resolver type is None, and null if not able to resolve tenant.</returns>
    async Task<TenantId?> ResolveTenantId()
    {
        // Require a configured strategy.
        if (_config.TenantResolution.Strategy == TenantSourceIdentifierResolverType.Undefined)
        {
            return null;
        }

        if (_config.TenantResolution.Strategy == TenantSourceIdentifierResolverType.None)
        {
            return TenantId.NotSet;
        }

        if (!await _tenantResolver.CanResolve(Request))
        {
            return null;
        }

        var tenantId = await _tenantResolver.Resolve(Request);
        if (tenantId == TenantId.NotSet && _config.TenantResolution.Strategy != TenantSourceIdentifierResolverType.None)
        {
            return null;
        }

        Response.Headers[Headers.TenantId] = tenantId.ToString();
        return tenantId;
    }
}