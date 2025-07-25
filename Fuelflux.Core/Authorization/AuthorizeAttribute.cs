﻿// Copyright (C) 2025 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of Fuelflux Core application
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using Fuelflux.Core.RestModels;

namespace Fuelflux.Core.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public AuthorizationType AuthorizationType { get; set; }

    public AuthorizeAttribute(AuthorizationType authorizationType)
    {
        AuthorizationType = authorizationType;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // skip authorization if action is decorated with [AllowAnonymous] attribute
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowAnonymous)
            return;

        bool isAuthorized = AuthorizationType switch
        {
            AuthorizationType.User => CheckUserAuthentication(context),
            AuthorizationType.Device => CheckDeviceAuthentication(context),
            _ => false
        };

        if (!isAuthorized)
        {
            const string errorMessage = "Необходимо войти в систему.";
            var logger = context.HttpContext.RequestServices.GetService(typeof(ILogger<AuthorizeAttribute>)) as ILogger<AuthorizeAttribute>;
            logger?.LogWarning(errorMessage);
            context.Result = new JsonResult(new ErrMessage { Msg = errorMessage }) { StatusCode = StatusCodes.Status401Unauthorized };
        }
    }

    private static bool CheckUserAuthentication(AuthorizationFilterContext context)
    {
        context.HttpContext.Items.TryGetValue("UserId", out var userIdObj);
        return userIdObj is int;
    }

    private static bool CheckDeviceAuthentication(AuthorizationFilterContext context)
    {
        context.HttpContext.Items.TryGetValue("UserUid", out var userUidObj);
        return userUidObj is string userUid && !string.IsNullOrEmpty(userUid);
    }
}
