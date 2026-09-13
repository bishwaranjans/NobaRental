namespace NobaRental.Backend.WebApi.Startups;

internal static class SwaggerStartup
{
    public static void ConfigureSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.CustomSchemaIds(x => x.ToString());

            var securityScheme = new Microsoft.OpenApi.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = Microsoft.OpenApi.ParameterLocation.Header,
                Type = Microsoft.OpenApi.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            var schemeReference = new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer");
            options.AddSecurityRequirement(_ => new Microsoft.OpenApi.OpenApiSecurityRequirement
            {
                [schemeReference] = []
            });
        });
    }
}
