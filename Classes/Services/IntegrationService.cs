using RePlays.Integrations;
using RePlays.Utils;
using System;

namespace RePlays.Services {
    public static class IntegrationService {
        private const string LEAGUE_OF_LEGENDS = "League of Legends";
        private const string PUBG = "PLAYERUNKNOWN'S BATTLEGROUNDS";
        private const string CS2 = "Counter-Strike 2";
        private const string CSGO = "Counter-Strike Global Offensive";
        private const string RAINBOW_SIX = "Tom Clancy's Rainbow Six Siege";
        private const string DEADLOCK = "Deadlock";
        private static Integration activeGameIntegration;
        public static Integration ActiveGameIntegration { get { return activeGameIntegration; } }
        public static async void Start(string gameName) {
            if (activeGameIntegration != null) {
                Logger.WriteLine("Active game integration already exists! Shutting down before starting");
                try {
                    await ActiveGameIntegration.Shutdown();
                }
                catch (Exception ex) {
                    Logger.WriteLine($"Previous game integration failed to shut down: {ex.Message}");
                }
            }
            switch (gameName) {
                case LEAGUE_OF_LEGENDS:
                    activeGameIntegration = new LeagueOfLegendsIntegration();
                    break;
                case PUBG:
                    activeGameIntegration = new PubgIntegration();
                    break;
                case RAINBOW_SIX:
                    activeGameIntegration = new RainbowSixIntegration();
                    break;
                case CSGO:
                case CS2:
                    activeGameIntegration = new CS2Integration();
                    break;
                case DEADLOCK:
                    activeGameIntegration = new DeadlockIntegration();
                    break;
                default:
                    activeGameIntegration = null;
                    break;
            }

            if (ActiveGameIntegration == null) return;
            Logger.WriteLine("Starting game integration");
            try {
                await ActiveGameIntegration.Start();
            }
            catch (Exception ex) {
                // this is async void, so an exception here (config folder missing, port already in use)
                // would take the whole app down instead of just skipping the integration
                Logger.WriteLine($"Game integration failed to start: {ex.Message}");
            }
        }

        public static async void Shutdown() {
            if (ActiveGameIntegration == null)
                return;
            Logger.WriteLine("Shutting down game integration");
            try {
                await ActiveGameIntegration.Shutdown();
            }
            catch (Exception ex) {
                Logger.WriteLine($"Game integration failed to shut down: {ex.Message}");
            }
            activeGameIntegration = null;
        }
    }
}