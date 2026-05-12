namespace ElevenTube_Music
{
    public sealed class PluginContext
    {
        public PluginContext(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
        }

        public MainWindow MainWindow { get; }
    }
}
