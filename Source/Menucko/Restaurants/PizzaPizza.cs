using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Menucko.Models;
using Menucko.Util.DateTime;
using Menucko.Util.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Menucko.Restaurants;

public class PizzaPizza
{
    private const string MenuUrl = "https://www.pizza-pizza.sk/menu---terasa";

    private IHtmlUtil htmlUtil;
    private IDateTimeUtil dateTimeUtil;

    public PizzaPizza(IHtmlUtil htmlUtil, IDateTimeUtil dateTimeUtil)
    {
        this.htmlUtil = htmlUtil;
        this.dateTimeUtil = dateTimeUtil;
    }

    [FunctionName(nameof(PizzaPizza))]
    public async Task<IActionResult> GetMenu(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pizza-pizza")]
        HttpRequest req)
    {
        var rawHtml = await htmlUtil.FetchDocument(MenuUrl);

        var document = await htmlUtil.ParseDocument(rawHtml);

        var menu = ParseMenu(document);

        return new OkObjectResult(menu);
    }

    private Menu ParseMenu(IParentNode document)
    {
        var menuSelector = $"#ObedoveMenuu .menuCategory:nth-of-type({dateTimeUtil.GetDayOfWeek()}) .menuItemBox";
        var menuEls = document.QuerySelectorAll(menuSelector);

        if (menuEls.Length == 0)
        {
            return null;
        }

        var soup = ParseSoup(menuEls.First());

        var mainCourses = menuEls.Select(ParseMainCourse);

        return new Menu(soup, mainCourses);
    }

    private static Soup ParseSoup(IParentNode menuEl)
    {
        var nameEl = menuEl.QuerySelector(".menuItemDesc p:first-of-type");
        var name = DeleteNbsp(nameEl.InnerHtml);
        name = DeleteAllergens(name);

        return new Soup(name);
    }

    private static MainCourse ParseMainCourse(IParentNode menuEl)
    {
        var identifierEl = menuEl.QuerySelector(".menuItemName");
        var identifier = DeleteNbsp(identifierEl.InnerHtml);

        var nameEl = menuEl.QuerySelector(".menuItemDesc p:last-of-type");
        var name = DeleteNbsp(nameEl.InnerHtml);
        name = DeleteAllergens(name);

        var priceEl = menuEl.QuerySelector(".menuItemPrice");
        var priceStr = DeleteNbsp(priceEl.InnerHtml).Trim()[..^1];
        var price = double.Parse(priceStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

        return new MainCourse(identifier, name, price);
    }

    private static string DeleteNbsp(string value)
    {
        return Regex.Replace(value, @"&nbsp;", "");
    }
    
    private static string DeleteAllergens(string value)
    {
        return Regex.Replace(value, @" ?\/[\d ,]+\/", "");
    }
}