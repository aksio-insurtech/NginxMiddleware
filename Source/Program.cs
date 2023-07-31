﻿// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.IngressMiddleware;
using Aksio.IngressMiddleware.BearerTokens;
using Aksio.IngressMiddleware.Configuration;
using Aksio.IngressMiddleware.Identities;
using Aksio.IngressMiddleware.Impersonation;
using Aksio.IngressMiddleware.Tenancy;
using IngressMiddleware.MutualTLS;

UnhandledExceptionsManager.Setup();

var config = Config.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMvc();
builder.Services.AddControllers();
builder.Services.AddSingleton(config);
builder.Services.AddTenantSourceIdentifierResolver();
builder.Services.AddTransient<ITenantResolver, TenantResolver>();
builder.Services.AddTransient<IIdentityDetailsResolver, IdentityDetailsResolver>();
builder.Services.AddTransient<IOAuthBearerTokens, OAuthBearerTokens>();
builder.Services.AddTransient<IMutualTLS, MutualTLS>();
builder.Services.AddTransient<TenantImpersonationAuthorizer>();
builder.Services.AddTransient<IdentityProviderImpersonationAuthorizer>();
builder.Services.AddTransient<ClaimImpersonationAuthorizer>();
builder.Services.AddTransient<RolesImpersonationAuthorizer>();
builder.Services.AddTransient<GroupsImpersonationAuthorizer>();
builder.Services.AddSingleton<IImpersonationFlow, ImpersonationFlow>();
builder.Services.AddSingleton<NoneSourceIdentifierResolver>();
builder.Services.AddSingleton<ClaimsSourceIdentifierResolver>();
builder.Services.AddSingleton<RouteSourceIdentifierResolver>();

builder.Services.AddHttpClient();

var app = builder.Build();
app.UseRouting();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.UseStaticFiles();

app.Run();
