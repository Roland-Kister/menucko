namespace Menucko.Models;

public class MainCourse
{
    public string Identifier { get; }
    public string Name { get; }
    public double Price { get; }
    public Soup SpecialSoup { get; }
    
    public MainCourse(string identifier, string name, double price, Soup specialSoup = null)
    {
        Identifier = identifier;
        Name = name;
        Price = price;
        SpecialSoup = specialSoup;
    }
}