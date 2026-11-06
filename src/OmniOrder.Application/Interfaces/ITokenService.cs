using OmniOrder.Application.DTOs;

namespace OmniOrder.Application.Interfaces;

public interface ITokenService
{
    TokenResponse GenerateDemoToken(DemoTokenRequest request);
}
