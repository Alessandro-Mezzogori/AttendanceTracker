using AttendanceTracker.Core.Model;
using AttendanceTracker.Data.Services;
using Avalonia.Controls;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace AttendanceTracker.ViewModels
{
    public partial class AttendedTime : ObservableObject
    {
        public required string CourseName { get; set; }
        public double Total { get; set; }
        public double Attended { get; set; }
        public double Percentage => Attended / Total * 100;
    }
 
    public partial class LessonDTO : ObservableObject
    {
        public int Id { get; set; } = 0;
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
                CourseName = lesson.CourseName,
                SecondName = lesson.SecondName,
                Date = lesson.Date,
                _status = lesson.Status,
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

        public MainWindowViewModel(DatabaseContext ctx)
        {
            _ctx = ctx;

            if (Design.IsDesignMode)
            {
                Lessons = new ObservableCollection<LessonDTO>([
                    new LessonDTO {
                        CourseName = "Test course name",
                        Date = DateOnly.FromDateTime(DateTime.Now),
                        From = new TimeOnly(10, 00),
                        To = new TimeOnly(11, 00),
                        Duration = TimeSpan.FromHours(1)
                    },
                    new LessonDTO{
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
            var enumerable = _ctx.Lessons
                .OrderBy(x => x.Date)
                .ThenBy(x => x.From)
                .ThenBy(x => x.To)
                .ThenBy(x => x.CourseName)
                .ThenBy(x => x.SecondName)
                .AsAsyncEnumerable();

            await foreach(var lesson in enumerable)
            {
                Lessons.Add(LessonDTO.FromLesson(lesson));
            }


            var attented = _ctx.Lessons
                .GroupBy(x => x.CourseName)
                .AsEnumerable()
                .Select(x => new AttendedTime
                {
                    CourseName = x.Key,
                    Total = x.Sum(x => x.Duration.TotalMinutes) / 45,
                    Attended = x.Sum(y => y.Status == LessonStatus.Attended ? y.Duration.TotalMinutes : 0) / 45
                });

            foreach (var a in attented)
            {
                AttendedTime.Add(a);
            }
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

        public async Task<bool> ImportExcel(Uri fileUri)
        {
            App.Logger.LogInformation("Starting import excel");

            using(var workbook = new XLWorkbook(fileUri.AbsolutePath)) 
            {
                var worksheet = workbook.Worksheet(1);
                var rows = worksheet.RangeUsed()?.RowsUsed().Skip(6);
                if(rows is null)
                {
                    App.Logger.LogError("Couldn't import");
                    return false;
                }

                _ctx.Lessons.ExecuteDelete();

                var list = new List<Core.Model.Lesson>();

                foreach(var row in rows)
                {
                    var date = row.Cell(1).GetValue<string>();
                    var hours = row.Cell(2).GetValue<string>().Split("-");

                    var startHour = hours[0].Trim();
                    var endHour = hours[1].Trim();

                    var courses = row.Cell(3).GetValue<string>().Split("-");

                    var course = courses[0].Trim();
                    var course_second = courses.Length > 1 ? courses[1].Trim() : null;

                    var from_time = TimeOnly.Parse(startHour);
                    var to_time = TimeOnly.Parse(endHour);

                    list.Add(new Core.Model.Lesson
                    {
                        CourseName = course,
                        SecondName = course_second,
                        Date = DateOnly.Parse(date),
                        From = from_time,
                        To = to_time,
                        Duration = to_time - from_time,
                    });
                }

                list.Sort((x,y) =>
                {
                    if (x.Date < y.Date)
                        return -1;
                    else if (x.Date > y.Date)
                        return 1;

                    if (x.From < y.From)
                        return -1;
                    else if (x.From > y.From)
                        return 1;

                    if (x.To < y.To)
                        return -1;
                    else if (x.To > y.To)
                        return 1;

                    var course = x.CourseName.CompareTo(y.CourseName);
                    if (course != 0)
                        return course;

                    if (x.SecondName is not null && y.SecondName is not null)
                        return x.SecondName.CompareTo(y.SecondName);

                    return 0;
                });

                var transaction = _ctx.Database.BeginTransaction();
                try
                {
                    await _ctx.Lessons.ExecuteDeleteAsync();
                    _ctx.Lessons.AddRange(list);
                    await _ctx.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                }

                Lessons.Clear();
                foreach(var lesson in list)
                {
                    Lessons.Add(LessonDTO.FromLesson(lesson));
                }
            }

            return true;
        }
    }
}
