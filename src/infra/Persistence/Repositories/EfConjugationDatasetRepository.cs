using Application.Conjugation.Ports;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public sealed class EfConjugationDatasetRepository(AppDbContext context) : IConjugationDatasetRepository
{
    public Task<ConjugationDataset?> GetAsync(string version, CancellationToken ct = default) =>
        context.ConjugationDatasets.AsNoTracking().SingleOrDefaultAsync(x => x.Version == version, ct);
}
