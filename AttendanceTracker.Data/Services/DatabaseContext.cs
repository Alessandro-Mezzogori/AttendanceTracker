using Microsoft.EntityFrameworkCore;
using AttendanceTracker.Core.Model;

namespace AttendanceTracker.Data.Services;

public class DatabaseContext : DbContext
{
    public DbSet<Lesson> Lessons { get; set; }
    public DbSet<Course> Courses { get; set; }

    public string Folder => Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    public String AppFolder => Path.Combine(Folder, "am-attendancetracker");
    public string DbPath => Path.Combine(AppFolder, "appdata.db");

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!Directory.Exists(AppFolder))
            Directory.CreateDirectory(AppFolder);

        optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var course = modelBuilder.Entity<Course>();
        course.HasKey(x => x.Id);
        course.Property(x => x.Id).ValueGeneratedOnAdd();
        course.Property(x => x.DisplayName).IsRequired();
        course.Property(x => x.CourseName).IsRequired();
        course.Property(x => x.SecondName);


        var lesson = modelBuilder.Entity<Lesson>();
        lesson.HasKey(x => x.Id);
        lesson.Property(x => x.Id).ValueGeneratedOnAdd();
        lesson.Property(x => x.Date).IsRequired();
        lesson.Property(x => x.From).IsRequired();
        lesson.Property(x => x.To).IsRequired();
        lesson.Property(x => x.Duration).IsRequired();
        lesson.Property(x => x.Status).IsRequired();
        lesson.HasOne(x => x.Course).WithMany().HasForeignKey(x => x.CourseId).HasPrincipalKey(x => x.Id);
    }

    public const string LessonIndex = "UNQ_LessonOnDay";
    public const string CourseIndex = "UNQ_CourseNames";
}
