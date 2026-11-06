namespace PosWebApp.Infrastructure.Exceptions;

/// <summary>
/// Base exception for all failures originating in the Backend_Service layer.
/// All <see cref="System.Net.Http.HttpRequestException"/> and transport-level exceptions
/// from <c>AsmxServiceClient</c> are wrapped in this type or one of its subclasses.
/// </summary>
public class ServiceException : Exception
{
    public ServiceException() { }

    public ServiceException(string message)
        : base(message) { }

    public ServiceException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when authentication with the Backend_Service fails (invalid credentials,
/// locked account, or unexpected auth response).
/// </summary>
public class AuthenticationException : ServiceException
{
    public AuthenticationException() { }

    public AuthenticationException(string message)
        : base(message) { }

    public AuthenticationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when the current cart state is invalid for the requested operation
/// (e.g., checkout attempted on an empty cart, invalid quantity, discount out of range).
/// </summary>
public class CartValidationException : ServiceException
{
    public CartValidationException() { }

    public CartValidationException(string message)
        : base(message) { }

    public CartValidationException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when sale creation or voiding fails on the Backend_Service side
/// (SOAP fault, business rule violation, or unexpected response).
/// </summary>
public class SaleProcessingException : ServiceException
{
    public SaleProcessingException() { }

    public SaleProcessingException(string message)
        : base(message) { }

    public SaleProcessingException(string message, Exception innerException)
        : base(message, innerException) { }
}
