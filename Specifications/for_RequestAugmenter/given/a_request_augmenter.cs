// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Execution;
using Aksio.IngressMiddleware.BearerTokens;
using Aksio.IngressMiddleware.Configuration;
using Aksio.IngressMiddleware.Identities;
using Aksio.IngressMiddleware.Impersonation;
using Aksio.IngressMiddleware.MutualTLS;
using Aksio.IngressMiddleware.RoleAuthorization;
using Aksio.IngressMiddleware.Tenancy;
using Microsoft.AspNetCore.Http;

namespace Aksio.IngressMiddleware.for_RequestAugmenter.given;

public class a_request_augmenter : Specification
{
    protected static readonly TenantId tenant_id = new(Guid.Parse("117a700a-f271-49af-a301-5d54cc2a8c9d"));
    protected Mock<IIdentityDetailsResolver> identity_details_resolver;
    protected Mock<IImpersonationFlow> impersonation_flow;
    protected Mock<ITenantResolver> tenant_resolver;
    protected Mock<IOAuthBearerTokens> bearer_tokens;
    protected Mock<IMutualTLS> mutual_tls;
    protected Mock<IRoleAuthorizer> endtraid_roles;
    protected RequestAugmenter augmenter;
    protected HttpRequest request;
    protected HttpResponse response;
    protected Config config;

    void Establish()
    {
        identity_details_resolver = new();
        impersonation_flow = new();
        tenant_resolver = new();
        tenant_resolver.Setup(_ => _.CanResolve(IsAny<HttpRequest>())).ReturnsAsync(true);
        tenant_resolver.Setup(_ => _.Resolve(IsAny<HttpRequest>())).ReturnsAsync(tenant_id);
        bearer_tokens = new();
        mutual_tls = new();
        endtraid_roles = new();
        config = new()
        {
            TenantResolution = new()
            {
                Strategy = TenantSourceIdentifierResolverType.Specified,
                Options = JsonSerializer.Deserialize<JsonObject>(
                    JsonSerializer.Serialize(new SpecifiedSourceIdentifierResolverOptions() { TenantId = tenant_id.ToString() }))
            }
        };

        augmenter = new(
            identity_details_resolver.Object,
            impersonation_flow.Object,
            tenant_resolver.Object,
            bearer_tokens.Object,
            mutual_tls.Object,
            endtraid_roles.Object,
            config)
        {
            ControllerContext = new()
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        request = augmenter.ControllerContext.HttpContext.Request;
        response = augmenter.ControllerContext.HttpContext.Response;
    }
}