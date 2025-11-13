using _123Vendas.Api.Middleware;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;

namespace _123Vendas.Api.Tests.Middleware;

public class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DeveAdicionarHeadersDeSeguranca()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        
        var middleware = new SecurityHeadersMiddleware(
            next: (innerContext) => Task.CompletedTask,
            environment: environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var headers = context.Response.Headers;
        
        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", headers["X-Frame-Options"]);
        Assert.Equal("1; mode=block", headers["X-XSS-Protection"]);
        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"]);
        Assert.Contains("geolocation=()", headers["Permissions-Policy"].ToString());
        Assert.Contains("default-src 'none'", headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_EmProducao_DeveAdicionarHSTS()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        
        var middleware = new SecurityHeadersMiddleware(
            next: (innerContext) => Task.CompletedTask,
            environment: environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains("max-age=31536000", context.Response.Headers["Strict-Transport-Security"].ToString());
        Assert.Contains("includeSubDomains", context.Response.Headers["Strict-Transport-Security"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_EmDevelopment_NaoDeveAdicionarHSTS()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        var middleware = new SecurityHeadersMiddleware(
            next: (innerContext) => Task.CompletedTask,
            environment: environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Strict-Transport-Security"));
    }

    [Fact]
    public async Task InvokeAsync_EmDevelopment_DevePermitirSwagger()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Development");
        
        var middleware = new SecurityHeadersMiddleware(
            next: (innerContext) => Task.CompletedTask,
            environment: environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var csp = context.Response.Headers["Content-Security-Policy"].ToString();
        Assert.Contains("unsafe-inline", csp); // Swagger precisa
        Assert.Contains("unsafe-eval", csp);   // Swagger precisa
    }

    [Fact]
    public async Task InvokeAsync_DeveRemoverHeadersQueExpoemInformacoes()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Headers["Server"] = "Kestrel";
        context.Response.Headers["X-Powered-By"] = "ASP.NET";
        context.Response.Headers["X-AspNet-Version"] = "9.0";
        
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        
        var middleware = new SecurityHeadersMiddleware(
            next: (innerContext) => Task.CompletedTask,
            environment: environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(context.Response.Headers.ContainsKey("Server"));
        Assert.False(context.Response.Headers.ContainsKey("X-Powered-By"));
        Assert.False(context.Response.Headers.ContainsKey("X-AspNet-Version"));
    }

    [Fact]
    public async Task InvokeAsync_DeveChamarProximoMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var environmentMock = new Mock<IWebHostEnvironment>();
        environmentMock.Setup(e => e.EnvironmentName).Returns("Production");
        
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(
            next: (innerContext) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            environment: environmentMock.Object
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }
}
