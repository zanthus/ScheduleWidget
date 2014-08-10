﻿using System;
using System.Collections.Generic;
using System.Linq;
using ScheduleWidget.ScheduledEvents.FrequencyBuilder;
using ScheduleWidget.TemporalExpressions;
using ScheduleWidget.Enums;

namespace ScheduleWidget.ScheduledEvents
{
    /// <summary>
    /// A schedule is a collection of one or more recurring events. It contains functionality to
    /// work out what event dates do or not not fall on the schedule's days (occurrences). This 
    /// schedule engine implements Martin Fowler's white paper "Recurring Events for Calendars" 
    /// (http://martinfowler.com/apsupp/recurring.pdf).
    /// </summary>
    public class Schedule : ISchedule
    {
        private readonly Event _event;
        public TemporalExpression TemporalExpression { get; private set; }
        public TemporalExpression TemporalExpressionIgnoringExclusions { get; private set; }

        public Schedule(Event aEvent)
        {
            _event = aEvent;
            TemporalExpression = Create();
            TemporalExpressionIgnoringExclusions = Create();
        }

        public Schedule(Event aEvent, IEnumerable<DateTime> excludedDates)
        {
            _event = aEvent;
            TemporalExpression = Create(excludedDates);
            TemporalExpressionIgnoringExclusions = Create();
        }

        public Schedule(Event aEvent, UnionTE excludedDates)
        {
            _event = aEvent;
            TemporalExpression = Create(excludedDates);
            TemporalExpressionIgnoringExclusions = Create();
        }

        /// <summary>
        /// Return the schedule's event
        /// </summary>
        public Event Event
        {
            get { return _event; }
        }

        /// <summary>
        /// Return true if the date occurs in the schedule.
        /// </summary>
        /// <param name="aDate"></param>
        /// <returns></returns>
        public bool IsOccurring(DateTime aDate)
        {
            if (!_event.DateIsWithinLimits(aDate))
                return false;
            return TemporalExpression.Includes(aDate);
        }

        /// <summary>
        /// Return true if the date occurs in the schedule, ignoring excluded dates.
        /// </summary>
        /// <param name="aDate"></param>
        /// <returns></returns>
        private bool IsOccurringIgnoringExcludedDates(DateTime aDate)
        {
            if (!_event.DateIsWithinLimits(aDate))
                return false;
            return TemporalExpressionIgnoringExclusions.Includes(aDate);
        }

        /// <summary>
        /// PreviousOccurrence(DateTime),
        /// Return the previous occurrence in the schedule for the given date.
        /// Returns null if nothing is found.
        /// Notes:
        /// This is not inclusive of the supplied date. Only earlier dates can be returned.
        /// This returned value will stay inside the event StartDateTime and EndDateTime.
        /// This function takes into account any excluded dates that were provided when the 
        /// schedule was created.
        /// </summary>
        /// <param name="aDate"></param>
        /// <returns></returns>
        public DateTime? PreviousOccurrence(DateTime aDate)
        {
            // Make sure that our search begins no later than the end of the event limits range.
            DateRange eventLimits = _event.GetEventLimitsAsDateRange();
            DateTime latestSearchStart = eventLimits.EndDateTime.SafeAddDays(1);
            if (aDate > latestSearchStart) { aDate = latestSearchStart; }
            // Get a working range for this search.
            var workingRange = DateRange(aDate, true);
            // Find the previous occurrence.
            var dates = Occurrences(workingRange).OrderByDescending(o => o.Date);
            DateTime? occurrence = dates.SkipWhile(o => o >= aDate.Date).FirstOrDefault();
            occurrence = (occurrence == default(DateTime)) ? null : occurrence;
            // Make sure that our result is no earlier than the start of the event limits range.
            if (occurrence != null && occurrence < eventLimits.StartDateTime) { occurrence = null; }
            return occurrence;
        }

        /// <summary>
        /// PreviousOccurrence(DateTime, DateRange),
        /// Return the previous occurrence in the schedule for the given date, from within the
        /// specified date range. Returns null if nothing is found.
        /// See PreviousOccurrence(DateTime) for additional details.
        /// </summary>
        public DateTime? PreviousOccurrence(DateTime aDate, DateRange during)
        {
            // Make sure that our search begins no later than the end of the during range.
            DateTime latestSearchStart = during.EndDateTime.SafeAddDays(1);
            if (aDate > latestSearchStart) { aDate = latestSearchStart; }
            // Perform the search.
            DateTime? occurrence = PreviousOccurrence(aDate);
            // Make sure that our result is no earlier than the start of the during range.
            if (occurrence != null && occurrence < during.StartDateTime)
                occurrence = null;
            return occurrence;
        }

