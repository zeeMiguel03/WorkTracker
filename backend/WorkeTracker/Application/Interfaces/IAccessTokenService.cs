using Application.DTOs.Auth;
using Domain.Entities;

namespace Application.Interfaces
{
    public interface IAccessTokenService
    {
        AccessTokenDTO Create(User user);
    }
}
