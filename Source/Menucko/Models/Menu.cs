using System.Collections.Generic;

namespace Menucko.Models;

public class Menu
{
    public Soup Soup { get; }
    public IEnumerable<MainCourse> MainCourses { get; }

    public Menu(Soup soup, IEnumerable<MainCourse> mainCourses)
    {
        Soup = soup;
        MainCourses = mainCourses;
    }
}