        /// <summary>
        /// NextOccurrence(DateTime),
        /// Return the next occurrence in the schedule for the given date.
        /// Returns null if nothing is found.
        /// Notes:
        /// This is not inclusive of the supplied date. Only later dates can be returned.
        /// This returned value will stay inside the event StartDateTime and EndDateTime.
        /// This function takes into account any excluded dates that were provided when the 
        /// schedule was created.
        /// </summary>
        /// <param name="aDate"></param>
        /// <returns></returns>
        public DateTime? NextOccurrence(DateTime aDate)
        {
            // Make sure that our search begins no earlier than the start of the event limits range.
            DateRange eventLimits = _event.GetEventLimitsAsDateRange();
            DateTime earliestSearchStart = eventLimits.StartDateTime.SafeAddDays(-1);
            if (aDate < earliestSearchStart) { aDate = earliestSearchStart; }
            // Get a working range for this search.
            var during = DateRange(aDate, false);
            // Find the next occurrence.
            var dates = Occurrences(during);
            DateTime? occurrence = dates.SkipWhile(o => o.Date <= aDate.Date).FirstOrDefault();
            occurrence = (occurrence == default(DateTime)) ? null : occurrence;
            // Make sure that our result is no later than the end of the event limits range.
            if (occurrence != null && occurrence > eventLimits.EndDateTime) { occurrence = null; }
            return occurrence;
        }

        /// <summary>
        /// NextOccurrence(DateTime, DateRange),
        /// Return the next occurrence in the schedule for the given date, from within the
        /// specified date range. Returns null if nothing is found.
        /// See NextOccurrence(DateTime) for additional details.
        /// </summary>
        public DateTime? NextOccurrence(DateTime aDate, DateRange during)
        {
            // Make sure that our search begins no earlier than the beginning of the during range.
            DateTime earliestSearchStart = during.StartDateTime.SafeAddDays(-1);
            if (aDate < earliestSearchStart) { aDate = earliestSearchStart; }
            // Perform the search.
            DateTime? occurrence = NextOccurrence(aDate);
            // Make sure that our result is no later than the end of the during range.
            if (occurrence != null && occurrence > during.EndDateTime)
                occurrence = null;
            return occurrence;
        }

        /// <summary>
        /// Return all occurrences within the given date range.
        /// </summary>
        /// <param name="during">DateRange</param>
        /// <returns></returns>
        public IEnumerable<DateTime> Occurrences(DateRange during)
        {
            return EachDay(during.StartDateTime, during.EndDateTime).Where(IsOccurring);
        }

        /// <summary>
        /// Return all occurrences within the given date range, ignoring excluded dates.
        /// </summary>
        /// <param name="during">DateRange</param>
        /// <returns></returns>
        private IEnumerable<DateTime> OccurrencesIgnoringExcludedDates(DateRange during)
        {
            return EachDay(during.StartDateTime, during.EndDateTime).Where(IsOccurringIgnoringExcludedDates);
        }

        /// <summary>
        /// Create and return a base schedule with no exclusions.
        /// </summary>
        /// <returns></returns>
        private TemporalExpression Create()
        {
            var union = new UnionTE();
            return Create(union);
        }
        /// <summary>
        /// Create and return a base schedule including exclusions if applicable.
        /// </summary>
        /// <param name="excludedDates"></param>
        /// <returns></returns>
        private TemporalExpression Create(IEnumerable<DateTime> excludedDates)
        {
            var union = new UnionTE();
            if (excludedDates != null)
            {
                foreach (var date in excludedDates)
                {
                    union.Add(new DateTE(date));
                }
            }
            return Create(union);
        }

        /// <summary>
        /// Create and return a base schedule including exclusions if applicable.
        /// </summary>
        /// <param name="excludedDates">Holidays or any excluded dates</param>
        /// <returns>Complete schedule as an expression</returns>
        private TemporalExpression Create(TemporalExpression excludedDates)
        {
            var intersectionTE = new IntersectionTE();

            // get a builder that knows how to create a UnionTE for the event frequency
            var builder = EventFrequencyBuilder.Create(_event);
            var union = builder.Create();
            intersectionTE.Add(union);

            if (_event.RangeInYear != null)
            {
                var rangeEachYear = GetRangeForYear(_event);
                intersectionTE.Add(rangeEachYear);
            }

            return new DifferenceTE(intersectionTE, excludedDates);
        }

