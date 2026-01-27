using System.ComponentModel.DataAnnotations;

namespace UQDegreePlanner.Pages;
public partial class Home
{
    // Structure: Degree -> listofPlans --> listofCourses
    public class Program {
        public string Name {set; get;} = "My Program";
        public List<Degree> Degrees {set; get;} = new List<Degree>();

        public List<Course> GetAllCourses()
        {
            return Degrees
                .SelectMany(d => d.Plans)
                .SelectMany(p => p.Courses)
                .ToList();
        }

        public List<Plan> GetAllPlans()
        {
            return (List<Plan>) Degrees
                .SelectMany(d => d.Plans)
                .ToList();
        }

        public List<Degree> GetAllDegrees()
        {
            return Degrees;
        }
    }
    public class Degree {
        public Program? ProgramFrom {set; get;}
        public string Name {set; get;} = string.Empty;
        public List<Plan> Plans {set; get;} = new List<Plan>();

        [Range(1, 100)]
        public int UnitsRequired {set; get;} = 32;

        public bool MeetsRequirements()
        {
            // A degree meets its requirements if all its plans meet their requirements
            // AND all meets minimum units required for degree
            return Plans.All(plan => plan.MeetsRequirements()) && Plans.Sum(plan => plan.TotalUnits()) >= UnitsRequired;
        }
    }

    public class Plan
    {
        public Degree? DegreeFrom;
        public string Name {set; get;} = string.Empty;
        public PlanType Type {set; get;} = PlanType.None;
        public List<Course> Courses {set; get;} = new List<Course>();
        public enum PlanType
        {   
            Major,
            ExtendedMajor,
            Minor,
            None // No requirements
        }

        public bool MeetsRequirements()
        {
            // major must include 8 units of courses level 3 or higher
            // extended major must contain 12 units of courses at level 3 or higher
            // minor must contain 4 units of courses at level 2 or higher
            
            if (Type == PlanType.Major)
            {
                int level3OrHigherUnits = Courses.Where(c => c.Level >= 3).Count() * Course.Units;
                return level3OrHigherUnits >= 8;
            }
            else if (Type == PlanType.ExtendedMajor)
            {
                int level3OrHigherUnits = Courses.Where(c => c.Level >= 3).Count() * Course.Units;
                return level3OrHigherUnits >= 12;
            }
            else if (Type == PlanType.Minor)
            {
                int level2OrHigherUnits = Courses.Where(c => c.Level >= 2).Count() * Course.Units;
                return level2OrHigherUnits >= 4;
            } 
            else 
            {
                // If TYPE == PlanType.None: 
                return true; // No specific requirements for unspecified plan types
            }
        }
        
        public int TotalUnits()
        {
            return Courses.Count * Course.Units;
        }
    }

    public class Course 
    {
        public Plan? PlanFrom;

        [Required]
        private string courseCode {set; get;} = string.Empty;

        public string CourseCode {
            set { courseCode = value.ToUpper().Replace(" ", ""); }
            get => courseCode;
        }

        [Required]
        public string Name {get; set;} = string.Empty;

        private List<Course> Prerequisites = new List<Course>();
        private List<Course> Corequisites = new List<Course>();

        private List<Course> Incompatibilities = new List<Course>();

        [Range(1, 9)]
        public int Level {set; get;} = 1;

        [Required]
        public bool IsRequired {set; get;} = true;
        public const int Units = 2;

        public string PrequisiteInput {
            get => FormatCourses(Prerequisites);
            set => Prerequisites = ParseCourses(value);
        }

        public string CorequisiteInput {
            get => FormatCourses(Corequisites);
            set => Corequisites = ParseCourses(value);
        }

        public string IncompatibilityInput {
            get => FormatCourses(Incompatibilities);
            set => Incompatibilities = ParseCourses(value);
        }

        // Parses a comma-separated list of course codes into a list of Course objects
        private static List<Course> ParseCourses(string input) {
            return input
                .ToUpper()
                .Replace(" ", "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(code => new Course { CourseCode = code })
                .ToList();
        }

        // Formats a list of Course objects into a comma-separated list of course codes
        private static string FormatCourses(List<Course> courses) => string.Join(", ", courses.Select(c => c.CourseCode)); 
        }
        
        public static bool IsUniqueInList<T>(List<T> list, string value, Func<T, string> selector)   
        {
            // ignores case
            return !list.Any(item =>
            string.Equals(selector(item), value, StringComparison.OrdinalIgnoreCase));
        }

    public static class Limits
    {
        public const int MinSemesters = 1;
        public const int MaxSemesters = 20;
        public const int MaxCharacters = 50;
        public const int MinCoursesPerSemester = 1;
        public const int MaxCoursesPerSemester = 10;
        public const int MaxNumericFieldWidthInPixels = 200;
        public const int MaxCharacterLength = 100;
    }
}