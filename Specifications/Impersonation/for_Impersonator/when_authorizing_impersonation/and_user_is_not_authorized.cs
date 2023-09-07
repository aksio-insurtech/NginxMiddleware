// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aksio.IngressMiddleware.Impersonation.for_Impersonator.when_authorizing_impersonation;

public class and_user_is_not_authorized : given.a_principal
{
    IActionResult result;

    void Establish() =>
        authorizer.Setup(_ => _.IsAuthorized(impersonator.ControllerContext.HttpContext.Request, IsAny<ClientPrincipal>()))
            .ReturnsAsync(false);

    async Task Because() => result = await impersonator.Authorize();

    [Fact]
    void should_return_forbidden() => ((StatusCodeResult)result).StatusCode.ShouldEqual(StatusCodes.Status403Forbidden);
}