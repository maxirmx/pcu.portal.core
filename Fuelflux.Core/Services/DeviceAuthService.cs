// Copyright (C) 2025 Maxim [maxirmx] Samsonov (www.sw.consulting)
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

using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.Extensions.Options;
using Fuelflux.Core.Settings;
using Fuelflux.Core.Models;

namespace Fuelflux.Core.Services;

public class DeviceAuthService : IDeviceAuthService
{
    private record DeviceSession(DateTime Expires, PumpController PumpController, User User);
    private readonly ConcurrentDictionary<string, DeviceSession> _sessions = new();
    private readonly DeviceAuthSettings _settings;
    private readonly AppSettings _appSettings;
    private readonly ILogger<DeviceAuthService> _logger;

    public DeviceAuthService(
        IOptions<DeviceAuthSettings> deviceOptions,
        IOptions<AppSettings> appOptions,
        ILogger<DeviceAuthService> logger)
    {
        _settings = deviceOptions.Value;
        _appSettings = appOptions.Value;
        _logger = logger;

        if (string.IsNullOrEmpty(_appSettings.Secret))
        {
            _logger.LogError("JWT secret not configured");
            throw new Exception("JWT secret not configured");
        }
    }

    public string Authorize(PumpController pump, User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(_appSettings.Secret!));

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_settings.SessionMinutes);
        var notBefore = expires <= now ? expires.AddSeconds(-1) : now;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("typ", "device")
            }),
            Expires = expires,
            NotBefore = notBefore,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(descriptor);
        var tokenString = tokenHandler.WriteToken(token);

        _sessions[tokenString] = new DeviceSession(expires, pump, user);

        var tokenPrefix = GetSafeTokenIdentifier(tokenString);
        _logger.LogDebug("Token {tokenPrefix} authorized until {expires}", tokenPrefix, expires);
        return tokenString;
    }

    public DeviceValidationResult? Validate(string token)
    {
        if (!_sessions.TryGetValue(token, out var session))
        {
            return null;
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(_appSettings.Secret!));
        try
        {
            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = false,
                ValidateAudience = false,
                ClockSkew = TimeSpan.Zero
            }, out _);

            if (DateTime.UtcNow >= session.Expires)
            {
                _sessions.TryRemove(token, out _);
                var tokenPrefix = GetSafeTokenIdentifier(token);
                _logger.LogDebug("Token {tokenPrefix} expired", tokenPrefix);
                return null;
            }

            return new DeviceValidationResult(session.PumpController.Uid, session.User.Uid!);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Invalid device token");
            _sessions.TryRemove(token, out _);
            return null;
        }
    }

    public void Deauthorize(string token)
    {
        _sessions.TryRemove(token, out _);

        var tokenPrefix = GetSafeTokenIdentifier(token);
        _logger.LogDebug("Token {tokenPrefix} deauthorized", tokenPrefix);
    }

    public void RemoveExpiredTokens()
    {
        var now = DateTime.UtcNow;

        foreach (var kv in _sessions)
        {
            if (kv.Value.Expires <= now)
            {
                // Try to remove - handle race conditions gracefully
                if (_sessions.TryRemove(kv.Key, out var removedSession) && removedSession.Expires <= now)
                {
                    var tokenPrefix = GetSafeTokenIdentifier(kv.Key);
                    _logger.LogDebug("Token {tokenPrefix} expired and removed", tokenPrefix);
                }
            }
        }
    }

    private static string GetSafeTokenIdentifier(string token)
    {
        return token.Length > 8 ? $"{token[..8]}..." : "short_token";
    }
}
