using System.Threading.Channels;

namespace SelectChannel;

public static class ChannelWriterExtensions
{
    public static bool IsCompleted<T>(this ChannelWriter<T> writer)
    {
        var task = writer.WaitToWriteAsync();
        // ReSharper disable once MergeIntoPattern
        return task.IsCompleted && !task.Result;
    }
}