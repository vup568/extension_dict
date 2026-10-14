using Domain.Entities;

namespace Application.Conjugation.Ports;

public interface IConjugationDatasetRepository
{
    Task<ConjugationDataset?> GetAsync(string version, CancellationToken ct = default);
}
