using System;

namespace RoutineBot
{
    [Flags]
    public enum WeekDays
    {
        Mon = 1,
        Tue = 2,
        Wed = 4,
        Thu = 8,
        Fri = 16,
        Sat = 32,
        Sun = 64

    }
}