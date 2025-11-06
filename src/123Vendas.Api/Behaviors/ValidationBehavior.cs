using FluentValidation;
using MediatR;
using _123Vendas.Shared.Common;

namespace _123Vendas.Api.Behaviors;

/// <summary>
/// Pipeline behavior que executa validações FluentValidation antes de processar comandos
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Se não há validadores, continua o pipeline
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        // Executa todas as validações em paralelo
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Coleta todos os erros
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Any())
        {
            var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));
            
            _logger.LogWarning(
                "Validação falhou para {RequestType}: {Errors}",
                typeof(TRequest).Name,
                errors);

            // Se o response é Result<T>, retorna Result.Failure
            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var resultType = typeof(TResponse).GetGenericArguments()[0];
                var failureMethod = typeof(Result<>)
                    .MakeGenericType(resultType)
                    .GetMethod("Failure", new[] { typeof(string) });

                if (failureMethod != null)
                {
                    return (TResponse)failureMethod.Invoke(null, new object[] { errors })!;
                }
            }

            // Fallback: lança exceção
            throw new ValidationException(failures);
        }

        return await next();
    }
}
