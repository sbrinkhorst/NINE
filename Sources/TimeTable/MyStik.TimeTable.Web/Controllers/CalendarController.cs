﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using log4net;
using Microsoft.AspNet.Identity;
using MyStik.TimeTable.Data;
using MyStik.TimeTable.DataServices;
using MyStik.TimeTable.Web.Helpers;
using MyStik.TimeTable.Web.Models;
using MyStik.TimeTable.Web.Services;
using MyStik.TimeTable.Web.Utils.Helper;
using RoomService = MyStik.TimeTable.Web.Services.RoomService;

namespace MyStik.TimeTable.Web.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public class CalendarController : BaseController
    {
        private IEnumerable<CalendarEventModel> GetCalendarEvents(IEnumerable<ActivityDateSummary> dates, bool printHosts, bool printRooms, bool useStates=true)
        {
            var events = new List<CalendarEventModel>();

            var datesToDelete = new List<ActivityDate>();

            foreach (var date in dates)
            {
                if (date.Activity != null)
                {
                    // State
                    // wenn Course => Activity
                    // wenn Sprechstunde => Date oder Slot

                    var eventViewModel = new CalenderEventViewModel
                    {
                        Summary = date,
                        State =
                            date.Date.Activity is Course && useStates
                                ? ActivityService.GetActivityState(date.Date.Activity.Occurrence, AppUser)
                                : null,
                        Lottery = date.Activity.Occurrence != null 
                                    ? Db.Lotteries.FirstOrDefault(x => x.Occurrences.Any(y => y.Id == date.Activity.Occurrence.Id))
                                    : null
                    };



                    // Workaround für fullcalendar
                    // wenn der Kalendereintrag ein zu geringe höhe hat,
                    // dann wird statt des Endes der Titel angezeigt
                    // Den Titel rendern wir selber, d.h. i.d.R. geben wir ihn nicht an!
                    // file: fullcalendar.js line 3945
                    /*
                    var duration = date.Date.End - date.Date.Begin;
                        if (duration.TotalMinutes <= 60)
                            string title = null;
                            title = date.Date.End.TimeOfDay.ToString(@"hh\:mm");
                     */
                    var sb = new StringBuilder();

                    if (date.Slot!=null)
                    {
                        events.Add(new CalendarEventModel
                        {
                            title = sb.ToString(),
                            allDay = false,
                            start = date.Date.Begin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            end = date.Date.End.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            textColor = date.TextColor,
                            backgroundColor = date.BackgroundColor,
                            borderColor = "#ff0000",
                            htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                            htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                            htmlContent = this.RenderViewToString("_CalendarEventContent", eventViewModel),
                        });
                    }
                    else
                    {
                        events.Add(new CalendarEventModel
                        {
                            title = sb.ToString(),
                            allDay = false,
                            start = date.Date.Begin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            end = date.Date.End.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            textColor = date.TextColor,
                            backgroundColor = date.BackgroundColor,
                            borderColor = "#000",
                            htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                            htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                            htmlContent = this.RenderViewToString("_CalendarEventContent", eventViewModel),
                        });
                    }
                }
                else
                {
                    datesToDelete.Add(date.Date);
                }
            }

            if (datesToDelete.Any())
            {
                // so ein Date darf es nicht geben => löschen
                var db = new TimeTableDbContext();
                foreach (var date in datesToDelete)
                {
                    var dd = db.ActivityDates.SingleOrDefault(d => d.Id == date.Id);
                    dd.Hosts.Clear();
                    dd.Rooms.Clear();
                    db.ActivityDates.Remove(dd);
                }
                db.SaveChanges();
            }

            return events;
        }

        private IEnumerable<CalendarEventModel> GetCalendarEventsMobile(IEnumerable<ActivityDateSummary> dates, bool printHosts, bool printRooms, bool useStates = true)
        {
            var events = new List<CalendarEventModel>();

            var user = AppUser;

            foreach (var date in dates)
            {
                if (date.Activity != null)
                {
                    // State
                    // wenn Course => Activity
                    // wenn Sprechstunde => Date oder Slot

                    var eventViewModel = new CalenderEventViewModel
                    {
                        Summary = date,
                        State =
                            date.Date.Activity is Course && useStates
                                ? ActivityService.GetActivityState(date.Date.Activity.Occurrence, user)
                                : null,
                    };

                    // Workaround für fullcalendar
                    // wenn der Kalendereintrag ein zu geringe höhe hat,
                    // dann wird statt des Endes der Titel angezeigt
                    // Den Titel rendern wir selber, d.h. i.d.R. geben wir ihn nicht an!
                    // file: fullcalendar.js line 3945
                    var duration = date.Date.End - date.Date.Begin;
                    
                    var sb = new StringBuilder();
                    //HIER ÄNDERN
                    events.Add(new CalendarEventModel
                    {
                        
                        allDay = false,
                        start = date.Date.Begin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        end = date.Date.End.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        textColor = date.TextColor,
                        backgroundColor = date.BackgroundColor,
                        borderColor = "#000000",
                        htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                        htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                        htmlContent = this.RenderViewToString("_CalendarEventContentMobile", eventViewModel),
                    });
                }
            }

            return events;
        }


        /// <summary>
        /// Umwandlung eines UNIX-Dates in ein DateTimeObjekt
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        /*
        private DateTime GetDateTime(int time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(time);
        }
         */

        private DateTime GetDateTime(string time)
        {
            var dt = DateTime.Parse(time);
            return dt;
        }

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="semGroupId"></param>
        /// <param name="topicId"></param>
        /// <param name="showPersonalDates"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public JsonResult CourseEventsByProgram(Guid semGroupId, Guid? topicId, bool showPersonalDates, string start, string end)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            var cal = new List<CalendarEventModel>();

            var db = new TimeTableDbContext();

            var dateMap = new Dictionary<Guid, ActivityDateSummary>();

            var user = UserManager.FindByName(User.Identity.Name);

            if (user != null && showPersonalDates)
            {
                // 1. Angebot des angemeldeten Dozentens
                var allDates = db.ActivityDates.Where(c =>
                    c.Begin >= startDate && c.End <= endDate &&
                    c.Hosts.Any(l => !string.IsNullOrEmpty(l.UserId) && l.UserId.Equals(user.Id))).ToList();

                foreach (var date in allDates)
                {
                    dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.Offer);
                }


                // 2. die gebuchten
                var myOcs = db.Occurrences.Where(o => o.Subscriptions.Any(s => s.UserId.Equals(user.Id))).ToList();

                var ac = new ActivityService();

                foreach (var occ in myOcs)
                {
                    var summary = ac.GetSummary(occ.Id);

                    var dates = summary.GetDates(startDate, endDate);

                    foreach (var date in dates)
                    {
                        if (!dateMap.ContainsKey(date.Id))
                        {
                            dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.Subscription);

                        }
                    }
                }
            }

            // 3. das Suchergebnis
            // das Semester suchen, dass zum Datum passt
            // Grundannahme:  Vorlesungszeiten überlappen sich nicht
            var semGroup = Db.SemesterGroups.SingleOrDefault(x => x.Id == semGroupId);

            if (semGroup != null && user != null)
            {
                // Daten anzeigen, wenn Booking Enabled
                var lookUp = semGroup.IsAvailable;
                if (!lookUp)
                {
                    // wenn noch nicht freigegeben, dann Staff und SysAdmin
                    lookUp = (user.MemberState == MemberState.Staff) || User.IsInRole("SysAdmin");
                }

                if (lookUp)
                {
                    var courses = semGroup.Activities.ToList();
                    foreach (var course in courses)
                    {
                        var dates = course.Dates.Where(c => c.Begin >= startDate && c.End <= endDate);

                        foreach (var date in dates)
                        {
                            if (!dateMap.ContainsKey(date.Id))
                            {
                                dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.SearchResult);
                            }
                        }
                    }
                }
            }

            cal.AddRange(GetCalendarEvents(dateMap.Values.ToList(), true, true));

            return Json(cal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dozId"></param>
        /// <param name="showPersonalDates"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CourseEventsByLecturer(Guid dozId, bool showPersonalDates, string start, string end)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            var cal = new List<CalendarEventModel>();

            var db = new TimeTableDbContext();

            var user = UserManager.FindByName(User.Identity.Name);

            var dateMap = new Dictionary<Guid, ActivityDateSummary>();

            if (showPersonalDates)
            {
                // 1. Angebot des angemeldeten Dozentens
                var allMyDates = db.ActivityDates.Where(c =>
                    c.Begin >= startDate && c.End <= endDate &&
                    c.Hosts.Any(l => !string.IsNullOrEmpty(l.UserId) && l.UserId.Equals(user.Id))).ToList();

                foreach (var date in allMyDates)
                {
                    dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.Offer);
                }

                // 2. die gebuchten
                var myOcs = db.Occurrences.Where(o => o.Subscriptions.Any(s => s.UserId.Equals(user.Id))).ToList();

                var ac = new ActivityService();

                foreach (var occ in myOcs)
                {
                    var summary = ac.GetSummary(occ.Id);

                    var dates = summary.GetDates(startDate, endDate);

                    foreach (var date in dates)
                    {
                        if (!dateMap.ContainsKey(date.Id))
                        {
                            dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.Subscription);

                        }
                    }
                }
            }


            // 3. das Suchergebnis
            // das Semester suchen, dass zum Datum passt
            // Grundannahme:  Vorlesungszeiten überlappen sich nicht
            var member = db.Members.SingleOrDefault(x => x.Id == dozId);
            if (member != null)
            {
            }
            var allDates = db.ActivityDates.Where(c =>
                c.Begin >= startDate && c.End <= endDate &&
                c.Hosts.Any(oc => oc.Id == member.Id)).ToList();

            foreach (var date in allDates)
            {
                if (!dateMap.ContainsKey(date.Id))
                {
                    dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.SearchResult);
                }
            }
        

        /*
            var semester = new SemesterService().GetSemester(startDate, endDate);

            if (semester != null)
            {
                // Daten anzeigen, wenn Booking Enabled oder
                // wenn Benutzer Member der FK09 ist (=Prof, LB, Sekretariat)

                var lookUp = semester.BookingEnabled;
                if (!lookUp)
                {
                    lookUp = new MemberService(Db, UserManager).IsUserMemberOf(User.Identity.Name, "FK 09") ||
                        User.IsInRole("SysAdmin");
                }

                var fk09 = db.Organisers.SingleOrDefault(org => org.ShortName.Equals("FK 09"));

                if (fk09 != null)
                {
                    var lecturer = fk09.Members.SingleOrDefault(l => l.Id == dozId);

                    if (lecturer != null)
                    {
                        var allDates = db.ActivityDates.Where(c =>
                            c.Begin >= startDate && c.End <= endDate &&
                            c.Hosts.Any(oc => oc.Id == lecturer.Id)).ToList();

                        foreach (var date in allDates)
                        {
                            // Sprechstunden immer anzeigen
                            if (lookUp || date.Activity is OfficeHour)
                            {
                                if (!dateMap.ContainsKey(date.Id))
                                {
                                    dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.SearchResult);
                                }
                            }
                        }
                    }
                }
            }
             * */

            cal.AddRange(GetCalendarEvents(dateMap.Values.ToList(), false, true));

            return Json(cal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="showPersonalDates"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CourseEventsByRoom(Guid roomId, bool showPersonalDates, string start, string end)
        {
            try
            {

                var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            var cal = new List<CalendarEventModel>();

            var db = new TimeTableDbContext();

            var user = UserManager.FindByName(User.Identity.Name);

            var dateMap = new Dictionary<Guid, ActivityDateSummary>();

            if (showPersonalDates)
            {
                // 1. Angebot des angemeldeten Dozentens
                var allMyDates = db.ActivityDates.Where(c =>
                    c.Begin >= startDate && c.End <= endDate &&
                    c.Hosts.Any(l => !string.IsNullOrEmpty(l.UserId) && l.UserId.Equals(user.Id))).ToList();

                foreach (var date in allMyDates)
                {
                    dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.Offer);
                }


                // 2. die gebuchten
                var myOcs = db.Occurrences.Where(o => o.Subscriptions.Any(s => s.UserId.Equals(user.Id))).ToList();

                var ac = new ActivityService();

                foreach (var occ in myOcs)
                {
                    var summary = ac.GetSummary(occ.Id);

                    var dates = summary.GetDates(startDate, endDate);

                    foreach (var date in dates)
                    {
                        if (!dateMap.ContainsKey(date.Id))
                        {
                            dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.Subscription);

                        }
                    }
                }
            }

            // 3. Suchergebnis
            // das Semester suchen, dass zum Datum passt
            // Grundannahme:  Vorlesungszeiten überlappen sich nicht

                var room = db.Rooms.SingleOrDefault(l => l.Id == roomId);

                if (room != null)
                {
                    var allDates = db.ActivityDates.Where(c =>
                        c.Begin >= startDate && c.End <= endDate &&
                        c.Rooms.Any(oc => oc.Id == room.Id)).ToList();

                    foreach (var date in allDates)
                    {
                        if (!dateMap.ContainsKey(date.Id))
                        {
                            dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.SearchResult);
                        }
                    }
                }

                cal.AddRange(GetCalendarEvents(dateMap.Values.ToList(), true, false));

                return Json(cal);

            }
            catch (Exception e)
            {
                return Json(e.ToString());
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ActivityPlan(string start, string end)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            return Json(
                GetCalendarEvents(
                    GetActivityPlan(User.Identity.Name, startDate, endDate),
                    true, true));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ActivityPrintPlan(string start, string end, Guid? id)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            var events = new List<CalendarEventModel>();

            // start und end sind "echte" Daten, d.h. eine Woche

            // Am Schluss muss alles in "Wochentage" umgerechnet werden

            // Alle Events abstrakt nah Wochentag
            var semester = SemesterService.GetSemester(id);
            var member = GetMyMembership();
            var user = GetCurrentUser();

            var courses = new List<Course>();


            // Alle Vorlesungen, die ich halte
            if (member != null)
            {
                var mylecture =
                    Db.Activities.OfType<Course>()
                        .Where(c =>
                            c.SemesterGroups.Any(g => g.Semester.Id == semester.Id) &&
                            c.Dates.Any(oc => oc.Hosts.Any(l => l.Id == member.Id)))
                        .ToList();
                courses.AddRange(mylecture);
            }

            // Alle Kurse, in denen ich in diesem Semester eingetragen bin
            var mylisten =
                Db.Activities.OfType<Course>()
                    .Where(c =>
                        c.SemesterGroups.Any(g => g.Semester.Id == semester.Id) &&
                        c.Occurrence.Subscriptions.Any(x => x.UserId == user.Id))
                    .ToList();

            courses.AddRange(mylisten);

            foreach (var course in courses)
            {
                var days = (from occ in course.Dates
                    select
                        new
                        {
                            Day = occ.Begin.DayOfWeek,
                            Begin = occ.Begin.TimeOfDay,
                            End = occ.End.TimeOfDay,
                        }).Distinct().ToList();

                var eventViewModel = new CalenderEventPrintViewModel();
                eventViewModel.Course = course;
                eventViewModel.Rooms = Db.Rooms.Where(x => x.Dates.Any(y => y.Activity.Id == course.Id)).Distinct().ToList();


                if (days.Count == 1)
                {
                    var day = days.First();

                    var calDay = startDate.AddDays((int) day.Day - (int) startDate.DayOfWeek);
                    var calBegin = calDay.Add(day.Begin);
                    var calEnd = calDay.Add(day.End);

                    // Einfacher Eintrag
                    events.Add(new CalendarEventModel
                    {
                        title = course.Name,
                        allDay = false,
                        start = calBegin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        end = calEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        textColor = "#000",
                        backgroundColor = "#fff",
                        borderColor = "#000",
                        //htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                        //htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                        htmlContent = this.RenderViewToString("_CalendarEventContentPrint", eventViewModel),
                    });

                }
                else
                {
                    foreach (var day in days)
                    {
                        var calDay = startDate.AddDays((int)day.Day - (int)startDate.DayOfWeek);
                        var calBegin = calDay.Add(day.Begin);
                        var calEnd = calDay.Add(day.End);

                        // Einfacher Eintrag
                        events.Add(new CalendarEventModel
                        {
                            title = course.Name,
                            allDay = false,
                            start = calBegin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            end = calEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            textColor = "#000",
                            backgroundColor = "#fff",
                            borderColor = "#000",
                            //htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                            //htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                            htmlContent = this.RenderViewToString("_CalendarEventContentPrint", eventViewModel),
                        });
                    }
                }
            }

            
            
            return Json(events);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult ActivityPlanMobile(string start, string end)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            return Json(
                GetCalendarEventsMobile(
                    GetActivityPlan(User.Identity.Name, startDate, endDate),
                    true, true));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult DailyRota(string start, string end)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            var db = new TimeTableDbContext();
            var org = GetMyOrganisation();

            var allDates = db.ActivityDates.Where(c =>
                (c.Activity.Organiser != null && c.Activity.Organiser.Id == org.Id) &&
                (c.Begin >= startDate && c.End <= endDate)).ToList();

            var eventDates = new List<ActivityDateSummary>();

            foreach (var date in allDates)
            {
                eventDates.Add(new ActivityDateSummary
                {
                    Date = date,
                    DateType = ActivityDateType.SearchResult,
                });
            }

            /*
             * wenn man eigene events bauen möchte
            var events = new List<CalendarEventModel>();

            foreach (var date in allDates)
            {
                events.Add(new CalendarEventModel
                {
                    title = sb.ToString(),
                    allDay = false,
                    start = date.Date.Begin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    end = date.Date.End.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    textColor = date.TextColor,
                    backgroundColor = date.BackgroundColor,
                    borderColor = "#000",
                    htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                    htmlContent = this.RenderViewToString("_CalendarEventContent", eventViewModel),
                });
            }
             */


            return Json(
                GetCalendarEvents(
                    eventDates,
                    true, true, false));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult RoomPrintPlan(Guid roomId, string start, string end)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            var events = new List<CalendarEventModel>();

            // start und end sind "echte" Daten, d.h. eine Woche

            // Am Schluss muss alles in "Wochentage" umgerechnet werden

            // Alle Events abstrakt nah Wochentag
            var semester = SemesterService.GetSemester(DateTime.Today);

            var roomService = new RoomService();

            var model = roomService.GetRoomSchedule(roomId, semester);


                foreach (var date in model.RegularDates)
                {
                    var day = date.Dates.First();


                    var calDay = startDate.AddDays((int)day.Begin.DayOfWeek - (int)startDate.DayOfWeek);
                    var calBegin = calDay.Add(day.Begin.TimeOfDay);
                    var calEnd = calDay.Add(day.End.TimeOfDay);

                    var duration = day.End.TimeOfDay - day.Begin.TimeOfDay;
                    var title = date.Activity.Name;

                    if (duration.TotalMinutes <= 60)
                        title = string.Empty;                   // nur Kurzname
                    else if (duration.TotalMinutes <= 90)
                        title = title.Truncate(20);             // Langname stark gekürzt
                    else
                        title = title.Truncate(50);             // Lamgname

                    var hostNames = "N.N.";
                    if (day.Hosts.Any())
                    {
                        var sbHost = new StringBuilder();
                        foreach (var host in day.Hosts)
                        {
                            sbHost.Append(host.Name);
                            if (host != day.Hosts.Last())
                            {
                                sbHost.Append(", ");
                            }
                        }
                        hostNames = sbHost.ToString();
                    }
                    

                    var sb = new StringBuilder();
                    if (duration.TotalMinutes <= 60)
                    {
                        sb.AppendFormat("{0} [{2}]", date.Activity.ShortName, title, hostNames);
                    }
                    else
                    {
                        sb.AppendFormat("{0} - {1} [{2}]", date.Activity.ShortName, title, hostNames);
                    }

                    // Einfacher Eintrag
                    events.Add(new CalendarEventModel
                    {
                        title = sb.ToString(),
                        allDay = false,
                        start = calBegin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        end = calEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        textColor = "#000",
                        backgroundColor = "#fff",
                        borderColor = "#000",
                        //htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                        //htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                        //htmlContent = this.RenderViewToString("_CalendarEventContent", eventViewModel),
                    });
                }



            return Json(events);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult RoomPrintPlanWeek(Guid roomId, string start, string end)
        {
            try
            {

                var startDate = GetDateTime(start);
                var endDate = GetDateTime(end);

                var events = new List<CalendarEventModel>();

                var db = new TimeTableDbContext();

                var user = UserManager.FindByName(User.Identity.Name);

                var dateMap = new Dictionary<Guid, ActivityDateSummary>();

                // 3. Suchergebnis
                // das Semester suchen, dass zum Datum passt
                // Grundannahme:  Vorlesungszeiten überlappen sich nicht

                var room = db.Rooms.SingleOrDefault(l => l.Id == roomId);

                if (room != null)
                {
                    var allDates = db.ActivityDates.Where(c =>
                        c.Begin >= startDate && c.End <= endDate &&
                        c.Rooms.Any(oc => oc.Id == room.Id)).ToList();

                    foreach (var date in allDates)
                    {
                        var day = date;


                        var calDay = startDate.AddDays((int)day.Begin.DayOfWeek - (int)startDate.DayOfWeek);
                        var calBegin = calDay.Add(day.Begin.TimeOfDay);
                        var calEnd = calDay.Add(day.End.TimeOfDay);

                        var duration = day.End.TimeOfDay - day.Begin.TimeOfDay;
                        var title = date.Activity.Name;

                        if (duration.TotalMinutes <= 60)
                            title = string.Empty;                   // nur Kurzname
                        else if (duration.TotalMinutes <= 90)
                            title = title.Truncate(20);             // Langname stark gekürzt
                        else
                            title = title.Truncate(50);             // Lamgname

                        var hostNames = "N.N.";
                        if (day.Hosts.Any())
                        {
                            var sbHost = new StringBuilder();
                            foreach (var host in day.Hosts)
                            {
                                sbHost.Append(host.Name);
                                if (host != day.Hosts.Last())
                                {
                                    sbHost.Append(", ");
                                }
                            }
                            hostNames = sbHost.ToString();
                        }


                        var sb = new StringBuilder();
                        if (duration.TotalMinutes <= 60)
                        {
                            sb.AppendFormat("{0} [{2}]", date.Activity.ShortName, title, hostNames);
                        }
                        else
                        {
                            sb.AppendFormat("{0} - {1} [{2}]", date.Activity.ShortName, title, hostNames);
                        }

                        // Einfacher Eintrag
                        events.Add(new CalendarEventModel
                        {
                            title = sb.ToString(),
                            allDay = false,
                            start = calBegin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            end = calEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            textColor = "#000",
                            backgroundColor = "#fff",
                            borderColor = "#000",
                            //htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                            //htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                            //htmlContent = this.RenderViewToString("_CalendarEventContent", eventViewModel),
                        });

                    }
                }

                return Json(events);

            }
            catch (Exception e)
            {
                return Json(e.ToString());
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private IEnumerable<ActivityDateSummary>  GetActivityPlan(string userName, DateTime startDate, DateTime endDate)
        {
            var dateMap = new Dictionary<Guid, ActivityDateSummary>();

            var db = new TimeTableDbContext();

            var user = UserManager.FindByName(userName);

            if (user != null)
            {
                // Was der User anbietet
                // Termine als Host
                var allMyDates = db.ActivityDates.Where(c =>
                    c.Begin >= startDate && c.End <= endDate &&
                    c.Hosts.Any(l => !string.IsNullOrEmpty(l.UserId) && l.UserId.Equals(user.Id))).ToList();

                foreach (var date in allMyDates)
                {
                    dateMap[date.Id] = new ActivityDateSummary(date, ActivityDateType.Offer);
                }

                // 2. die gebuchten
                var myOcs = db.Occurrences.Where(o => o.Subscriptions.Any(s => s.UserId.Equals(user.Id))).ToList();

                var ac = new ActivityService();

                foreach (var occ in myOcs)
                {
                    var summary = ac.GetSummary(occ.Id);

                    var dates = summary.GetDates(startDate, endDate);

                    foreach (var date in dates)
                    {
                        if (!dateMap.ContainsKey(date.Id))
                        {
                            var dateSummary = new ActivityDateSummary(date, ActivityDateType.Subscription);
                            if (summary is ActivitySlotSummary slotSummary)
                            {
                                dateSummary.Slot = slotSummary.Slot;
                            }
                            dateMap[date.Id] = dateSummary;

                        }
                    }
                }
            }

            return dateMap.Values.ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public FileResult File()
        {
            var user = UserManager.FindByName(User.Identity.Name);

            if (user != null)
            {
                return GetCalendarData(user);
            }
            return null;
        }

        private FileResult GetCalendarData(ApplicationUser user)
        {
            // immer nach vorne sehen
            var semester = SemesterService.GetSemester(DateTime.Today);
            var nextSemester = SemesterService.GetNextSemester(DateTime.Today);

            var from = semester.StartCourses;
            var to = semester.EndCourses;

            if (nextSemester != null)
            {
                to = nextSemester.EndCourses;
            }


            var dateList = GetActivityPlan(user.UserName, from, to);

            var tz = "Europe/Berlin";

            var iCal = new Calendar();


            iCal.AddTimeZone(new VTimeZone(tz));

            var now = DateTime.Now;

            foreach (var date in dateList)
            {
                var evt = iCal.Create<CalendarEvent>(); 
                evt.Summary = date.Name;
                if (date.Date.Occurrence != null && date.Date.Occurrence.IsCanceled)
                {
                    evt.Summary += " - abgesagt!";
                }

                if (date.Date.Activity is OfficeHour && user.MemberState == MemberState.Staff)
                {
                    int n = 0;
                    if (date.Date.Occurrence != null)
                    {
                        n += date.Date.Occurrence.Subscriptions.Count;
                    }
                    n += date.Date.Slots.Sum(slot => slot.Occurrence.Subscriptions.Count);

                    if (n > 0)
                    {
                        evt.Summary += $" - {n} Eintragungen";
                    }
                    else
                    {
                        evt.Summary += " - Keine Eintragungen";
                    }
                }

                evt.Description = date.Date.Description;


                // Die Endung Z macht, dass die Daten im Google-Calender richtig angezeigt werden
                //evt.DtStart = new Ical.Net.CalDateTime(date.Date.Begin.ToUniversalTime().ToString(@"yyyyMMdd\THHmmss\Z"));
                //evt.DtEnd = new Ical.Net.CalDateTime(date.Date.End.ToUniversalTime().ToString(@"yyyyMMdd\THHmmss\Z"));
                if (date.Slot != null)
                {
                    evt.Start = new CalDateTime(date.Slot.Begin);
                    evt.End = new CalDateTime(date.Slot.End);
                }
                else
                {
                    evt.Start = new CalDateTime(date.Date.Begin);
                    evt.End = new CalDateTime(date.Date.End);
                }

                evt.Status = (date.Date.Occurrence != null && date.Date.Occurrence.IsCanceled) ? EventStatus.Cancelled : EventStatus.Confirmed;

                var sb = new StringBuilder();
                foreach (var room in date.Date.Rooms)
                {
                    sb.Append(room.Number);
                    if (room != date.Date.Rooms.Last())
                        sb.Append(", ");
                }

                evt.Location = sb.ToString();
                evt.Categories.Add(date.Controller);
                evt.IsAllDay = false;
            }


            var contentType = "text/calendar";
            var serializer = new CalendarSerializer(new SerializationContext());
            string output = serializer.SerializeToString(iCal);
            var bytes = Encoding.UTF8.GetBytes(output);

            return File(bytes, contentType, "nine.ics");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public FileResult Feed(string token)
        {
            var user = UserManager.FindById(token);

            if (user != null)
            {
                var logger = LogManager.GetLogger("iCal");
                logger.DebugFormat("Feed for [{0}]", user.UserName);
                return GetCalendarData(user);
            }

            return null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult GroupPrintPlan(string start, string end, Guid? id)
        {
            var startDate = GetDateTime(start);
            var endDate = GetDateTime(end);

            var events = new List<CalendarEventModel>();

            // start und end sind "echte" Daten, d.h. eine Woche

            // Am Schluss muss alles in "Wochentage" umgerechnet werden

            // Alle Events abstrakt nah Wochentag
            var semester = SemesterService.GetSemester(id);
            var member = GetMyMembership();
            var user = GetCurrentUser();

            var courses = new List<Course>();


            // Alle Vorlesungen, der Semestergruppe
            var mylecture =
                Db.Activities.OfType<Course>()
                    .Where(c => c.SemesterGroups.Any(g => g.Id == id))
                    .ToList();
            courses.AddRange(mylecture);


            foreach (var course in courses)
            {
                var days = (from occ in course.Dates
                            select
                                new
                                {
                                    Day = occ.Begin.DayOfWeek,
                                    Begin = occ.Begin.TimeOfDay,
                                    End = occ.End.TimeOfDay,
                                }).Distinct().ToList();

                var eventViewModel = new CalenderEventPrintViewModel();
                eventViewModel.Course = course;
                eventViewModel.Rooms = Db.Rooms.Where(x => x.Dates.Any(y => y.Activity.Id == course.Id)).Distinct().ToList();


                if (days.Count == 1)
                {
                    var day = days.First();

                    var calDay = startDate.AddDays((int)day.Day - (int)startDate.DayOfWeek);
                    var calBegin = calDay.Add(day.Begin);
                    var calEnd = calDay.Add(day.End);

                    // Einfacher Eintrag
                    events.Add(new CalendarEventModel
                    {
                        title = course.Name,
                        allDay = false,
                        start = calBegin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        end = calEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        textColor = "#000",
                        backgroundColor = "#fff",
                        borderColor = "#000",
                        //htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                        //htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                        htmlContent = this.RenderViewToString("_CalendarEventContentPrint", eventViewModel),
                    });

                }
                else
                {
                    foreach (var day in days)
                    {
                        var calDay = startDate.AddDays((int)day.Day - (int)startDate.DayOfWeek);
                        var calBegin = calDay.Add(day.Begin);
                        var calEnd = calDay.Add(day.End);

                        // Einfacher Eintrag
                        events.Add(new CalendarEventModel
                        {
                            title = course.Name,
                            allDay = false,
                            start = calBegin.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            end = calEnd.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                            textColor = "#000",
                            backgroundColor = "#fff",
                            borderColor = "#000",
                            //htmlToolbarInfo = this.RenderViewToString("_CalendarEventToolbarInfo", eventViewModel),
                            //htmlToolbar = this.RenderViewToString("_CalendarEventToolbar", eventViewModel),
                            htmlContent = this.RenderViewToString("_CalendarEventContentPrint", eventViewModel),
                        });
                    }
                }
            }



            return Json(events);
        }

    }
}