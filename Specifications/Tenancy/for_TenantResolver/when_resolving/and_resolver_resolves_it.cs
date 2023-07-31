// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Execution;
using Aksio.IngressMiddleware.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Aksio.IngressMiddleware.Tenancy.for_TenantResolver.when_resolving;

public class and_resolver_resolves_it : Specification
{
    string tenant_id = "c392e7be-5cb4-4d1b-a461-7077e197309c";
    TenantResolver resolver;
    Mock<ITenantSourceIdentifierResolver> source_identifier_resolver;
    DefaultHttpContext context;
    Config config;
    TenantId result;

    void Establish()
    {
        source_identifier_resolver = new();
        config = new();
        config.Tenants[tenant_id] = new TenantConfig
        {
            SourceIdentifiers = new[] { "3610" }
        };

        resolver = new(
            config,
            source_identifier_resolver.Object,
            Mock.Of<ILogger<TenantResolver>>());

        context = new();

        source_identifier_resolver.Setup(_ => _.Resolve(config, context.Request)).ReturnsAsync("3610");
    }

    async Task Because() => result = await resolver.Resolve(context.Request);

    [Fact] void should_resolve_to_the_tenant_id() => result.Value.ShouldEqual(Guid.Parse(tenant_id));
}