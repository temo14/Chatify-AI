using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace ChatAI.Application.Plugins;

/// <summary>
/// Time plugin for Semantic Kernel - provides date and time information
/// </summary>
public class TimePlugin
{
    [KernelFunction, Description("Get the current date and time in UTC")]
    public string GetCurrentDateTime()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC");
    }

    [KernelFunction, Description("Get the current date in UTC")]
    public string GetCurrentDate()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-dd");
    }

    [KernelFunction, Description("Get the current time in UTC")]
    public string GetCurrentTime()
    {
        return DateTime.UtcNow.ToString("HH:mm:ss UTC");
    }

    [KernelFunction, Description("Get the day of the week")]
    public string GetDayOfWeek()
    {
        return DateTime.UtcNow.DayOfWeek.ToString();
    }

    [KernelFunction, Description("Calculate the number of days between two dates")]
    public int DaysBetween(
        [Description("Start date in format yyyy-MM-dd")] string startDate,
        [Description("End date in format yyyy-MM-dd")] string endDate)
    {
        if (!DateTime.TryParse(startDate, out var start))
            throw new ArgumentException("Invalid start date format. Use yyyy-MM-dd");
        
        if (!DateTime.TryParse(endDate, out var end))
            throw new ArgumentException("Invalid end date format. Use yyyy-MM-dd");
        
        return (int)(end - start).TotalDays;
    }

    [KernelFunction, Description("Add days to a date")]
    public string AddDays(
        [Description("The base date in format yyyy-MM-dd")] string baseDate,
        [Description("Number of days to add (can be negative)")] int days)
    {
        if (!DateTime.TryParse(baseDate, out var date))
            throw new ArgumentException("Invalid date format. Use yyyy-MM-dd");
        
        return date.AddDays(days).ToString("yyyy-MM-dd");
    }
}
