using System.Threading.Tasks.Dataflow;
using BenchmarkDotNet.Attributes;

namespace Orion.ObjectPooling.Benchmarks;

[MemoryDiagnoser]
public class Benchmarks
{
	[Params(100, 1000, 1000000)]
	public int ObjectCount { get; set; }
	
	[Benchmark]
	public async Task DataflowWithoutPool()
	{
		ActionBlock<Person> actionBlock = new(p =>
		{
			var t = p.Height;
		});

		for (var i = 0; i < ObjectCount; i++) await actionBlock.SendAsync(new());
		actionBlock.Complete();
		await actionBlock.Completion;
	}
	
	[Benchmark]
	public async Task DataflowWithPool()
	{
		await ObjectPool<Person>.Shared.ClearAsync();
		ObjectPool<Person>.RecreateShared(new PooledObjectPolicy<Person>()
			{ InitialPoolSize = 32, MaximumPoolSize = 8192, ReturnFunc = _ => true });
		
		ActionBlock<Person> actionBlock = new(async p =>
		{
			var t = p.Height;
			await ObjectPool<Person>.Shared.ReturnAsync(p);
		});

		for (var i = 0; i < ObjectCount; i++)
			await actionBlock.SendAsync(await ObjectPool<Person>.Shared.RentAsync());
		
		actionBlock.Complete();
		await actionBlock.Completion;
	}
}