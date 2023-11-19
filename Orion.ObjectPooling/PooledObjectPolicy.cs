namespace Orion.ObjectPooling;

public class PooledObjectPolicy<T> : IPooledObjectPolicy<T> where T : class, new()
{
	public static IPooledObjectPolicy<T> Default => new PooledObjectPolicy<T>();
	
	public Func<T> FactoryFunc
	{
		get => _factoryFunc;
		init
		{
			ArgumentNullException.ThrowIfNull(value);
			if (_isDefaultFactoryFunc) _asyncFactoryFunc = _ => Task.FromResult(value());
			_isDefaultFactoryFunc = false;
			_factoryFunc = value;
		}
	}

	public Func<CancellationToken, Task<T>> AsyncFactoryFunc
	{
		get => _asyncFactoryFunc;
		init
		{
			ArgumentNullException.ThrowIfNull(value);

			if (_isDefaultFactoryFunc)
				_factoryFunc = () =>
				{
					var task = value(CancellationToken);
					task.RunSynchronously();
					return task.Result;
				};
			
			_isDefaultFactoryFunc = false;
			_asyncFactoryFunc = value;
		}
	}

	public Func<T, bool> ReturnFunc { get; init; } = o =>
	{
		if (o is IResettable r) return r.TryReset();
		return true;
	};
	
	public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

	public int InitialPoolSize
	{
		get => _initialPoolSize;
		init
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			_initialPoolSize = value;
		}
	}
	
	public int MaximumPoolSize
	{
		get => _maximumPoolSize;
		init
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			_maximumPoolSize = value;
		}
	}

	private readonly Func<T> _factoryFunc = () => new();
	private readonly Func<CancellationToken, Task<T>> _asyncFactoryFunc = _ => Task.FromResult(new T());
	private readonly int _initialPoolSize = 0;
	private readonly int _maximumPoolSize = Environment.ProcessorCount * 2;
	private readonly bool _isDefaultFactoryFunc = true;
}