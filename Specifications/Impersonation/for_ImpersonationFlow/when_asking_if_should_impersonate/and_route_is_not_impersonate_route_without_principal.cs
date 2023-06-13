// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Aksio.IngressMiddleware.Impersonation.when_asking_if_should_impersonate;

public class and_route_is_not_impersonate_route_without_principal : given.a_impersonation_flow
{
    DefaultHttpContext http_context;
    bool result;

    void Establish()
    {
        var rawPrincipal = new RawClientPrincipal("aad", string.Empty, string.Empty, Enumerable.Empty<RawClaim>());
        var rawPrincipalAsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rawPrincipal)));

        http_context = new();
        http_context.Request.Headers[Headers.OriginalUri] = "/something/random";

        config.Impersonation.IdentityProviders = new[]
        {
            "aad"
        };
    }

    void Because() => result = flow.ShouldImpersonate(http_context.Request);

    [Fact] void should_not_impersonate() => result.ShouldBeFalse();
}
