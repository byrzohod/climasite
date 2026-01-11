using System.Reflection;
using Mapster;

namespace ClimaSite.Application.Common.Mappings;

public static class MappingConfig
{
    public static TypeAdapterConfig GetConfiguration()
    {
        var config = new TypeAdapterConfig();
        config.Scan(Assembly.GetExecutingAssembly());
        return config;
    }
}
