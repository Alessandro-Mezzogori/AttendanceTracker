using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AttendanceTracker.Core.Model;

public enum LessonStatus
{
    Attended = 1,
    NotAttended= 2,
    Cancelled = 3,
}

public class Lesson
{
    public int Id { get; set; } = 0;
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;
    public required DateOnly Date { get; set; }
    public required TimeOnly From { get; set; }
    public required TimeOnly To { get; set; }
    public required TimeSpan Duration { get; set; }
    public LessonStatus Status { get; set; } = LessonStatus.Attended;
}

public class Course
{
    public int Id { get; set; } = 0;
    public required string DisplayName { get; set; }
    public required string CourseName { get; set; }
    public string? SecondName { get; set; }
}