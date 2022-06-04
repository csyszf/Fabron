using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fabron.Store;

public interface IStateStore2<TState>
{
    Task<string> SetAsync(TState state, string? expectedETag);
    Task<(TState? state, string? eTag)> GetAsync(string key);
    Task RemoveAsync(string key, string? expectedETag);
}

public abstract class InMemoryStateStore2<TState> : IStateStore2<TState>
{
    private readonly Dictionary<string, (TState state, string eTag)> _dict = new();

    public Task<(TState? state, string? eTag)> GetAsync(string key)
    {
        return Task.FromResult(_dict.TryGetValue(key, out var state) ? state : (default, default));
    }

    public Task RemoveAsync(string key, string? expectedETag)
    {
        _dict.Remove(key);
        return Task.CompletedTask;
    }

    public Task<string> SetAsync(TState state, string? expectedETag)
    {
        string key = GetStateKey(state);
        string newETag = Guid.NewGuid().ToString();
        _dict[key] = (state, newETag);
        return Task.FromResult(newETag);
    }

    protected abstract string GetStateKey(TState state);
}
