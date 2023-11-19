using System.Collections.Concurrent;

namespace Orion.ObjectPooling;

public class ObjectPool<T> : IObjectPool<T> where T : class, new()
{
	private const int MaximumAllowedPoolSize = 1048576;
	
	public static IObjectPool<T> Shared { get; private set; } = new ObjectPool<T>();

	public IPooledObjectPolicy<T> Policy { get; }
	public int Count => _items.Count + (_fastItem is null ? 0 : 1);
	public bool IsEmpty => _fastItem is null && _items.IsEmpty;
	public bool IsDisposed { get; private set; }

	private readonly ConcurrentQueue<T> _items;
	
	private T? _fastItem;
	private int _itemCount;

	public ObjectPool(IPooledObjectPolicy<T>? policy = null)
		: this(policy, new(), null, 0) { }
	
	private ObjectPool(IPooledObjectPolicy<T>? policy, ConcurrentQueue<T> items, T? fastItem, int itemCount)
	{
		Policy = policy ?? PooledObjectPolicy<T>.Default;
		_items = items;
		_fastItem = fastItem;
		_itemCount = itemCount;

		ArgumentOutOfRangeException.ThrowIfNegative(Policy.InitialPoolSize);
		ArgumentOutOfRangeException.ThrowIfNegative(Policy.MaximumPoolSize);
		ArgumentOutOfRangeException.ThrowIfLessThan(Policy.MaximumPoolSize, Policy.InitialPoolSize);

		if (Policy.InitialPoolSize == 0) return;
		_fastItem = Policy.FactoryFunc();

		if (Policy.InitialPoolSize == 1) return;
		for (var i = 1; i < Policy.InitialPoolSize; i++) Return(Rent());
	}

	public static IObjectPool<T> AssignSharedPolicy(IPooledObjectPolicy<T>? policy = null)
	{
		var pool = (ObjectPool<T>)Shared;
		Shared = new ObjectPool<T>(policy, pool._items, pool._fastItem, pool._itemCount);
		return Shared;
	}
	
	public T Rent() => RentCore(out var item) ? item! : Policy.FactoryFunc();

	public async Task<T> RentAsync(CancellationToken cancellationToken = default) =>
		RentCore(out var item) ? item! : await Policy.AsyncFactoryFunc(cancellationToken);

	private bool RentCore(out T? item)
	{
		if (IsDisposed) throw new ObjectDisposedException(nameof(ObjectPool<T>));
		item = _fastItem;

		if (item is not null && Interlocked.CompareExchange(ref _fastItem, null, item) == item) return true;
		if (!_items.TryDequeue(out item)) return false;

		Interlocked.Decrement(ref _itemCount);
		return true;
	}

	public bool Return(T obj)
	{
		if (ReturnCore(obj)) return true;
		if (obj is IDisposable d) d.Dispose();
		return false;
	}
	
	public async Task<bool> ReturnAsync(T obj)
	{
		if (ReturnCore(obj)) return true;
		if (obj is IAsyncDisposable d) await d.DisposeAsync();
		return false;
	}
	
	private bool ReturnCore(T obj)
	{
		if (IsDisposed) throw new ObjectDisposedException(nameof(ObjectPool<T>));
		ArgumentNullException.ThrowIfNull(obj);
		
		if (!Policy.ReturnFunc(obj)) return false;

		if (_fastItem is null && Interlocked.CompareExchange(ref _fastItem, obj, null) is null) return true;
		
		if (Interlocked.Increment(ref _itemCount) < Policy.MaximumPoolSize)
		{
			_items.Enqueue(obj);
			return true;
		}
		
		Interlocked.Decrement(ref _itemCount);
		return false;
	}

	public void Clear()
	{
		if (IsDisposed) throw new ObjectDisposedException(nameof(ObjectPool<T>));

		while (_items.TryDequeue(out var item))
		{
			if (item is IDisposable d1) d1.Dispose();
			Interlocked.Decrement(ref _itemCount);
		}

		var fastItem = Interlocked.Exchange(ref _fastItem, null);
		if (fastItem is IDisposable d0) d0.Dispose();
	}

	public async Task ClearAsync()
	{
		if (IsDisposed) throw new ObjectDisposedException(nameof(ObjectPool<T>));

		while (_items.TryDequeue(out var item))
		{
			switch (item)
			{
				case IAsyncDisposable ad:
					await ad.DisposeAsync();
					break;
				case IDisposable d:
					d.Dispose();
					break;
			}
			
			Interlocked.Decrement(ref _itemCount);
		}

		var fastItem = Interlocked.Exchange(ref _fastItem, null);
		
		switch (fastItem)
		{
			case IAsyncDisposable ad:
				await ad.DisposeAsync();
				break;
			case IDisposable d:
				d.Dispose();
				break;
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (IsDisposed) return;
		Clear();
		IsDisposed = true;
	}
	
	public async ValueTask DisposeAsync()
	{
		await DisposeAsync(true);
		GC.SuppressFinalize(this);
	}

	protected virtual async Task DisposeAsync(bool disposing)
	{
		if (IsDisposed) return;
		await ClearAsync();
		IsDisposed = true;
	}
}