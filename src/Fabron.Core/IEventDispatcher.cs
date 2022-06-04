using System;
using System.Threading.Tasks;

namespace Fabron;
public interface IEventDispatcher
{
    Task DispatchAsync(DateTimeOffset schedule, string data);
}
