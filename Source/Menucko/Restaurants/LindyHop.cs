using AngleSharp.Dom;
using Menucko.Models;
using Menucko.Util.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Text;
using Menucko.Util.StringUtil;
using SixLabors.ImageSharp.Drawing.Processing;
using Tesseract;

namespace Menucko;

public class LindyHop
{
    private const string MenuUrl = "http://www.lindyhop.sk/";

    private readonly IHtmlUtil htmlUtil;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IStringUtil stringUtil;

    public LindyHop(IHtmlUtil htmlUtil, IHttpClientFactory httpClientFactory, IStringUtil stringUtil)
    {
        this.htmlUtil = htmlUtil;
        this.httpClientFactory = httpClientFactory;
        this.stringUtil = stringUtil;
    }

    [FunctionName(nameof(LindyHop))]
    public async Task<IActionResult> GetMenu(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "lindy-hop")]
        HttpRequest req, ExecutionContext context)
    {
        var rawDocument = await htmlUtil.FetchDocument(MenuUrl);

        var document = await htmlUtil.ParseDocument(rawDocument);

        var imageSrc = ParseMenuImageSrc(document);

        var imageBytes = await FetchImage(imageSrc);

        imageBytes = await OptimizeImage(imageBytes);

        var menuString = ParseTextFromImage(context, imageBytes);

        var menu = ParseMenu(menuString);

        return new OkObjectResult(menu);
    }

    private static string ParseMenuImageSrc(IParentNode document)
    {
        var imageEl = document.QuerySelector(".menu_of_the_day img");

        var relativeImageSrc = imageEl.GetAttribute("src");

        return MenuUrl + relativeImageSrc;
    }

    private async Task<byte[]> FetchImage(string url)
    {
        using var client = httpClientFactory.CreateClient();

        await using var imageStream = await client.GetStreamAsync(url);

        using var memoryStream = new MemoryStream();

        await imageStream.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }

    private static async Task<byte[]> OptimizeImage(byte[] imageBytes)
    {
        using var image = Image.Load(imageBytes);
        
        var rectangle = new Rectangle((int)(image.Width * 0.84), 0, (int)(image.Width * 0.05), image.Height);
            
        image.Mutate(context =>
            context
                .Fill(Color.White, rectangle)
                .Resize((int)(image.Width * 2.5), (int)(image.Height * 2.5), KnownResamplers.Spline)
                .GaussianSharpen());

        using var memoryStream = new MemoryStream();

        await image.SaveAsTiffAsync(memoryStream);

        return memoryStream.ToArray();
    }

    private static string ParseTextFromImage(ExecutionContext context, byte[] imageBytes)
    {
        var languagesDir = Path.Combine(context.FunctionAppDirectory, "Languages");

        using var tesseractEngine = new TesseractEngine(languagesDir, "slk", EngineMode.Default);

        using var image = Pix.LoadTiffFromMemory(imageBytes);
        image.ConvertRGBToGray();

        using var page = tesseractEngine.Process(image, PageSegMode.SingleBlock);

        return page.GetText();
    }

    private Menu ParseMenu(string menuString)
    {
        var lines = Regex.Split(menuString, "\r\n|\r|\n").Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
        
        var splitMenuLines = SplitMenuLines(lines);

        var soup = FindAndParseSoup(splitMenuLines);

        var mainCourses = splitMenuLines.Select(menuLines => ParseMainCourse(menuLines, soup)).ToList();

        return new Menu(soup, mainCourses);
    }

    private static IList<IList<string>> SplitMenuLines(IList<string> lines)
    {
        lines = SkipUntilMenuStart(lines);

        var splitMenuLines = new List<IList<string>>();

        while (lines.Count > 0)
        {
            var menuLines = TakeUntilMenuEnd(lines);

            if (menuLines is null)
            {
                break;
            }

            splitMenuLines.Add(menuLines);

            lines = lines.Skip(1).ToList();

            lines = SkipUntilMenuStart(lines);
        }

        return splitMenuLines;
    }

    private Soup FindAndParseSoup(IEnumerable<IList<string>> splitMenuLines)
    {
        foreach (var menuLines in splitMenuLines)
        {
            if (!menuLines[0].StartsWith("Menu", StringComparison.InvariantCultureIgnoreCase))
            {
                continue;
            }

            if (!IsSoup(menuLines[1]))
            {
                continue;
            }

            var name = stringUtil.RemoveVolumeInfo(menuLines[1]);

            return new Soup(name);
        }

        return null;
    }

    private MainCourse ParseMainCourse(IList<string> menuLines, Soup soup)
    {
        var match = Regex.Match(menuLines[0], @"^(\w+ \d+) ([\d,]+)", RegexOptions.IgnoreCase);
        
        var identifier = match.Groups[1].Value;

        var priceStr = match.Groups[2].Value.Replace(',', '.');
        var price = double.Parse(priceStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);;
        
        var specialSoup = ParseSpecialSoup(menuLines[1], soup);

        var nameLines = menuLines.Skip(1).ToList();
        if (specialSoup is null)
        {
            nameLines = nameLines.Skip(1).ToList();
        }
        

        var nameBuilder = new StringBuilder(stringUtil.RemoveVolumeInfo(nameLines[0]));
        nameLines = nameLines.Skip(1).ToList();

        foreach (var nameLine in nameLines)
        {
            if (nameLine[0].IsDigit())
            {
                nameBuilder.Append(", ");
                nameBuilder.Append(stringUtil.RemoveVolumeInfo(nameLine));
                continue;
            }
            
            nameBuilder.Append(' ');
            nameBuilder.Append(nameLine);
        }

        var name = nameBuilder.ToString();

        return new MainCourse(identifier, name, price, specialSoup);
    }

    private Soup ParseSpecialSoup(string line, Soup soup)
    {
        if (!IsSoup(line))
        {
            return null;
        }
        
        var soupName = stringUtil.RemoveVolumeInfo(line);
            
        if (!soupName.Equals(soup.Name, StringComparison.InvariantCulture))
        {
            return new Soup(soupName);
        }

        return null;
    }
    
    private static bool IsSoup(string line)
    {
        return Regex.IsMatch(line, @"^[\d,]+(?:l|1|) ", RegexOptions.IgnoreCase);
    }
    
    private static IList<string> SkipUntilMenuStart(IList<string> menuLines)
    {
        var firstMenuIndex = 0;

        while (menuLines.Count > firstMenuIndex && !IsLineAMainCourseStart(menuLines[firstMenuIndex]))
        {
            ++firstMenuIndex;
        }

        return menuLines.Skip(firstMenuIndex).ToList();
    }

    private static IList<string> TakeUntilMenuEnd(IList<string> menuLines)
    {
        if (!IsLineAMainCourseStart(menuLines[0]))
        {
            return null;
        }

        var menuLength = 1;

        while (menuLines.Count > menuLength && !IsLineAMainCourseStart(menuLines[menuLength]) && !IsMenuPartEnd(menuLines[menuLength]))
        {
            ++menuLength;
        }

        return menuLines.Take(menuLength).ToList();
    }

    private static bool IsLineAMainCourseStart(string line)
    {
        return line.StartsWith("Špecialita", StringComparison.InvariantCultureIgnoreCase) ||
               line.StartsWith("Menu", StringComparison.InvariantCultureIgnoreCase);
    }

    private static bool IsMenuPartEnd(string line)
    {
        return line.StartsWith("Ponuka polievok");
    }
}