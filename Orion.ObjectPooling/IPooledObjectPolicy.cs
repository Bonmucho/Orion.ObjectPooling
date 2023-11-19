namespace Orion.ObjectPooling;

public interface IPooledObjectPolicy<T> where T : notnull
{
	Func<T> FactoryFunc { get; }
	Func<CancellationToken, Task<T>> AsyncFactoryFunc { get; }
	Func<T, bool> ReturnFunc { get; }
	CancellationToken CancellationToken { get; }
	int InitialPoolSize { get; }
	int MaximumPoolSize { get; }
}