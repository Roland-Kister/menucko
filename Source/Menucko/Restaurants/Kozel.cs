using System.Globalization;
using System.Linq;
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

public class Kozel
{
    private const string MenuUrl = "http://kozeltankpub.sk/obedove-menu/";

    private readonly IHtmlUtil htmlUtil;
    private readonly IStringUtil stringUtil;
    
    public Kozel(IHtmlUtil htmlUtil, IStringUtil stringUtil)
    {
        this.htmlUtil = htmlUtil;
        this.stringUtil = stringUtil;
    }

    [FunctionName(nameof(Kozel))]
    public async Task<IActionResult> GetMenu(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "kozel")]
        HttpRequest req)
    {
        var rawDocument = await htmlUtil.FetchDocument(MenuUrl);
        
        var document = await htmlUtil.ParseDocument(rawDocument);
        
        var menu = ParseMenu(document);
        
        return new OkObjectResult(menu);
    }
    
    private Menu ParseMenu(IParentNode document)
    {
        var menuEl = document.QuerySelector(".product.daily-menu.list.today");
        
        var soup = ParseSoup(menuEl);

        var mainCourseEls = menuEl.QuerySelectorAll(".hlavne .menu-holder");

        var mainCourses = mainCourseEls.Select(ParseMainCourse);

        return new Menu(soup, mainCourses);
    }

    private Soup ParseSoup(IParentNode menuEl)
    {
        var soupEls = menuEl.QuerySelectorAll(".polievky p");
        var soupStrs = soupEls.Select(el => stringUtil.RemoveVolumeInfo(stringUtil.RemoveAllergens(el.InnerHtml.Trim())));

        var soupName = string.Join(" ALEBO ", soupStrs);

        return new Soup(soupName);
    }

    private MainCourse ParseMainCourse(IParentNode mainCourseEl)
    {
        var identifierEl = mainCourseEl.QuerySelector("span:first-of-type");
        var identifier = identifierEl.InnerHtml.Trim();

        var nameEl = mainCourseEl.QuerySelector("p");
        var name = stringUtil.RemoveVolumeInfo(nameEl.InnerHtml);
        name = stringUtil.RemoveAllergens(name);
        
        var priceEl = mainCourseEl.QuerySelector("span:last-of-type");
        var priceStr = stringUtil.RemoveNbsp(priceEl.InnerHtml.Trim().Replace(',', '.'))[..^1];
        var price = double.Parse(priceStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);;

        return new MainCourse(identifier, name, price);
    }
}