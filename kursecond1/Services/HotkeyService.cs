using Microsoft.JSInterop;

namespace kursecond1.Services;

public class HotkeyService : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private readonly Dictionary<string, Action> _hotkeys = new();
    private DotNetObjectReference<HotkeyService>? _dotNetRef;

    public HotkeyService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        _dotNetRef = DotNetObjectReference.Create(this);
        await _jsRuntime.InvokeVoidAsync("hotkeyManager.initialize", _dotNetRef);
    }

    public void RegisterHotkey(string key, Action callback)
    {
        _hotkeys[key.ToLower()] = callback;
    }

    [JSInvokable]
    public void TriggerHotkey(string key)
    {
        if (_hotkeys.TryGetValue(key.ToLower(), out var callback))
        {
            callback?.Invoke();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_dotNetRef != null)
        {
            await _jsRuntime.InvokeVoidAsync("hotkeyManager.dispose");
            _dotNetRef.Dispose();
        }
    }
}

