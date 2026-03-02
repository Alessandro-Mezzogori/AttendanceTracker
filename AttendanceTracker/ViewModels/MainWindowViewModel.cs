using AttendanceTracker.Core.Model;
using AttendanceTracker.Data.Services;
using Avalonia.Controls;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AttendanceTracker.ViewModels
{
    public partial class AttendedTime : ObservableObject
    {
        public Course Course { get; set; }
        public double Total { get; set; }
        public double Attended { get; set; }
        public double Percentage => Attended / Total * 100;
    }

    public partial class LessonDTO : ObservableObject
    {
        public int Id { get; set; } = 0;
        public required string DisplayName { get; set; }
        public required string CourseName { get; set; }
        public string? SecondName { get; set; }
        public required DateOnly Date { get; set; }
        public required TimeOnly From { get; set; }
        public required TimeOnly To { get; set; }
        public required TimeSpan Duration { get; set; }

        public double DurataAccadamica => Duration.TotalMinutes / 45;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(isCancelled))]
        [NotifyPropertyChangedFor(nameof(isUnattended))]
        [NotifyPropertyChangedFor(nameof(isAttend))]
        public LessonStatus _status;

        public bool isCancelled => Status == LessonStatus.Cancelled;
        public bool isUnattended => Status == LessonStatus.NotAttended;
        public bool isAttend => Status == LessonStatus.Attended;


        public static LessonDTO FromLesson(Lesson lesson)
        {
            return new()
            {
                Id = lesson.Id,
                DisplayName = lesson.Course.DisplayName,
                CourseName = lesson.Course.CourseName,
                SecondName = lesson.Course.SecondName,
                Date = lesson.Date,
                Status = lesson.Status,
                Duration = lesson.Duration,
                From = lesson.From,
                To = lesson.To,
            };
        }
    }

    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly DatabaseContext _ctx;

        public ObservableCollection<LessonDTO> Lessons { get; } = new ObservableCollection<LessonDTO>();
        public ObservableCollection<AttendedTime> AttendedTime { get; } = new ObservableCollection<AttendedTime>();

        public DateOnly? FilterStartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
        public DateOnly? FilterEndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);


        public MainWindowViewModel(DatabaseContext ctx)
        {
            _ctx = ctx;

            if (Design.IsDesignMode)
            {
                Lessons = new ObservableCollection<LessonDTO>([
                    new LessonDTO {
                        DisplayName = "Test course name",
                        CourseName = "Test course name",
                        Date = DateOnly.FromDateTime(DateTime.Now),
                        From = new TimeOnly(10, 00),
                        To = new TimeOnly(11, 00),
                        Duration = TimeSpan.FromHours(1)
                    },
                    new LessonDTO{
                        DisplayName = "Test course name 2",
                        CourseName = "Test course name 2",
                        Date = DateOnly.FromDateTime(DateTime.Now),
                        From = new TimeOnly(12, 00),
                        To = new TimeOnly(14, 00),
                        Duration = TimeSpan.FromHours(2)
                    }
                ]);
            }
        }

        public string Greeting { get; set; } = "Welcome to Avalonia!";

        public async Task LoadAsync()
        {
            IQueryable<Lesson> query = _ctx.Lessons.Include(x => x.Course);
            if (FilterStartDate is not null)
                query = query.Where(x => x.Date >= FilterStartDate);
            if (FilterEndDate is not null)
                query = query.Where(x => x.Date <= FilterEndDate);

            query = query
                .OrderBy(x => x.Date)
                .ThenBy(x => x.From)
                .ThenBy(x => x.To)
                .ThenBy(x => x.Course.DisplayName)
                .ThenBy(x => x.Course.SecondName)
                ;

            Lessons.Clear();
            await foreach (var lesson in query.AsAsyncEnumerable())
            {
                Lessons.Add(LessonDTO.FromLesson(lesson));
            }


            //var attented = _ctx.Lessons
            //    .GroupBy(x => x.CourseName)
            //    .AsEnumerable()
            //    .Select(x => new AttendedTime
            //    {
            //        CourseName = x.Key,
            //        Total = x.Sum(x => x.Duration.TotalMinutes) / 45,
            //        Attended = x.Sum(y => y.Status == LessonStatus.Attended ? y.Duration.TotalMinutes : 0) / 45
            //    });

            //foreach (var a in attented)
            //{
            //    AttendedTime.Add(a);
            //}
        }

        private async Task MarkStatus(LessonDTO lessonDto, LessonStatus status)
        {
            var lesson = _ctx.Lessons.FirstOrDefault(x => x.Id == lessonDto.Id);
            if (lesson is null)
                return;

            lesson.Status = status;
            await _ctx.SaveChangesAsync();

            lessonDto.Status = lesson.Status;
        }

        [RelayCommand]
        public Task MarkCancelledAsync(LessonDTO lessonDto) => MarkStatus(lessonDto, LessonStatus.Attended);

        [RelayCommand]
        public Task MarkNotAttendedAsync(LessonDTO lessonDto) => MarkStatus(lessonDto, LessonStatus.Cancelled);

        [RelayCommand]
        public Task MarkAttendedAsync(LessonDTO lessonDto) => MarkStatus(lessonDto, LessonStatus.NotAttended);

        public async Task ImportExcel(Uri fileUri)
        {
            App.Logger.LogInformation("Starting import excel");

            using (var workbook = new XLWorkbook(fileUri.AbsolutePath))
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(6);
                if (rows is null)
                {
                    App.Logger.LogError("Couldn't import");
                    return;
                }

                var lessons = await _ctx.Lessons.ToListAsync();
                var courses = await _ctx.Courses.ToListAsync();

                foreach (var row in rows)
                {
                    var date = DateOnly.Parse(row.Cell(1).GetValue<string>());
                    var hours = row.Cell(2).GetValue<string>().Split("-");

                    var startHour = hours[0].Trim();
                    var endHour = hours[1].Trim();

                    var courseSplitName = row.Cell(3).GetValue<string>().Split("-");
                    var courseName = courseSplitName[0].Trim();
                    var courseSecondName = courseSplitName.Length > 1 ? courseSplitName[1].Trim() : null;

                    var from_time = TimeOnly.Parse(startHour);
                    var to_time = TimeOnly.Parse(endHour);

                    // find course or update
                    var course = courses.Find(x => x.CourseName == courseName && x.SecondName == x.SecondName);
                    if (course is null)
                    {
                        course = new()
                        {
                            DisplayName = courseName,
                            CourseName = courseName,
                            SecondName = courseSecondName,
                        };
                        courses.Add(course);
                    }

                    // find lesson
                    var lesson = lessons.Find(x => x.CourseId == course.Id && x.Date == date && x.From == from_time && x.To == to_time);
                    if (lesson is null)
                    {
                        lesson = new Core.Model.Lesson
                        {
                            Course = course,
                            Date = date,
                            From = from_time,
                            To = to_time,
                            Duration = to_time - from_time,
                        };
                        lessons.Add(lesson);
                    }
                }

                var transaction = _ctx.Database.BeginTransaction();
                try
                {
                    _ctx.Courses.UpdateRange(courses);
                    await _ctx.SaveChangesAsync();

                    _ctx.Lessons.UpdateRange(lessons);
                    await _ctx.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }

            }
            return;
        }

        public async Task LoadDashboard()
        {
            var rawData = _ctx
                .Lessons
                .GroupBy(l => l.Course)
                .AsEnumerable()
                .Select(g => new
                {
                    Course = g.Key,

                    // Sums duration for lessons that are NOT cancelled
                    Total = g
                        .Where(lesson => lesson.Status != LessonStatus.Cancelled)
                        .Sum(lesson => lesson.Duration.TotalMinutes),

                    // Sums duration for lessons that ARE attended
                    Attended = g
                        .Where(lesson => lesson.Status == LessonStatus.Attended)
                        .Where(x => x.Date <= DateOnly.FromDateTime(DateTime.Now))
                        .Sum(lesson => lesson.Duration.TotalMinutes)
                });

            var courses = _ctx.Courses.ToList();

            // 2. Map the results to your ObservableObject class in memory
            AttendedTime.Clear();
            foreach (var data in rawData)
            {
                AttendedTime.Add(new AttendedTime
                {
                    Course = data.Course,
                    Total = data.Total / 45,
                    Attended = data.Attended / 45
                });
            }
        }
    }
}
