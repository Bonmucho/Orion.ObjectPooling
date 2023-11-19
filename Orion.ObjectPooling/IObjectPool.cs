namespace Orion.ObjectPooling;

public interface IObjectPool<T> : IAsyncDisposable, IDisposable where T : class, new()
{
	IPooledObjectPolicy<T> Policy { get; }
	int Count { get; }
	bool IsEmpty { get; }
	bool IsDisposed { get; }

	T Rent();
	Task<T> RentAsync(CancellationToken cancellationToken = default);
	bool Return(T obj);
	Task<bool> ReturnAsync(T obj);
	void Clear();
	Task ClearAsync();
}