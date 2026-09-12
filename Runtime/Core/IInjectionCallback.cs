namespace StrataDI
{
    /// <summary>
    /// Optional callback invoked after all marked fields,
    /// properties, and methods have been injected.
    /// </summary>
    public interface IInjectionCallback
    {
        void OnInjected();
    }
}