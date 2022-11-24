// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Aksio.IngressMiddleware;

public static class AzureAd
{
    public static async Task HandleAuthorize(Config config, HttpRequest request, HttpResponse response)
    {
        var query = request.Query
            .WithRedirectUri(config.AzureAd.Callback)
            .ToDictionary(_ => _.Key, _ => _.Value);
        var queryString = query.ToQueryString();
        var url = $"{config.AzureAd.AuthorizationEndpoint}?{queryString}";
        response.Redirect(url);
        await Task.CompletedTask;
    }

    public static async Task HandleCallback(Config config, HttpRequest request, HttpResponse response)
    {
        request.Query.TryGetValue("code", out var code);
        var tokens = await OpenIDConnect.ExchangeCodeForAccessToken(config.AzureAd, code);
        await AzureContainerAppAuth.Login(config.AzureAd, request, response, tokens);
        response.RedirectToOrigin(request);
        await Task.CompletedTask;
    }

    public static async Task HandleWellKnownConfiguration(Config config, HttpRequest request, HttpResponse response)
    {
        var client = new HttpClient();
        var url = $"{config.AzureAd.Issuer}/.well-known/openid-configuration";
        var result = await client.GetAsync(url);
        var json = await result.Content.ReadAsStringAsync();

        var document = (JsonNode.Parse(json) as JsonObject)!;
        document["authorization_endpoint"] = config.AzureAd.AuthorizationEndpoint;
        await response.WriteAsJsonAsync(document);
    }
}
