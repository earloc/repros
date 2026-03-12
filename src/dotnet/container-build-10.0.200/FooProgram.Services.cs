namespace FooNamespace;

internal static partial class FooProgramExtensions
{
    extension(IServiceCollection services)
    {
        internal IServiceCollection AddServices()
        {
            // Add services to the container.
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            services.AddOpenApi();

            return services;
        }
    }
}