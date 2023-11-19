namespace Orion.ObjectPooling.Benchmarks;

public record Person(string Name, string Name1, string Name2, string Name3, string Name4,
	int Age, double Height, double Weight)
{
	public Person() : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, 0, 0) { }
}