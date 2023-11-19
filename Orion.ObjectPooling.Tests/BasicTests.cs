namespace Orion.ObjectPooling.Tests;

public class BasicTests
{
	[Fact]
	public void AssignSharedPolicy()
	{
		Assert.Equal(0, ObjectPool<Person>.Shared.Policy.InitialPoolSize);
		ObjectPool<Person>.AssignSharedPolicy(new PooledObjectPolicy<Person> { InitialPoolSize = 4 });
		Assert.Equal(4, ObjectPool<Person>.Shared.Policy.InitialPoolSize);
		
		ObjectPool<Person>.AssignSharedPolicy(PooledObjectPolicy<Person>.Default).Clear();
	}
	
	[Fact]
	public void Count()
	{
		Assert.Equal(0, ObjectPool<Person>.Shared.Count);
		
		ObjectPool<Person>.Shared.Return(new());
		Assert.Equal(1, ObjectPool<Person>.Shared.Count);
		
		ObjectPool<Person>.Shared.Return(new());
		Assert.Equal(2, ObjectPool<Person>.Shared.Count);
		
		ObjectPool<Person>.Shared.Rent();
		Assert.Equal(1, ObjectPool<Person>.Shared.Count);
		
		ObjectPool<Person>.AssignSharedPolicy(new PooledObjectPolicy<Person>());
		Assert.Equal(1, ObjectPool<Person>.Shared.Count);
		
		ObjectPool<Person>.AssignSharedPolicy(PooledObjectPolicy<Person>.Default).Clear();
	}

	[Fact]
	public void ReturnRent()
	{
		ObjectPool<Person> pool = new();

		var person1 = pool.Rent();
		pool.Return(person1);
		var person2 = pool.Rent();
		
		Assert.Same(person1, person2);
	}
}