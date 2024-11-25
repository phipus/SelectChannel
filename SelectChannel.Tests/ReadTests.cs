using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;

namespace SelectChannel.Tests;

public class ReadTests
{
    [Fact]
    public async Task ReadFirstUnbounded()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        await Task.Run(async () =>
        {
            await Task.Delay(300);
            await ch1.Writer.WriteAsync(42);
        });

        // act
        var select = Select.Setup();
        var ch1Case = select.Read(ch1.Reader);
        var ch2Case = select.Read(ch2.Reader);
        await select.Wait();

        ch1Case.IsMatching.Should().BeTrue();
        ch1Case.Value.Should().Be(42);
        ch2Case.IsMatching.Should().BeFalse();
    }

    [Fact]
    public async Task CompletedFirstUnbounded()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        await Task.Run(async () =>
        {
            await Task.Delay(300);
            ch1.Writer.Complete();
        });

        // act
        var select = Select.Setup();
        var ch1Case = select.Read(ch1.Reader);
        var ch2Case = select.Read(ch2.Reader);
        await select.Wait();

        ch1Case.IsMatching.Should().BeTrue();
        ch1Case.TryGetValue(out _).Should().BeFalse();
        var getValue = () => ch1Case.Value;
        getValue.Should().Throw<ChannelClosedException>();
        ch2Case.IsMatching.Should().BeFalse();
    }

    [Fact]
    public async Task ReadComplete()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        ch1.Writer.Complete();

        // act
        var select = Select.Setup();
        var ch1Case = select.Read(ch1.Reader);
        var ch2Case = select.Read(ch2.Reader);
        await select.Wait();

        // assert
        ch1Case.IsMatching.Should().BeTrue();
        ch2Case.IsMatching.Should().BeFalse();
    }

    [Fact]
    public async Task ReadCompleteDefault()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        ch1.Writer.Complete();

        // act
        var select = Select.Setup();
        var ch1Case = select.Read(ch1.Reader);
        var ch2Case = select.Read(ch2.Reader);
        var defaultCase = select.DefaultCase();
        select.UseShuffle(false);
        await select.Wait();

        // assert
        ch1Case.IsMatching.Should().BeTrue();
        ch2Case.IsMatching.Should().BeFalse();
        defaultCase.IsMatching.Should().BeFalse();
    }

    [Fact]
    public async Task ReadNonMatching()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        ch1.Writer.Complete();

        // act
        var select = Select.Setup();
        select.Read(ch1.Reader);
        var ch2Case = select.Read(ch2.Reader);
        await select.Wait();

        // assert
        var getValue = () => ch2Case.Value;
        getValue.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadConcurrent()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        ch1.Writer.TryWrite(42);

        var gotValue = false;
        var wasCompleted = false;

        // act
        var task1 = Task.Run(SelectTask);
        var task2 = Task.Run(SelectTask);

        await Task.WhenAll(task1, task2);

        // assert
        gotValue.Should().BeTrue();
        wasCompleted.Should().BeTrue();
        return;

        async Task SelectTask()
        {
            var select = Select.Setup();
            var ch1Case = select.Read(ch1.Reader);
            var ch2Case = select.Read(ch2.Reader);
            await select.Wait();

            try
            {
                ch1Case.IsMatching.Should().BeTrue();
                ch2Case.IsMatching.Should().BeFalse();

                if (ch1Case.TryGetValue(out _))
                {
                    ch1.Writer.Complete();
                    gotValue = true;
                }
                else
                {
                    wasCompleted = true;
                }
            }
            catch (Exception)
            {
                ch2.Writer.Complete();
                throw;
            }
        }
    }

    [Fact]
    public async Task ReadConcurrentDefault()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        ch1.Writer.TryWrite(42);

        var gotValue = false;
        var wasDefault = false;

        // act
        var task1 = Task.Run(SelectTask);
        var task2 = Task.Run(SelectTask);

        await Task.WhenAll(task1, task2);

        // assert
        gotValue.Should().BeTrue();
        wasDefault.Should().BeTrue();
        return;

        async Task SelectTask()
        {
            var select = Select.Setup();
            var ch1Case = select.Read(ch1.Reader);
            var ch2Case = select.Read(ch2.Reader);
            var defaultCase = select.DefaultCase();
            await select.Wait();

            try
            {
                ch2Case.IsMatching.Should().BeFalse();

                if (ch1Case.IsMatching)
                {
                    gotValue = true;
                    defaultCase.IsMatching.Should().BeFalse();
                }
                else
                {
                    wasDefault = true;
                    defaultCase.IsMatching.Should().BeTrue();
                }
            }
            catch (Exception)
            {
                ch2.Writer.Complete();
                throw;
            }
        }
    }

    [Fact]
    public async Task ReadWithDefaultNoneReadable()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();

        // act
        var select = Select.Setup();
        var ch1Case = select.Read(ch1.Reader);
        var ch2Case = select.Read(ch2.Reader);
        var defaultCase = select.DefaultCase();
        await select.Wait();

        // assert
        ch1Case.IsMatching.Should().BeFalse();
        ch2Case.IsMatching.Should().BeFalse();
        defaultCase.IsMatching.Should().BeTrue();
    }

    [Fact]
    public async Task ReadWithDefaultReadable()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();
        var ch2 = Channel.CreateUnbounded<long>();
        ch1.Writer.TryWrite(42);

        // act
        var select = Select.Setup();
        var ch1Case = select.Read(ch1.Reader);
        var ch2Case = select.Read(ch2.Reader);
        var defaultCase = select.DefaultCase();
        await select.Wait();

        // assert
        ch1Case.IsMatching.Should().BeTrue();
        ch2Case.IsMatching.Should().BeFalse();
        defaultCase.IsMatching.Should().BeFalse();
    }

    [Fact]
    public void ReadNotReady()
    {
        // arrange
        var ch1 = Channel.CreateUnbounded<int>();

        // act
        var select = Select.Setup();
        var ch1Case = select.Read(ch1.Reader);
        var defaultCase = select.DefaultCase();

        // assert
        var checkCh1 = () => ch1Case.IsMatching;
        var checkDefault = () => defaultCase.IsMatching;
        checkCh1.Should().Throw<CaseNotReadyException>();
        checkDefault.Should().Throw<CaseNotReadyException>();
    }
}