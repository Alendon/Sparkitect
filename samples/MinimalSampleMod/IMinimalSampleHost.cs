namespace MinimalSampleMod;

public interface IMinimalSampleHost
{
    bool IsOpen { get; }

    void Initialize();

    void PollEvents();

    void RunFrame();

    void Shutdown();
}
