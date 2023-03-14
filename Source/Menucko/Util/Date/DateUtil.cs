using System;

namespace Menucko.Util.Date;

public class DateUtil : IDateUtil
{
    public int GetDayOfWeek()
    {
#if DEBUG
        return 1;
#else
        return (int)DateTime.Now.DayOfWeek + 1;
#endif
    }
}