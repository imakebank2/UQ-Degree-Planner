using System.Net.Http.Json;

namespace UQDegreePlanner.Pages;
public partial class Index
{
    public static class SemesterLimits
    {
        public const int Min = 1;
        public const int Max = 20;
    }
    
    private int numberOfSemesters
    {
        get => _numberOfSemesters;
        set => _numberOfSemesters = Math.Clamp(value, SemesterLimits.Min, SemesterLimits.Max);
    } 

    public static class CoursePerSemesterLimits
    {
        public const int Min = 1;
        public const int Max = 10;
    }

    
    private int numberOfCourses
    {
        get => _numberOfCourses;
        set => _numberOfCourses = Math.Clamp(value, CoursePerSemesterLimits.Min, CoursePerSemesterLimits.Max);
    } 

    public async Task<List<string>> getProgramList()
    {
        var program = await Http.GetFromJsonAsync<List<string>>("degrees.json");
        return program;
    }  
}