using Microsoft.EntityFrameworkCore;
using AttendanceTracker.Core.Model;

namespace AttendanceTracker.Data.Services;

public class DatabaseContext : DbContext
{
    public DbSet<Lesson> Lessons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string appfolder = Path.Combine(folder, "am-attendancetracker");
        if (!Directory.Exists(appfolder))
            Directory.CreateDirectory(appfolder);

        string dbpath = Path.Combine(appfolder, "appdata.db");
        optionsBuilder.UseSqlite($"Data Source={dbpath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var lesson = modelBuilder.Entity<Lesson>();
        lesson.HasKey(x => x.Id);
        lesson.Property(x => x.Id).ValueGeneratedOnAdd();
        lesson.Property(x => x.CourseName).IsRequired();
        lesson.Property(x => x.SecondName);
        lesson.Property(x => x.Date).IsRequired();
        lesson.Property(x => x.From).IsRequired();
        lesson.Property(x => x.To).IsRequired();
        lesson.Property(x => x.Duration).IsRequired();
        lesson.Property(x => x.Status).IsRequired();
    }
}
