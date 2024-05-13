using System;
using System.Linq;
using BepInEx;
using BepInEx;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using HarmonyLib;
using Libplanet.Crypto;
using Libplanet.Types.Tx;
using Nekoyume;
using Nekoyume.Multiplanetary;
using UniRx;
using UnityEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.EventSystems;

namespace NineChronicles.Mods.AutoArena
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AutoArenaPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "org.ninechronicles.mods.autoarena";
        private const string PluginName = "AutoArena";
        private const string PluginVersion = "0.1.0";

        internal static AutoArenaPlugin Instance { get; private set; }

        private Harmony _harmony;

        private bool _initialized = false;
        private bool _alreadyExecuted = false;

        public static void Log(LogLevel logLevel, object data)
        {
            Instance?.Logger.Log(logLevel, data);
        }

        public static void Log(object data) => Log(LogLevel.Info, data);

        public static void LogWarning(object data) => Log(LogLevel.Warning, data);

        public static void LogError(object data) => Log(LogLevel.Error, data);

        private void Awake()
        {
            if (Instance is not null)
            {
                throw new InvalidOperationException(
                    $"{nameof(AutoArenaPlugin)} must be only one instance."
                );
            }

            Instance = this;

            InitializeAsync();
            Log("Loaded");
        }

        private async void InitializeAsync()
        {
            await UniTask.WaitUntil(() => Nekoyume.Game.Game.instance?.Agent != null);
            var game = Nekoyume.Game.Game.instance;

            await UniTask.WaitUntil(() => game.CurrentPlanetId.HasValue);
            var planetId = game.CurrentPlanetId.Value;
            await UniTask.WaitUntil(() => game.States?.CurrentAvatarState != null);
            var states = game.States;
            _initialized = true;
            Logger.LogInfo("Initialized");
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.A) && !_alreadyExecuted)
            {
                ExecuteAutoArena();
            }
        }

        private async void ExecuteAutoArena()
        {
            _alreadyExecuted = true;
            var main = new Main();
        }
    }
}
