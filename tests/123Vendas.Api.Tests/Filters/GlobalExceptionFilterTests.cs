using _123Vendas.Api.Filters;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace _123Vendas.Api.Tests.Filters;

public class GlobalExceptionFilterTests
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly GlobalExceptionFilter _filter;
    
    public GlobalExceptionFilterTests()
    {
        _logger = Substitute.For<ILogger<GlobalExceptionFilter>>();
        _environment = Substitute.For<IWebHostEnvironment>();
        _filter = new GlobalExceptionFilter(_logger, _environment);
    }
    
    [Fact]
    public void OnException_DbUpdateException_ShouldReturn500WithPersistenceError()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new DbUpdateException("Database error");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        context.ExceptionHandled.Should().BeTrue();
        
        var result = context.Result as ObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(500);
        
        var problemDetails = result.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Erro de persistência");
        problemDetails.Detail.Should().Be("Ocorreu um erro ao salvar dados no banco de dados");
        problemDetails.Status.Should().Be(500);
        problemDetails.Extensions.Should().ContainKey("traceId");
    }
    
    [Fact]
    public void OnException_TaskCanceledException_ShouldReturn499()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new TaskCanceledException("Task was canceled");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        context.ExceptionHandled.Should().BeTrue();
        
        var result = context.Result as ObjectResult;
        result.Should().NotBeNull();
        result!.StatusCode.Should().Be(499);
        
        var problemDetails = result.Value as ProblemDetails;
        problemDetails.Should().NotBeNull();
        problemDetails!.Title.Should().Be("Operação cancelada");
        problemDetails.Detail.Should().Be("A requisição foi cancelada pelo cliente");
        problemDetails.Status.Should().Be(499);
    }
    
    [Fact]
    public void OnException_OperationCanceledException_ShouldReturn499()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new OperationCanceledException("Operation was canceled");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        context.ExceptionHandled.Should().BeTrue();
        
        var result = context.Result as ObjectResult;
        result!.StatusCode.Should().Be(499);
        
        var problemDetails = result.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Operação cancelada");
    }
    
    [Fact]
    public void OnException_HttpRequestException_ShouldReturn502()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new HttpRequestException("HTTP request failed");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        context.ExceptionHandled.Should().BeTrue();
        
        var result = context.Result as ObjectResult;
        result!.StatusCode.Should().Be(502);
        
        var problemDetails = result.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Erro de comunicação externa");
        problemDetails.Detail.Should().Be("Falha ao comunicar com serviço externo");
        problemDetails.Status.Should().Be(502);
    }
    
    [Fact]
    public void OnException_TimeoutException_ShouldReturn504()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new TimeoutException("Operation timed out");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        context.ExceptionHandled.Should().BeTrue();
        
        var result = context.Result as ObjectResult;
        result!.StatusCode.Should().Be(504);
        
        var problemDetails = result.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Timeout");
        problemDetails.Detail.Should().Be("A operação excedeu o tempo limite");
        problemDetails.Status.Should().Be(504);
    }
    
    [Fact]
    public void OnException_GenericException_ShouldReturn500()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new InvalidOperationException("Something went wrong");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        context.ExceptionHandled.Should().BeTrue();
        
        var result = context.Result as ObjectResult;
        result!.StatusCode.Should().Be(500);
        
        var problemDetails = result.Value as ProblemDetails;
        problemDetails!.Title.Should().Be("Erro interno");
        problemDetails.Detail.Should().Be("Ocorreu um erro inesperado ao processar a requisição");
        problemDetails.Status.Should().Be(500);
    }
    
    [Fact]
    public void OnException_ShouldIncludeTraceIdInAllResponses()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new Exception("Test exception");
        var context = CreateExceptionContext(exception);
        var expectedTraceId = context.HttpContext.TraceIdentifier;
        
        // Act
        _filter.OnException(context);
        
        // Assert
        var result = context.Result as ObjectResult;
        var problemDetails = result!.Value as ProblemDetails;
        
        problemDetails!.Extensions.Should().ContainKey("traceId");
        problemDetails.Extensions["traceId"].Should().Be(expectedTraceId);
    }
    
    [Fact]
    public void OnException_Development_ShouldIncludeStackTrace()
    {
        // Arrange
        _environment.EnvironmentName.Returns(Environments.Development);
        
        // Create exception with stack trace by throwing it
        Exception exception;
        try
        {
            throw new Exception("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        var result = context.Result as ObjectResult;
        var problemDetails = result!.Value as ProblemDetails;
        
        problemDetails!.Extensions.Should().ContainKey("stackTrace");
        problemDetails.Extensions["stackTrace"].Should().NotBeNull();
    }
    
    [Fact]
    public void OnException_Production_ShouldNotIncludeStackTrace()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new Exception("Test exception");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        var result = context.Result as ObjectResult;
        var problemDetails = result!.Value as ProblemDetails;
        
        problemDetails!.Extensions.Should().NotContainKey("stackTrace");
    }
    
    [Fact]
    public void OnException_Development_ShouldIncludeActualExceptionMessage()
    {
        // Arrange
        _environment.EnvironmentName.Returns(Environments.Development);
        var exceptionMessage = "Detailed error message for debugging";
        var exception = new Exception(exceptionMessage);
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        var result = context.Result as ObjectResult;
        var problemDetails = result!.Value as ProblemDetails;
        
        problemDetails!.Detail.Should().Be(exceptionMessage);
    }
    
    [Fact]
    public void OnException_Production_ShouldUseGenericMessage()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exceptionMessage = "Detailed error message that should not be exposed";
        var exception = new Exception(exceptionMessage);
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        var result = context.Result as ObjectResult;
        var problemDetails = result!.Value as ProblemDetails;
        
        problemDetails!.Detail.Should().NotBe(exceptionMessage);
        problemDetails.Detail.Should().Be("Ocorreu um erro inesperado ao processar a requisição");
    }
    
    [Fact]
    public void OnException_ShouldLogErrorWithContextualInformation()
    {
        // Arrange
        _environment.EnvironmentName.Returns("Production");
        var exception = new Exception("Test exception");
        var context = CreateExceptionContext(exception);
        
        // Act
        _filter.OnException(context);
        
        // Assert
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(exception.GetType().Name)),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
    
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new GlobalExceptionFilter(null!, _environment);
        
        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }
    
    [Fact]
    public void Constructor_WithNullEnvironment_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new GlobalExceptionFilter(_logger, null!);
        
        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("environment");
    }
    
    private ExceptionContext CreateExceptionContext(Exception exception)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = Guid.NewGuid().ToString();
        httpContext.Request.Path = "/api/v1/vendas";
        
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());
        
        return new ExceptionContext(actionContext, new List<IFilterMetadata>())
        {
            Exception = exception
        };
    }
}