        private static RangeEachYearTE GetRangeForYear(Event aEvent)
        {
            if (aEvent.RangeInYear == null)
            {
                return null;
            }

            var startMonth = aEvent.RangeInYear.StartMonth;
            var endMonth = aEvent.RangeInYear.EndMonth;

            if (!aEvent.RangeInYear.StartDayOfMonth.HasValue)
            {
                return new RangeEachYearTE(startMonth, endMonth);
            }

            if (!aEvent.RangeInYear.EndDayOfMonth.HasValue)
            {
                return new RangeEachYearTE(startMonth, endMonth);
            }

            var startDay = aEvent.RangeInYear.StartDayOfMonth.Value;
            var endDay = aEvent.RangeInYear.EndDayOfMonth.Value;
            return new RangeEachYearTE(startMonth, endMonth, startDay, endDay);
        }

        /// <summary>
        /// Return each calendar day in the date range in ascending order
        /// </summary>
        /// <param name="from"></param>
        /// <param name="through"></param>
        /// <returns></returns>
        private static IEnumerable<DateTime> EachDay(DateTime from, DateTime through)
        {
            for (var day = from.Date; day.Date <= through.Date; day = day.AddDays(1))
                yield return day;
        }

        //An effective way to find date range especially when the interval is greater than one for any date frequencies 
        //(every x days, every x weeks, every x months, every x quarters or every x years).
        //NOTE: Quarterly is still not completely done as it is not supporting the interval (every n quarter(s)) feature right now.
        /// <summary>
        /// Return a date range to find either previous or next occurrence
        /// for a given date by evaluating some properties of the event
        /// </summary>
        /// <param name="aDate"></param>
        /// <param name="previousOccurrence"></param>
        /// <returns></returns>
        private DateRange DateRange(DateTime aDate, bool previousOccurrence)
        {
            if (_event.FrequencyTypeOptions == FrequencyTypeEnum.None)
                return new DateRange { StartDateTime = aDate, EndDateTime = aDate };

            int interval;
            DateRange dateRange = null;

            switch (_event.FrequencyTypeOptions)
            {
                case FrequencyTypeEnum.Daily:
                    interval = _event.RepeatInterval + 1;
                    dateRange = previousOccurrence
                                ? new DateRange { StartDateTime = aDate.AddDays(-interval), EndDateTime = aDate }
                                : new DateRange { StartDateTime = aDate, EndDateTime = aDate.AddDays(interval) };
                    break;
                case FrequencyTypeEnum.Weekly:
                case FrequencyTypeEnum.EveryWeekDay:
                case FrequencyTypeEnum.EveryMonWedFri:
                case FrequencyTypeEnum.EveryTuTh:
                    interval = (_event.RepeatInterval + 1) * 7;
                    dateRange = previousOccurrence
                                ? new DateRange { StartDateTime = aDate.AddDays(-interval), EndDateTime = aDate }
                                : new DateRange { StartDateTime = aDate, EndDateTime = aDate.AddDays(interval) };
                    break;
                case FrequencyTypeEnum.Monthly:
                    interval = _event.RepeatInterval + 1;
                    dateRange = previousOccurrence
                                ? new DateRange { StartDateTime = aDate.AddMonths(-interval), EndDateTime = aDate }
                                : new DateRange { StartDateTime = aDate, EndDateTime = aDate.AddMonths(interval) };
                    break;
                case FrequencyTypeEnum.Quarterly:
                    //Assign a default value as there is no interval option available for this frequency type now.
                    interval = 12;
                    dateRange = previousOccurrence
                                ? new DateRange { StartDateTime = aDate.AddMonths(-interval), EndDateTime = aDate }
                                : new DateRange { StartDateTime = aDate, EndDateTime = aDate.AddMonths(interval) };
                    break;
                case FrequencyTypeEnum.Yearly:
                    interval = _event.RepeatInterval + 1;
                    dateRange = previousOccurrence
                                ? new DateRange { StartDateTime = aDate.AddYears(-interval), EndDateTime = aDate }
                                : new DateRange { StartDateTime = aDate, EndDateTime = aDate.AddYears(interval) };
                    break;
            }

            return dateRange;
        }


        /// <summary>
        /// GetLastOccurrenceDate,
        /// Returns the date of last occurrence of the event.
        /// This method uses NumberOfOccurrences and EndDateTime properties to decide the last date.
        /// Only one of these properties should be set at any time, but if both are set, then the 
        /// most restrictive of the two properties will be used to determine the last date.
        /// 
        /// Dates that have been excluded in the Schedule constructor will not be included.
        /// If nothing is found, this will return null.
        /// </summary>
        /// <returns>The date of last occurrence of the event.</returns>
        public DateTime? GetLastOccurrenceDate()
        {
            DateTime? basedOnOccurrences = GetLastOccurrenceDateBasedOnlyOnNumberOfOccurrences();
            DateTime? basedOnEndDateTime = GetLastOccurrenceDateBasedOnlyOnEndDateTime();
            if (basedOnOccurrences == null && basedOnEndDateTime == null)
                return null;
            if (basedOnOccurrences != null && basedOnEndDateTime != null)
                return (basedOnOccurrences < basedOnEndDateTime) ? basedOnOccurrences : basedOnEndDateTime;
            if (basedOnOccurrences != null)
                return basedOnOccurrences;
            else
                return basedOnEndDateTime;
        }


