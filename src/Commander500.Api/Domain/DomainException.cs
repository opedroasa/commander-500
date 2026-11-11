namespace Commander500.Api.Domain;

/// <summary>A business-rule violation (e.g. wrong round state). Mapped to HTTP 409.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>Invalid input. Mapped to HTTP 400.</summary>
public class ValidationException(string message) : DomainException(message);

/// <summary>An external service (Scryfall) is unavailable. Mapped to HTTP 503.</summary>
public class ServiceUnavailableException(string message) : Exception(message);

/// <summary>Entity not found. Mapped to HTTP 404.</summary>
public class NotFoundException(string message) : Exception(message);
