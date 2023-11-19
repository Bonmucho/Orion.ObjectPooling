namespace Orion.ObjectPooling.Tests;

public record Person(string Name, int Age, double Height, double Weight)
{
	public Person() : this(string.Empty, 0, 0, 0) { }
}