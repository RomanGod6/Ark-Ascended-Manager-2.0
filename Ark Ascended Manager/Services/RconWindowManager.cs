using Ark_Ascended_Manager.ViewModels.Windows;
using Ark_Ascended_Manager.Views.Windows;

public class RconWindowManager
{
    private RconWindow _rconWindow;

    public void OpenRcon(RconViewModel viewModel)
    {
        if (_rconWindow == null || !_rconWindow.IsLoaded)
        {
            _rconWindow = new RconWindow();
            _rconWindow.Closed += (s, e) => _rconWindow = null;
            _rconWindow.Show();
        }

        _rconWindow.AddRconTab(viewModel);
    }

    public int GetOpenTabsCount()
    {
        return _rconWindow?.GetTabCount() ?? 0;
    }
}
