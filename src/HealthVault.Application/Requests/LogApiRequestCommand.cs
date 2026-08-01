using HealthVault.Domain.Entities;
using HealthVault.Persistence.Data;
using MediatR;

namespace HealthVault.Application.Requests;

/// <summary>
/// Command that records a completed API request.
/// </summary>
public record LogApiRequestCommand : IRequest
{
    public string RequestApi { get; init; } = string.Empty;
    public DateTime RequestDateTime { get; init; }
    public bool WasSuccessful { get; init; }
    public string RequestedBy { get; init; } = string.Empty;
}

/// <summary>
/// Handles a <see cref="LogApiRequestCommand"/>.
/// </summary>
public class LogApiRequestHandler : IRequestHandler<LogApiRequestCommand>
{
    private readonly AppDbContext _context;

    public LogApiRequestHandler(AppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(
        LogApiRequestCommand request,
        CancellationToken cancellationToken)
    {
        _context.Requests.Add(new ApiRequest
        {
            RequestApi = request.RequestApi,
            RequestDateTime = request.RequestDateTime,
            WasSuccessful = request.WasSuccessful,
            RequestedBy = request.RequestedBy
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}
