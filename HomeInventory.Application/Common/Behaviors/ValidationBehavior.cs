using FluentValidation;
using HomeInventory.Application.Common.Results;
using MediatR;

namespace HomeInventory.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs the FluentValidation validators registered for a request
/// before it reaches its handler. On failure it returns a failed <see cref="Result"/> (when the
/// response is a Result type) instead of throwing, keeping validation in the expected-flow path.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
        var failures = results.SelectMany(r => r.Errors).Where(f => f is not null).ToList();

        if (failures.Count == 0)
        {
            return await next(cancellationToken);
        }

        var error = Error.Validation(
            "Validation",
            string.Join(" ", failures.Select(f => f.ErrorMessage)));

        if (TryCreateFailureResult(error, out var failureResult))
        {
            return failureResult;
        }

        throw new ValidationException(failures);
    }

    private static bool TryCreateFailureResult(Error error, out TResponse response)
    {
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            response = (TResponse)(object)Result.Failure(error);
            return true;
        }

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var valueType = responseType.GetGenericArguments()[0];
            var failureMethod = typeof(Result)
                .GetMethods()
                .First(m => m is { Name: nameof(Result.Failure), IsGenericMethod: true })
                .MakeGenericMethod(valueType);

            response = (TResponse)failureMethod.Invoke(null, [error])!;
            return true;
        }

        response = default!;
        return false;
    }
}
