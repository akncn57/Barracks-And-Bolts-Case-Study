namespace BarracksAndBolts.UI.Common
{
    /// <summary>A panel that Esc can close.</summary>
    public interface ICloseablePanel
    {
        bool IsOpen { get; }
        void Close();
    }
}
