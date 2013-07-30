﻿using System;
using NUnit.Framework;
using ScheduleWidget.ScheduledEvents;
using ScheduleWidget.Enums;
using ScheduleWidget.TemporalExpressions;

namespace ScheduleWidget.UnitTest
{
    [TestFixture]
    public class DailyTests
    {
        [Test]
        public void DailyEventTest1()
        {
            var aEvent = new Event()
            {
                ID = 1,
                Title = "Event 1",
                Frequency = 1,        // daily
                MonthlyInterval = 0,  // not applicable
                DaysOfWeek = 127      // every day of week
            };

            var schedule = new Schedule(aEvent);

            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 2, 10)));
            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 4, 29)));
            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 11, 17)));
        }

        [Test]
        public void DailyEventTest2()
        {
            var aEvent = new Event()
            {
                ID = 1,
                Title = "Event 2",
                RangeInYear = null,
                Frequency = 1,        // daily
                MonthlyInterval = 0,  // not applicable
                DaysOfWeek = 16       // Thursday
            };

            var schedule = new Schedule(aEvent);

            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 2, 14)));
            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 4, 25)));
            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 11, 7)));
        }

        [Test]
        public void DailyEventTest3()
        {
            var aEvent = new Event()
            {
                ID = 1,
                Title = "Event 3",
                FrequencyTypeOptions = FrequencyTypeEnum.Daily,
                DayInterval = 4,
                FirstDateTime = new DateTime(2013, 1, 3)
            };

            var schedule = new Schedule(aEvent);

            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 1, 7)));
            Assert.IsFalse(schedule.IsOccurring(new DateTime(2013, 1, 12)));
            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 2, 4)));
        }

        [Test]
        public void DailyEventTest4()
        {
            var holidays = new UnionTE();
            holidays.Add(new FixedHolidayTE(2, 4));

            var aEvent = new Event()
            {
                ID = 1,
                Title = "Event 4",
                FrequencyTypeOptions = FrequencyTypeEnum.Daily,
                DayInterval = 4,
                FirstDateTime = new DateTime(2013, 1, 3)
            };

            var schedule = new Schedule(aEvent, holidays);

            Assert.IsTrue(schedule.IsOccurring(new DateTime(2013, 1, 7)));
            Assert.IsFalse(schedule.IsOccurring(new DateTime(2013, 1, 13)));
            Assert.IsFalse(schedule.IsOccurring(new DateTime(2013, 2, 4)));
        }
    }
}
