namespace Menucko.Util.StringUtil;

public interface IStringUtil
{
    public string RemoveVolumeInfo(string line);

    public string RemoveNbsp(string line);

    public string RemoveAllergens(string line);
}