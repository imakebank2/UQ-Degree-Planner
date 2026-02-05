using System.ComponentModel.DataAnnotations;


namespace UQDegreePlanner.Pages;

public partial class Home
{
    // Structure: Degree -> listofPlans --> listofCourses
    public class Program
    {
        public string Name { set; get; } = String.Empty;
        public List<Degree> Degrees { set; get; } = new List<Degree>();
        public List<Semester> Semesters { set; get; } = new List<Semester>();

        public List<Course> GetAllCourses()
        {
            return Degrees
                .SelectMany(d => d.Plans)
                .SelectMany(p => p.Courses)
                .ToList();
        }

        public List<Plan> GetAllPlans()
        {
            return Degrees
                .SelectMany(d => d.Plans)
                .ToList();
        }

        public List<Degree> GetAllDegrees()
        {
            return Degrees;
        }

        public List<Course> GetAllCoursesTakenOrdered()
        {
            return Semesters
                .SelectMany(s => s.Courses)
                .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                .ToList();
        }

        public bool HasDuplicateCoursesTaken()
        {
            return GetAllCoursesTakenOrdered()
                .GroupBy(c => c.Code)
                .Any(g => g.Count() > 1);
        }

        public bool CourseRulesSatisfied(out string message, List<Course> coursesTaken)
        {

            Console.WriteLine(string.Join(", ", coursesTaken.Select(c => c.Code)));
            foreach (Course course in coursesTaken)
            {

                foreach (string incCode in GetIncompatibilities(course))
                {
                    if (coursesTaken.Any(c => c.Code == incCode && c != course))
                    {
                        message = $"{course.Code} is incompatible with {incCode}.";
                        return false;
                    }
                }
                
                foreach (string cCode in GetPrerequisites(course))
                {
                    // Find where the current course is taken
                    int courseSemesterIndex = Semesters.FindIndex(sem =>
                        sem.Courses.Contains(course));

                    // Find where the prerequisite is taken
                    int prereqSemesterIndex = Semesters.FindIndex(sem =>
                        sem.Courses.Any(c => c.Code == cCode));

                    if (prereqSemesterIndex == -1)
                    {
                        message = $"{course.Code} requires {cCode}, but it is not taken.";
                        return false;
                    }

                    if (prereqSemesterIndex >= courseSemesterIndex)
                    {
                        message = $"{course.Code} requires {cCode} to be taken earlier.";
                        return false;
                    }
                }

                foreach (string cCode in GetCorequisites(course))
                {
                    // Find the semester where THIS course is taken
                    int courseSemesterIndex = Semesters.FindIndex(sem =>
                        sem.Courses.Contains(course));

                    // Find the semester where the corequisite is taken
                    int coreqSemesterIndex = Semesters.FindIndex(sem =>
                        sem.Courses.Any(c => c.Code == cCode));

                    // Not taken at all
                    if (coreqSemesterIndex == -1)
                    {
                        message = $"{course.Code} requires {cCode} as a corequisite, but it is not taken.";
                        return false;
                    }

                    // Must be in the SAME semester
                    if (coreqSemesterIndex != courseSemesterIndex)
                    {
                        message = $"{course.Code} requires {cCode} to be taken in the same semester.";
                        return false;
                    }
                }

            
        }

            message = "All course rules satisfied.";
            return true;
        }

        private List<string> GetFromCatalog(
            Course semesterCourse,
            Func<Course, List<string>> selector)
        {
            var catalogCourse = GetAllCourses()
                .FirstOrDefault(c => c.Code == semesterCourse.Code);

            return catalogCourse != null
                ? selector(catalogCourse)
                : new List<string>();
        }

        private List<string> GetIncompatibilities(Course c)
            => GetFromCatalog(c, x => x.Incompatibilities);

        private List<string> GetPrerequisites(Course c)
            => GetFromCatalog(c, x => x.Prerequisites);

        private List<string> GetCorequisites(Course c)
            => GetFromCatalog(c, x => x.Corequisites);

        

    }
    public class Degree
    {
        public Program? ProgramFrom { set; get; }
        public string Name { set; get; } = string.Empty;
        public List<Plan> Plans { set; get; } = new List<Plan>();

        [Range(1, 100)]
        public int UnitsRequired { set; get; } = 32;

        public bool MeetsRequirements(out string message, List<Course> coursesTaken)
        {
            // A degree meets its requirements if all its plans meet their requirements
            // AND all meets minimum units required for degree

            List<Course> coursesTakenInDegree = coursesTaken
                .Where(c => Plans.SelectMany(p => p.Courses)
                .Any(dc => dc.Code == c.Code))
                .ToList();

            foreach (Plan plan in Plans)
            {
                if (!plan.MeetsRequirements(out var planMessage, coursesTaken))
                {
                    message = $"{plan.Name} does not meet requirements: {planMessage}";
                    return false;
                }
            }


            var totalUnits = coursesTakenInDegree.Count * Course.Units;
            if (totalUnits < UnitsRequired)
            {
                message = $"Not enough units. Required: {UnitsRequired}, actual: {totalUnits}.";
                return false;
            }

            message = "All requirements met.";
            return true;
        }
    }


