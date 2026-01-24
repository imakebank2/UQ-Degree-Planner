namespace UQDegreePlanner.Pages;
public partial class Index
{
    public static class SemesterLimits
    {
    public const int Min = 1;
    public const int Max = 10;
    }
    private int _numberOfSemesters = SemesterLimits.Min;
    private int numberOfSemesters
    {
        get => _numberOfSemesters;
        set => _numberOfSemesters = Math.Clamp(value, SemesterLimits.Min, SemesterLimits.Max);
    } 

    public static class CoursePerSemesterLimits
    {
        public const int Min = 1;
        public const int Max = 5;
    }

    private int _numberOfCourses = 4;
    private int numberOfCourses
    {
        get => _numberOfCourses;
        set => _numberOfCourses = Math.Clamp(value, CoursePerSemesterLimits.Min, CoursePerSemesterLimits.Max);
    } 
}