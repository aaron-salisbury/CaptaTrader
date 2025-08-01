using CaptaTrader.Business.Modules.UserAccess.DTOs;
using CaptaTrader.Data.Entities;
using Riok.Mapperly.Abstractions;
using RunnethOverStudio.AppToolkit.Modules.UserAccess;

namespace CaptaTrader.Business.Modules.UserAccess;

[Mapper]
internal partial class UserAccessMapper
{
    internal partial TTarget MapToDto<TTarget>(object source);
    internal partial TTarget MapToEntity<TTarget>(object source);

    [MapperIgnoreSource(nameof(EndUser.TradeOrders))]
    private partial EndUserDto MapToDto(EndUser source);
    [MapperIgnoreTarget(nameof(EndUser.TradeOrders))]
    private partial EndUser MapToEntity(EndUserDto source);

    [MapperIgnoreSource(nameof(UserCredential.EndUser))]
    private partial UserCredentials MapToDto(UserCredential source);
    [MapperIgnoreTarget(nameof(UserCredential.EndUser))]
    private partial UserCredential MapToEntity(UserCredentials source);

    [MapperIgnoreSource(nameof(UserConfig.EndUser))]
    private partial UserConfigDto MapToDto(UserConfig source);
    [MapperIgnoreTarget(nameof(UserConfig.EndUser))]
    private partial UserConfig MapToEntity(UserConfigDto source);
}
