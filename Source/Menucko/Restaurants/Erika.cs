using System.Collections.Generic;
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

public class Erika
{
    private const string MenuUrl = "https://www.bowlingerika.sk/";

    private IHtmlUtil htmlUtil;
    private IDateTimeUtil dateTimeUtil;
    
    public Erika(IHtmlUtil htmlUtil, IDateTimeUtil dateTimeUtil)
    {
        this.htmlUtil = htmlUtil;
        this.dateTimeUtil = dateTimeUtil;
    }
    
    [FunctionName(nameof(Erika))]
    public async Task<IActionResult> GetMenu(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "erika")]
        HttpRequest req)
    {
        var rawHtml = await htmlUtil.FetchDocument(MenuUrl);
        
        var document = await htmlUtil.ParseDocument(rawHtml);
        
        var menu = ParseMenu(document);
        
        return new OkObjectResult(menu);
    }
    
    private Menu ParseMenu(IParentNode document)
    {
        var menuSelector =
            $"#obedove-menu .eael-tab-content-item:nth-of-type({dateTimeUtil.GetDayOfWeek()}) .elementor-widget-wd_menu_price";
        var menuEls = document.QuerySelectorAll(menuSelector);

        if (menuEls.Length == 0)
        {
            return null;
        }

        var soup = ParseSoup(menuEls.First());

        var mainCourses = menuEls.Skip(1).Select(ParseMainCourse).Where(mainCourse => mainCourse is not null);

        return new Menu(soup, mainCourses);
    }
    
    private static Soup ParseSoup(IParentNode menuEl)
    {
        var nameEl = menuEl.QuerySelector(".menu-price-title span");
        var name = nameEl.InnerHtml.Trim();
        
        name = Regex.Replace(name, @"^[\d,.]+\w ", "");

        return new Soup(name);
    }
    
    private static MainCourse ParseMainCourse(IParentNode menuEl)
    {
        var nameEl = menuEl.QuerySelector(".menu-price-title span");

        if (nameEl is null)
        {
            return null;
        }
        
        var name = nameEl.InnerHtml.Trim();
        
        var match = Regex.Match(name, @"(Menu \d\)) ?", RegexOptions.IgnoreCase);
        var identifier = match.Groups[1].Value[..^1];

        name = Regex.Replace(name, @"Menu \d\) ?", "", RegexOptions.IgnoreCase);

        var priceEl = menuEl.QuerySelector(".menu-price-price");
        var priceStr = priceEl.InnerHtml.Trim().Replace(',', '.')[..^2];
        
        var price = double.Parse(priceStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);

        return new MainCourse(identifier, name, price);
    }
}