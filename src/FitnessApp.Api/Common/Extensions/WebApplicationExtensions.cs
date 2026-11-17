namespace FitnessApp.Api.Common.Extensions;

/// <summary>The HTTP pipeline in one place. Order matters, so each step says why it is where it is.</summary>
public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // 1. First, so it catches exceptions from everything below and turns them into ProblemDetails.
        app.UseExceptionHandler();
        // 2. Empty error responses (404 for an unknown route, 405, …) also get a ProblemDetails body.
        app.UseStatusCodePages();

        // 3. HTTPS only in production; locally and in tests plain HTTP is fine.
        if (app.Environment.IsProduction())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        // 4. Cheap protections on every response.
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            await next();
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // 5. CORS before anything that can reject the request, so the browser sees the CORS headers.
        app.UseCors(ServiceCollectionExtensions.FrontendCorsPolicy);

        // 6. Who is calling (reads the JWT), then are they allowed (fallback policy, roles).
        app.UseAuthentication();
        app.UseAuthorization();

        // 7. After authentication, so limits can later be partitioned per user as well as per IP.
        app.UseRateLimiter();

        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapHealthChecks("/health").AllowAnonymous();
        return app;
    }
}