    public class Plan
    {
        public Degree? DegreeFrom;
        public string Name { set; get; } = string.Empty;
        public PlanType Type { set; get; } = PlanType.None;
        public List<Course> Courses { set; get; } = new List<Course>();
        public enum PlanType
        {
            Major,
            ExtendedMajor,
            Minor,
            None // No requirements
        }

        public bool MeetsRequirements(out string message, List<Course> courseList)
        {
            // Filter: only keep courses that belong to THIS plan
            List<Course> coursesTakenInPlan = courseList
                .Where(c => Courses.Any(pc => pc.Code == c.Code))
                .Select(c => GetCatalogCourse(c.Code, Courses))   
                .Where(c => c != null)
                .ToList();

            // Compute units
            int totalUnits = coursesTakenInPlan.Count * Course.Units;
            int unitsAtLevel2 = coursesTakenInPlan.Count(c => c.Level >= 2) * Course.Units;
            int unitsAtLevel3 = coursesTakenInPlan.Count(c => c.Level >= 3) * Course.Units;

            // Required courses
            var requiredCourses = Courses
                .Where(c => c.IsRequired && c.PlanFrom == this);

            var missing = requiredCourses
                .FirstOrDefault(req => !coursesTakenInPlan.Any(t => t.Code == req.Code));

            if (missing != null)
            {
                message = $"Missing required course: {missing.Code}";
                return false;
            }

            switch (Type)
            {
                case PlanType.Major:
                    if (unitsAtLevel3 < 8)
                    {
                        message = "Major requires 8 units at level 3 or higher.";
                        return false;
                    }
                    if (totalUnits < 16)
                    {
                        message = "Major requires 16 total units.";
                        return false;
                    }
                    message = "Major requirements met.";
                    return true;

                case PlanType.ExtendedMajor:
                    if (unitsAtLevel3 < 12)
                    {
                        message = "Extended major requires 12 units at level 3 or higher.";
                        return false;
                    }
                    if (totalUnits < 24)
                    {
                        message = "Extended major requires 24 total units.";
                        return false;
                    }
                    message = "Extended major requirements met.";
                    return true;

                case PlanType.Minor:
                    if (unitsAtLevel2 < 4)
                    {
                        message = "Minor requires 4 units at level 2 or higher.";
                        return false;
                    }
                    if (totalUnits < 8)
                    {
                        message = "Minor requires 8 total units.";
                        return false;
                    }
                    message = "Minor requirements met.";
                    return true;

                case PlanType.None:
                    message = "No requirements for this plan type.";
                    return true;
            }

            message = "Unknown plan type.";
            return false;
        }

        private Course GetCatalogCourse(string code, List<Course> courseList)
        {
            return courseList.FirstOrDefault(c => c.Code == code);
        }
    }

    public class Course
    {
        public Plan? PlanFrom;

        [Required]
        private string code { set; get; } = string.Empty;

        public string Code
        {
            set { code = value.ToUpper().Replace(" ", ""); }
            get => code;
        }

        [Required]
        public string Name { get; set; } = string.Empty;

        public List<string> Prerequisites { set; get; } = new List<string>();
        public List<string> Corequisites { set; get; } = new List<string>();

        public List<string> Incompatibilities { set; get; } = new List<string>();

        [Range(1, 9)]
        public int Level { set; get; } = 1;

        [Required]
        public bool IsRequired { set; get; } = true;
        public const int Units = 2;

        public string PrequisiteInput
        {
            get => FormatCourses(Prerequisites);
            set => Prerequisites = ParseCourses(value);
        }

        public string CorequisiteInput
        {
            get => FormatCourses(Corequisites);
            set => Corequisites = ParseCourses(value);
        }

        public string IncompatibilityInput
        {
            get => FormatCourses(Incompatibilities);
            set => Incompatibilities = ParseCourses(value);
        }

        // Parses a comma-separated list of course codes into a list of Course objects
        private static List<string> ParseCourses(string input)
        {
            return input
                .ToUpper()
                .Replace(" ", "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        // Formats a list of Course objects into a comma-separated list of course codes
        private static string FormatCourses(List<string> courses) =>
        string.Join(", ", courses);
        public class Semester(string name)
        {
            public string Name { get; set; } = name;
            public List<Course> Courses { get; set; } = new List<Course>();
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

    public class Semester(int semesterIndex)
    {
        public readonly string Name = $"Semester {semesterIndex}";
        public readonly int SemesterIndex = semesterIndex;
        public List<Course> Courses {get; set;} = new List<Course>();
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

