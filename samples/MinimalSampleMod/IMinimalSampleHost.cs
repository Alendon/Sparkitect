namespace MinimalSampleMod;

public interface IMinimalSampleHost
{
    bool IsOpen { get; }

    void Initialize();

    void RunFrame();

    void Shutdown();
}
