using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace restapi.Models
{
    public class Timecard
    {
        public Timecard(int resource)
        {
            this.Resource = resource;
            UniqueIdentifier = Guid.NewGuid();
            Identity = new TimecardIdentity();
            Lines = new List<AnnotatedTimecardLine>();
            Transitions = new List<Transition> { 
                new Transition(new Entered() { Resource = resource }) 
            };
        }

        public int Resource { get; private set; }
        
        [JsonProperty("id")]
        public TimecardIdentity Identity { get; private set; }

        public TimecardStatus Status { 
            get 
            { 
                return Transitions
                    .OrderByDescending(t => t.OccurredAt)
                    .First()
                    .TransitionedTo;
            } 
        }

        public DateTime Opened;

        [JsonProperty("recId")]
        public int RecordIdentity { get; set; } = 0;

        [JsonProperty("recVersion")]
        public int RecordVersion { get; set; } = 0;

        public Guid UniqueIdentifier { get; set; }

        [JsonIgnore]
        public IList<AnnotatedTimecardLine> Lines { get; set; }

        [JsonIgnore]
        public IList<Transition> Transitions { get; set; }

        public IList<ActionLink> Actions { get => GetActionLinks(); }
    
        [JsonProperty("documentation")]
        public IList<DocumentLink> Documents { get => GetDocumentLinks(); }

        public string Version { get; set; } = "timecard-0.1";

        private IList<ActionLink> GetActionLinks()
        {
            var links = new List<ActionLink>();

            switch (Status)
            {
                case TimecardStatus.Draft:
                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{Identity.Value}/cancellation"
                    });

                    if (this.Lines.Count > 0)
                    {
                        links.Add(new ActionLink() {
                            Method = Method.Post,
                            Type = ContentTypes.Submittal,
                            Relationship = ActionRelationship.Submit,
                            Reference = $"/timesheets/{Identity.Value}/submittal"
                        });
                    }

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.TimesheetLine,
                        Relationship = ActionRelationship.RecordLine,
                        Reference = $"/timesheets/{Identity.Value}/lines"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Delete,
                        Type = ContentTypes.Timesheet,
                        Relationship = ActionRelationship.Remove,
                        Reference = $"/timesheets/{Identity.Value}"
                    });
                    break;

                case TimecardStatus.Submitted:
                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Cancellation,
                        Relationship = ActionRelationship.Cancel,
                        Reference = $"/timesheets/{Identity.Value}/cancellation"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Rejection,
                        Relationship = ActionRelationship.Reject,
                        Reference = $"/timesheets/{Identity.Value}/rejection"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Approval,
                        Relationship = ActionRelationship.Approve,
                        Reference = $"/timesheets/{Identity.Value}/approval"
                    });

                    links.Add(new ActionLink() {
                        Method = Method.Post,
                        Type = ContentTypes.Return,
                        Relationship = ActionRelationship.Return,
                        Reference = $"/timesheets/{Identity.Value}/return"
                    });
                    break;

                case TimecardStatus.Approved:
                    // terminal state, nothing possible here
                    break;

                case TimecardStatus.Cancelled:
                    links.Add(new ActionLink() {
                        Method = Method.Delete,
                        Type = ContentTypes.Timesheet,
                        Relationship = ActionRelationship.Remove,
                        Reference = $"/timesheets/{Identity.Value}"
                    });
                    break;
            }

            return links;
        }

        private IList<DocumentLink> GetDocumentLinks()
        {
            var links = new List<DocumentLink>();

            links.Add(new DocumentLink() {
                Method = Method.Get,
                Type = ContentTypes.Transitions,
                Relationship = DocumentRelationship.Transitions,
                Reference = $"/timesheets/{Identity.Value}/transitions"
            });

            if (this.Lines.Count > 0)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.TimesheetLine,
                    Relationship = DocumentRelationship.Lines,
                    Reference = $"/timesheets/{Identity.Value}/lines"
                });
            }

            if (this.Status == TimecardStatus.Submitted)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Submittal,
                    Reference = $"/timesheets/{Identity.Value}/submittal"
                });
            }

            if (this.Status == TimecardStatus.Rejected)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Rejection,
                    Reference = $"/timesheets/{Identity.Value}/rejection"
                });
            }

            if (this.Status == TimecardStatus.Approved)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Approval,
                    Reference = $"/timesheets/{Identity.Value}/approval"
                });
            }

            if (this.Status == TimecardStatus.Cancelled)
            {
                links.Add(new DocumentLink() {
                    Method = Method.Get,
                    Type = ContentTypes.Transitions,
                    Relationship = DocumentRelationship.Cancellation,
                    Reference = $"/timesheets/{Identity.Value}/cancellation"
                });
            }
            return links;
        }

        public AnnotatedTimecardLine AddLine(TimecardLine timecardLine)
        {
            var annotatedLine = new AnnotatedTimecardLine(timecardLine, Identity);

            Lines.Add(annotatedLine);

            return annotatedLine;
        }

        public AnnotatedTimecardLine ReplaceLine(TimecardLine timecardLine, AnnotatedTimecardLine oldLine)
        {
            var annotatedLine = new AnnotatedTimecardLine(timecardLine, Identity);
            annotatedLine.UniqueIdentifier = oldLine.UniqueIdentifier;
            Lines.Remove(oldLine);
            Lines.Add(annotatedLine);
            return annotatedLine;
        }

        public AnnotatedTimecardLine FindLine(string lineId)
        {
            AnnotatedTimecardLine timecardLine = null;
            foreach (AnnotatedTimecardLine line in Lines)
            {
                if (line.UniqueIdentifier.ToString() == lineId)
                {
                    timecardLine = line;
                }
            }
            return timecardLine;
        }

        public AnnotatedTimecardLine UpdateLine(UpdatedTimecardLine timecardLine, AnnotatedTimecardLine annotatedLine)
        {
            {
                int index = Lines.IndexOf(annotatedLine);
                if( timecardLine.Week != null ) Lines.ElementAt(index).Week = timecardLine.Week.Value;
                if( timecardLine.Year != null ) Lines.ElementAt(index).Year = timecardLine.Year.Value;
                if( timecardLine.Day != null) Lines.ElementAt(index).Day = timecardLine.Day.Value;
                if( timecardLine.Hours != null ) Lines.ElementAt(index).Hours = timecardLine.Hours.Value;
                if( timecardLine.Project != null ) Lines.ElementAt(index).Project = timecardLine.Project;
            }
            return annotatedLine;
        }
    }
}