using Rocket.API;

namespace GodModeArea
{
    public class GodModeAreaConfiguration : IRocketPluginConfiguration
    {
        public List<GodModeAreas> GodModeAreas = new();
        public int GodModeMillisecondsExitDelay = 5000;
        public bool GodModeDefaultValue = false;
        public bool VanillaGodMode = false;
        public bool DebugExtended = false;
        public void LoadDefaults()
        {
            GodModeAreas = new()
            {
                new()
                {
                    X1 = 431.41,
                    X2 = 440.92,
                    Y1 = 51.00,
                    Y2 = 54.00,
                    Z1 = 437.25,
                    Z2 = 447.05
                }
            };
        }
    }
    public class GodModeAreas
    {
        public double X1;
        public double Y1;
        public double Z1;
        public double X2;
        public double Y2;
        public double Z2;
    }
}
