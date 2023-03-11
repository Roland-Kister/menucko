using Menucko.Util.DateTime;
using Menucko.Util.Html;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Menucko.Startup))]

namespace Menucko;

public class Startup : FunctionsStartup
{
    public override void Configure(IFunctionsHostBuilder builder)
    {
        builder.Services.AddHttpClient();
        
        builder.Services.AddSingleton<IHtmlUtil, HtmlUtil>();
        builder.Services.AddSingleton<IDateTimeUtil, DateTimeUtil>();
    }
}