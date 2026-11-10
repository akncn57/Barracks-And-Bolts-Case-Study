namespace BarracksAndBolts.UI.Common
{
    /// <summary>The window Esc opens when there is nothing else to close, and closes first when it is open.</summary>
    public interface IEscapeMenu
    {
        /// <returns>False if no window was open.</returns>
        bool TryCloseTop();

        void Open();
    }
}
