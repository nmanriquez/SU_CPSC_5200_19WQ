using System;
using System.Globalization;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace restapi.Models
{
    public class TimecardLine
    {
        public int Week { get; set; }

        public int Year { get; set; }

        public DayOfWeek Day { get; set; }

        public float Hours { get; set; }

        public string Project { get; set; }
    }

    public class UpdatedTimecardLine
    {
        public Nullable<int> Week { get; set; }
        public Nullable<int> Year { get; set; }
        public Nullable<float> Hours { get; set; }
        public Nullable<DayOfWeek> Day { get; set; }
        public string Project { get; set; }
    }

    public class AnnotatedTimecardLine : TimecardLine
    {
        private DateTime workDate;
        private DateTime? periodFrom;
        private DateTime? periodTo;

        public AnnotatedTimecardLine(TimecardLine line, TimecardIdentity timecardIdentity)
        {
            Week = line.Week;
            Year = line.Year;
            Day = line.Day;
            Hours = line.Hours;
            Project = line.Project;

            Recorded = DateTime.UtcNow;
            workDate = FirstDateOfWeekISO8601(line.Year, line.Week).AddDays((int)line.Day - 1);
            UniqueIdentifier = Guid.NewGuid();
            this.timecardIdentity = timecardIdentity;
        }

        public DateTime Recorded { get; set; }

        public string WorkDate { get => workDate.ToString("yyyy-MM-dd"); }

        public float LineNumber { get; set; }

        [JsonProperty("recId")]
        public int RecordIdentity { get; set; } = 0;

        [JsonProperty("recVersion")]
        public int RecordVersion { get; set; } = 0;

        public Guid UniqueIdentifier { get; set; }

        public string PeriodFrom { get => periodFrom?.ToString("yyyy-MM-dd"); }

        public string PeriodTo { get => periodTo?.ToString("yyyy-MM-dd"); }

        public string Version { get; set; } = "line-0.1";
        
        [JsonIgnore]
        public TimecardIdentity timecardIdentity { get; set; }

        public IList<ActionLink> Actions { get => GetActionLinks(); }

        private static DateTime FirstDateOfWeekISO8601(int year, int weekOfYear)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            var firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            var firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            var weekNum = weekOfYear;
            if (firstWeek <= 1)
            {
                weekNum -= 1;
            }

            var result = firstThursday.AddDays(weekNum * 7);

            return result.AddDays(-3);
        }

        private IList<ActionLink> GetActionLinks()
        {
            var links = new List<ActionLink>();
            links.Add(new ActionLink() {
                Method = Method.Post,
                Type = ContentTypes.TimesheetLine,
                Relationship = ActionRelationship.ReplaceLine,
                Reference = $"/timesheets/{timecardIdentity.Value}/lines/{UniqueIdentifier}"
            });

            links.Add(new ActionLink() {
                Method = Method.Patch,
                Type = ContentTypes.TimesheetLine,
                Relationship = ActionRelationship.UpdateLine,
                Reference = $"/timesheets/{timecardIdentity.Value}/lines/{UniqueIdentifier}"
            });
            return links;
        }
    }
}