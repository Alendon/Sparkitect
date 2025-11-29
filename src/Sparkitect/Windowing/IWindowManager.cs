using Silk.NET.Windowing;

namespace Sparkitect.Windowing;

public interface IWindowManager
{
    IWindow? Window { get; }
    bool IsOpen { get; }

    void CreateWindow(string title, int width, int height);
    void PollEvents();
    void Close();

    IReadOnlyList<string> GetRequiredVulkanExtensions();
}