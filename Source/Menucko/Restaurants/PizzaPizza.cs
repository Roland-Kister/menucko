using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Menucko.Models;
using Menucko.Util.Date;
using Menucko.Util.Html;
using Menucko.Util.StringUtil;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace Menucko.Restaurants;

public class PizzaPizza
{
    private const string MenuUrl = "https://www.pizza-pizza.sk/menu---terasa";

    private readonly IHtmlUtil htmlUtil;
    private readonly IDateUtil dateUtil;
    private readonly IStringUtil stringUtil;

    public PizzaPizza(IHtmlUtil htmlUtil, IDateUtil dateUtil, IStringUtil stringUtil)
    {
        this.htmlUtil = htmlUtil;
        this.dateUtil = dateUtil;
        this.stringUtil = stringUtil;
    }

    [FunctionName(nameof(PizzaPizza))]
    public async Task<IActionResult> GetMenu(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "pizza-pizza")]
        HttpRequest req)
    {
        var rawDocument = await htmlUtil.FetchDocument(MenuUrl);

        var document = await htmlUtil.ParseDocument(rawDocument);

        var menu = ParseMenu(document);

        return new OkObjectResult(menu);
    }

    private Menu ParseMenu(IParentNode document)
    {
        var menuSelector = $"#ObedoveMenuu .menuCategory:nth-of-type({dateUtil.GetDayOfWeek()}) .menuItemBox";
        var menuEls = document.QuerySelectorAll(menuSelector);

        if (menuEls.Length == 0)
        {
            return null;
        }

        var soup = ParseSoup(menuEls.First());

        var mainCourses = menuEls.Select(ParseMainCourse);

        return new Menu(soup, mainCourses);
    }

    private Soup ParseSoup(IParentNode menuEl)
    {
        var nameEl = menuEl.QuerySelector(".menuItemDesc p:first-of-type");
        var name = stringUtil.RemoveNbsp(nameEl.InnerHtml);
        name = stringUtil.RemoveAllergens(name);

        return new Soup(name);
    }

    private MainCourse ParseMainCourse(IParentNode menuEl)
    {
        var identifierEl = menuEl.QuerySelector(".menuItemName");
        var identifier = stringUtil.RemoveNbsp(identifierEl.InnerHtml);

        var nameEl = menuEl.QuerySelector(".menuItemDesc p:last-of-type");
        var name = stringUtil.RemoveNbsp(nameEl.InnerHtml);
        name = stringUtil.RemoveAllergens(name);
        name = stringUtil.RemoveVolumeInfo(name);

        var priceEl = menuEl.QuerySelector(".menuItemPrice");
        var priceStr = stringUtil.RemoveNbsp(priceEl.InnerHtml).Trim()[..^1];
        var price = double.Parse(priceStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

        return new MainCourse(identifier, name, price);
    }
}