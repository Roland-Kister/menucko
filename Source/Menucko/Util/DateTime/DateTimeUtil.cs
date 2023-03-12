namespace Menucko.Util.DateTime;

public class DateTimeUtil : IDateTimeUtil
{
    public int GetDayOfWeek()
    {
#if DEBUG
        return 1;
#else
        return (int)System.DateTime.Now.DayOfWeek + 1;
#endif
    }
}