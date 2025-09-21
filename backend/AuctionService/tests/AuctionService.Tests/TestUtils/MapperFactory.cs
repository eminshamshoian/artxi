using AutoMapper;
using AuctionService.Api.Mappings;
using Microsoft.Extensions.Logging.Abstractions;

namespace AuctionService.Tests.TestUtils;

public static class MapperFactory
{
    public static IMapper CreateMapper()
    {
        var cfg = new MapperConfiguration(
            c => c.AddProfile<MappingProfiles>(),
            loggerFactory: NullLoggerFactory.Instance);
        cfg.AssertConfigurationIsValid();
        return cfg.CreateMapper();
    }
}
