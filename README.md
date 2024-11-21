# SelectChannel

SelectChannel is a simple .NET class library that allows to read from / write to multiple
`System.Threading.Channels.Channel<T>` concurrently.

### Examples

```csharep
// create two channels to read from

var ch1 = Channel.CreateUnbounded<int>();
var ch2 = Channel.CreateUnbounded<long>();

// create a second thread that writes something to ch1
await Task.Run(async () =>
{
    await Task.Delay(300);
    await ch1.Writer.WriteAsync(42);
});

// setup the read of multiple channels
var select = Select.Setup();
var ch1Case = select.Read(ch1.Reader);
var ch2Case = select.Read(ch2.Reader);
await select.Wait();

// check what channel received data
if (ch1Case.IsMatching) 
{
    Console.WriteLine("Received on ch1: {0}", ch1Case.Value);
}
else if (ch2Case.IsMatching) 
{
    Console.WriteLine("Received on ch2: {0}", ch2Case.Value);
}
```
