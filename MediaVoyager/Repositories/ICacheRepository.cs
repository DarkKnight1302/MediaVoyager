using System.Threading.Tasks;

namespace MediaVoyager.Repositories
{
    // Generic cache repository interface for TMDb cached entities
    public interface ICacheRepository<T>
    {
        Task<T> GetAsync(string id);
        Task<T> UpsertAsync(T item);
    }
}
