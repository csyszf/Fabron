using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using Fabron.CloudEvents;
using Fabron.Models;
using Fabron.Schedulers;
using Fabron.Store;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Runtime;
using Xunit;

namespace Fabron.Core.Test.SchedulerTests.CronSchedulerTests;

public class CronEventTickingTests
{
    private Fakes PrepareGrain(string? schedule = null, [CallerMemberName] string key = "Default")
    {
        var clock = new FakeSystemClock();
        var reminderRegistry = new FakeReminderRegistry();
        var timerRegistry = new FakeTimerRegistry();
        var context = A.Fake<IGrainContext>();
        var runtime = A.Fake<IGrainRuntime>();
        var store = A.Fake<ICronEventStore>();
        A.CallTo(() => context.GrainId)
            .Returns(GrainId.Create(nameof(CronEventScheduler), key));
        A.CallTo(() => runtime.ReminderRegistry).Returns(reminderRegistry);
        A.CallTo(() => runtime.TimerRegistry).Returns(timerRegistry);

        if (schedule is not null)
        {
            var state = new CronEvent
            {
                Metadata = new ScheduleMetadata
                {
                    Key = key,
                },
                Template = JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
                Spec = new CronEventSpec
                {
                    Schedule = schedule,
                }
            };
            A.CallTo(() => store.GetAsync(key)).Returns(Task.FromResult<StateEntry<CronEvent>?>(new(state, "0")));
        }

        var grain = new CronEventScheduler(
            context,
            runtime,
            A.Fake<ILogger<CronEventScheduler>>(),
            Options.Create(new CronSchedulerOptions { CronFormat = CronFormat.IncludeSeconds }),
            clock,
            store,
            A.Fake<IEventDispatcher>());

        return new(grain, timerRegistry, reminderRegistry, clock, store);
    }

    [Fact]
    public async Task ShouldSetTimersAndNextTick()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain("*/20 * * * * *");
        await (scheduler as IGrainBase).OnActivateAsync(default);
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        clock.UtcNow = tickTime.AddMilliseconds(100);
        await scheduler.Tick(tickTime);
        timerRegistry.Timers.Count.Should().Be(6);
        timerRegistry.Timers[0].DueTime.Should().Be(TimeSpan.Zero);
        timerRegistry.Timers[1].DueTime.Should().Be(tickTime.AddSeconds(20) - clock.UtcNow);
        timerRegistry.Timers[2].DueTime.Should().Be(tickTime.AddSeconds(40) - clock.UtcNow);
        timerRegistry.Timers[3].DueTime.Should().Be(tickTime.AddSeconds(60) - clock.UtcNow);
        timerRegistry.Timers[4].DueTime.Should().Be(tickTime.AddSeconds(80) - clock.UtcNow);
        timerRegistry.Timers[5].DueTime.Should().Be(tickTime.AddSeconds(100) - clock.UtcNow);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(tickTime.AddSeconds(120) - clock.UtcNow);
    }

    [Fact]
    public async Task ShouldDispatchForCurrentTick()
    {
        var (scheduler, timerRegistry, reminderRegistry, clock, _) = PrepareGrain("0 0 0 * * *");
        await (scheduler as IGrainBase).OnActivateAsync(default);
        var tickTime = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(20);
        await reminderRegistry.RegisterOrUpdateReminder(scheduler.GetGrainId(), Names.TickerReminder, TimeSpan.FromMilliseconds(10), Timeout.InfiniteTimeSpan);
        clock.UtcNow = tickTime.AddMilliseconds(100);
        await reminderRegistry.Fire(scheduler, Names.TickerReminder, TickStatus.Create(tickTime.UtcDateTime, Timeout.InfiniteTimeSpan, clock.UtcNow.UtcDateTime));
        timerRegistry.Timers.Should().HaveCount(1);
        timerRegistry.Timers[0].DueTime.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task ShouldRegisterNextTickOnSchedule()
    {
        var (scheduler, _, reminderRegistry, clock, _) = PrepareGrain();
        await (scheduler as IGrainBase).OnActivateAsync(default);
        clock.UtcNow = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).AddMilliseconds(20);
        await scheduler.Schedule(
            JsonSerializer.Serialize(new { data = new { foo = "bar" } }),
            new CronEventSpec { Schedule = "0 0 0 * * *" },
            null,
            null,
            null);
        reminderRegistry.Reminders.Should().HaveCount(1);
        reminderRegistry.Reminders.Single().Value.DueTime.Should().Be(new DateTimeOffset(2020, 1, 2, 0, 0, 0, TimeSpan.Zero) - clock.UtcNow);
    }
}

internal record Fakes(
    CronEventScheduler scheduler,
    FakeTimerRegistry timerRegistry,
    FakeReminderRegistry reminderRegistry,
    FakeSystemClock clock,
    ICronEventStore store);
