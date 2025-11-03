using System;
using System.Collections.Generic;
// Alias rõ ràng tới entity User
using AppUser = TRManager.Api.Data.Entities.User;

namespace TRManager.Api.Features.Auth
{
    public interface IJwtTokenGenerator
    {
        (string token, DateTime expiresAt) Generate(AppUser user, IEnumerable<string> roles);
    }
}