        /// <summary>
        /// GetLastOccurrenceDateBasedOnlyOnNumberOfOccurrences,
        /// Returns the date of last occurrence of the event, based only on the NumberOfOccurrences property.
        /// If nothing is found, this will return null.
        /// </summary>
        /// <returns>The date of last occurrence of the event.</returns>
        private DateTime? GetLastOccurrenceDateBasedOnlyOnNumberOfOccurrences()
        {
            if (_event.StartDateTime == null) { return null; }
            DateTime startDateTime = (DateTime)_event.StartDateTime;
            FrequencyTypeEnum frequencyType = _event.FrequencyTypeOptions;
            if (frequencyType == FrequencyTypeEnum.None || !_event.NumberOfOccurrences.HasValue)
            {
                return startDateTime;
            }

            int interval;
            int occurences = _event.NumberOfOccurrences.Value;
            DateRange dateRange = null;

            switch (frequencyType)
            {
                case FrequencyTypeEnum.Daily:
                    interval = _event.RepeatInterval + 1;
                    dateRange = new DateRange { StartDateTime = startDateTime, EndDateTime = startDateTime.AddDays(interval * occurences) };
                    break;
                case FrequencyTypeEnum.Weekly:
                case FrequencyTypeEnum.EveryWeekDay:
                case FrequencyTypeEnum.EveryMonWedFri:
                case FrequencyTypeEnum.EveryTuTh:
                    interval = (_event.RepeatInterval + 1) * 7;
                    dateRange = new DateRange { StartDateTime = startDateTime, EndDateTime = startDateTime.AddDays(interval * occurences) };
                    break;
                case FrequencyTypeEnum.Monthly:
                    interval = _event.RepeatInterval + 1;
                    dateRange = new DateRange { StartDateTime = startDateTime, EndDateTime = startDateTime.AddMonths(interval * occurences) };
                    break;
                case FrequencyTypeEnum.Quarterly:
                    //Assign a default value as there is no interval option available for this frequency type now.
                    interval = 12;
                    dateRange = new DateRange { StartDateTime = startDateTime, EndDateTime = startDateTime.AddMonths(interval * occurences) };
                    break;
                case FrequencyTypeEnum.Yearly:
                    interval = _event.RepeatInterval + 1;
                    dateRange = new DateRange { StartDateTime = startDateTime, EndDateTime = startDateTime.AddYears(interval * occurences) };
                    break;
            }
            // Note: this section is designed to operate in such a way that excluding dates from an event that has
            // a fixed number of occurrences, will -not- increase the last occurrence date.
            // Find out when the last occurrence would be, if no dates were excluded.
            IEnumerable<DateTime> items = OccurrencesIgnoringExcludedDates(dateRange);
            if (items == null) return null;
            DateTime maximumDate = items.ElementAtOrDefault(occurences - 1);
            if (maximumDate == default(DateTime)) return null;
            // Find the actual last occurrence, using the previously found date as the maximum value.
            dateRange.EndDateTime = maximumDate;
            items = Occurrences(dateRange);
            if (items == null) return null;
            DateTime lastDate = items.ElementAtOrDefault(occurences - 1);
            if (lastDate == default(DateTime)) return null;
            return lastDate;
        }


        /// <summary>
        /// GetLastOccurrenceDateBasedOnlyOnEndDateTime,
        /// Returns the date of last occurrence of the event, based only on the EndDateTime property.
        /// If nothing is found, this will return null.
        /// </summary>
        /// <returns>The date of last occurrence of the event.</returns>
        private DateTime? GetLastOccurrenceDateBasedOnlyOnEndDateTime()
        {
            if (_event.StartDateTime == null || _event.EndDateTime == null) { return null; }
            DateTime startDateTime = (DateTime)_event.StartDateTime;
            DateTime endDateTime = (DateTime)_event.EndDateTime;
            FrequencyTypeEnum frequencyType = _event.FrequencyTypeOptions;
            if (frequencyType == FrequencyTypeEnum.None)
            {
                return startDateTime;
            }

            int occurences = _event.NumberOfOccurrences.Value;
            DateRange dateRange = new DateRange { StartDateTime = startDateTime, EndDateTime = endDateTime };

            IEnumerable<DateTime> items = Occurrences(dateRange);
            DateTime enddate = startDateTime;
            if (items != null)
            {
                DateTime foundDate = items.ElementAtOrDefault(occurences - 1);
                enddate = (foundDate == default(DateTime)) ? enddate : foundDate;
            }
            return enddate;
        }
    }
}
