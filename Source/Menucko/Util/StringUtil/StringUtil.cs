using System;
using System.Text.RegularExpressions;
using AngleSharp.Text;

namespace Menucko.Util.StringUtil;

public class StringUtil : IStringUtil
{
    public string RemoveVolumeInfo(string line)
    {
        line = line.Trim();
        
        if (!line[0].IsDigit())
        {
            return line;
        }
        
        var firstWordEndIndex = line.IndexOf(' ', StringComparison.InvariantCulture) + 1;

        return line[firstWordEndIndex..];
    }

    public string RemoveNbsp(string line)
    {
        return Regex.Replace(line, @"&nbsp;", "");
    }

    public string RemoveAllergens(string line)
    {
        return Regex.Replace(line, @" ?\/[\d ,]+\/", "");
    }
}