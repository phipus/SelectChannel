using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using FluentAssertions;

namespace SelectChannel.Tests;

public class WriteTests
{
    [Fact]
    public async Task WriteFirst()
    {
        // arrange
        var ch1 = Channel.CreateBounded<int>(1);
        var ch2 = Channel.CreateBounded<long>(1);
        ch1.Writer.TryWrite(10);

        // act
        var select = Select.Setup();
        var ch1Case = select.Write(ch1.Writer, 11);
        var ch2Case = select.Write(ch2.Writer, 42);
        await select.Wait();

        // assert
        ch1Case.IsMatching.Should().BeFalse();
        ch2Case.IsMatching.Should().BeTrue();
        ch2.Reader.TryRead(out var value).Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public async Task WriteFirstDefault()
    {
        // arrange
        var ch1 = Channel.CreateBounded<int>(1);
        var ch2 = Channel.CreateBounded<long>(1);
        ch1.Writer.TryWrite(10);
        ch2.Writer.TryWrite(11);

        // act
        var select = Select.Setup();
        var ch1Case = select.Write(ch1.Writer, 11);
        var ch2Case = select.Write(ch2.Writer, 42);
        var defaultCase = select.DefaultCase();
        await select.Wait();

        // assert
        ch1Case.IsMatching.Should().BeFalse();
        ch2Case.IsMatching.Should().BeFalse();
        defaultCase.IsMatching.Should().BeTrue();
    }
    
    [Fact]
    public async Task WriteFirstDefaultReady()
    {
        // arrange
        var ch1 = Channel.CreateBounded<int>(1);
        var ch2 = Channel.CreateBounded<long>(1);
        ch2.Writer.TryWrite(11);

        // act
        var select = Select.Setup();
        var ch1Case = select.Write(ch1.Writer, 11);
        var ch2Case = select.Write(ch2.Writer, 42);
        var defaultCase = select.DefaultCase();
        await select.Wait();

        // assert
        ch1Case.IsMatching.Should().BeTrue();
        ch2Case.IsMatching.Should().BeFalse();
        defaultCase.IsMatching.Should().BeFalse();
    }

    [Fact]
    public void CheckNonReady()
    {
        // arrange
        var ch1 = Channel.CreateBounded<int>(1);
        var ch2 = Channel.CreateBounded<long>(1);
        ch1.Writer.TryWrite(10);
        ch2.Writer.TryWrite(11);

        // act
        var select = Select.Setup();
        var ch1Case = select.Write(ch1.Writer, 11);
        var ch2Case = select.Write(ch2.Writer, 42);
        var defaultCase = select.DefaultCase();

        // assert
        var checkCh1Case = () => ch1Case.IsMatching;
        checkCh1Case.Should().Throw<CaseNotReadyException>();

        var checkCh2Case = () => ch2Case.IsMatching;
        checkCh2Case.Should().Throw<CaseNotReadyException>();

        var checkDefaultCase = () => defaultCase.IsMatching;
        checkDefaultCase.Should().Throw<CaseNotReadyException>();
    }

    [Fact]
    public async Task WriteConcurrent()
    {
        // arrange
        var ch1 = Channel.CreateBounded<int>(1);
        var ch2 = Channel.CreateBounded<long>(1);
        ch2.Writer.TryWrite(11);
        var ch1Written = false;
        var ch2Written = false;

        // act
        var task1 = Task.Run(DelayedWrite);
        var task2 = Task.Run(DelayedWrite);
        await Task.WhenAll(task1, task2);

        // assert
        ch1Written.Should().BeTrue();
        ch2Written.Should().BeTrue();

        return;

        async Task DelayedWrite()
        {
            try
            {
                await Task.Delay(100);

                var select = Select.Setup();
                var ch1Case = select.Write(ch1.Writer, 10);
                var ch2Case = select.Write(ch2.Writer, 15);
                await select.Wait();

                if (ch1Case.IsMatching)
                {
                    ch2.Reader.TryRead(out _).Should().BeTrue();
                    ch1Written = true;
                }
                else if (ch2Case.IsMatching)
                {
                    ch2Written = true;
                }
            }
            catch (Exception)
            {
                ch2.Reader.TryRead(out _);
                throw;
            }
        }
    }

    [Fact]
    public async Task WriteToClosed()
    {
        // arrange
        var ch1 = Channel.CreateBounded<int>(1);
        var ch2 = Channel.CreateBounded<long>(1);
        ch1.Writer.Complete();
        ch2.Writer.TryWrite(10);

        // act
        var select = Select.Setup();
        select.Write(ch1.Writer, 11);
        select.Write(ch2.Writer, 42);
        var wait = async () => await select.Wait();

        // assert
        await wait.Should().ThrowAsync<ChannelClosedException>();
    }
    
    [Fact]
    public async Task WriteToClosedDefault()
    {
        // arrange
        var ch1 = Channel.CreateBounded<int>(1);
        var ch2 = Channel.CreateBounded<long>(1);
        ch1.Writer.Complete();
        ch2.Writer.TryWrite(10);

        // act
        var select = Select.Setup();
        select.Write(ch1.Writer, 11);
        select.Write(ch2.Writer, 42);
        select.DefaultCase();
        var wait = async () => await select.Wait();

        // assert
        await wait.Should().ThrowAsync<ChannelClosedException>();
    }
}