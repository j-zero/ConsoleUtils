using System.Globalization;
using TimeZoneNames;
using System;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace d
{
    internal class d
    {

        static void Main(string[] args)
        {
            string WeekOfYear = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.Now, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday).ToString();
            string DayOfYear = CultureInfo.InvariantCulture.Calendar.GetDayOfYear(DateTime.Now).ToString();
            var TimeZone = TZNames.GetAbbreviationsForTimeZone(TimeZoneInfo.Local.Id, "de-DE");
            var TimeZoneName = TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? TimeZone.Daylight : TimeZone.Standard;
            var beats = (int)(DateTime.Now.ToUniversalTime().AddHours(1).TimeOfDay.TotalMilliseconds / 86400);

            Console.WriteLine(DateTime.Now.ToLongDateString() + " | DoW " + DayOfYear);
            Console.WriteLine(DateTime.Now.ToLongTimeString() + " " + TimeZoneName + " | CW " + WeekOfYear);
            Console.WriteLine(DateTime.UtcNow.ToLongTimeString() + " UTC | Unix: " + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + " | @" + beats);
            Console.WriteLine();

            PrintCalendar(DateTime.Now);

        }

        static void PrintCalendar(DateTime Month)
        {
            // Get the first day of the current month
            var month = new DateTime(Month.Year, Month.Month, 1);

            // Print out the month, year, and the days of the week   
            // headingSpaces is calculated to align the year to the right side            
            var headingSpaces = new string(' ', 16 - month.ToString("MMMM").Length);
            Console.WriteLine($"{month.ToString("MMMM")}{headingSpaces}{month.Year}");
            Console.WriteLine(new string('-', 20));
            Console.WriteLine("Su Mo Tu We Th Fr Sa");

            // Get the number of days we need to leave blank at the 
            // start of the week. 
            var padLeftDays = (int)month.DayOfWeek;
            var currentDay = month;

            // Print out the day portion of each day of the month
            // iterations is the number of times we loop, which is the number
            // of days in the month plus the number of days we pad at the beginning
            var iterations = DateTime.DaysInMonth(month.Year, month.Month) + padLeftDays;

            for (int j = 0; j < iterations; j++)
            {
                // Pad the first week with empty spaces if needed
                if (j < padLeftDays)
                {
                    Console.Write("   ");
                }
                else
                {
                    // Write the day - pad left adds a space before single digit days
                    Console.Write($"{currentDay.Day.ToString().PadLeft(2, ' ')} ");

                    // If we've reached the end of a week, start a new line
                    if ((j + 1) % 7 == 0)
                    {
                        Console.WriteLine();
                    }

                    // Increment our 'currentDay' to the next day
                    currentDay = currentDay.AddDays(1);
                }
            }
            
        }
    }
}
