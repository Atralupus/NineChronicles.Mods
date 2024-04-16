﻿using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using Nekoyume;
using Nekoyume.Game;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.State;
using NineChronicles.Mods.Athena.Factories;
using NineChronicles.Mods.Athena.GUIs;
using NineChronicles.Mods.Athena.Managers;
using NineChronicles.Mods.Athena.Patches;
using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using Nekoyume.UI.Model;
using NineChronicles.Mods.Athena.Components;
using NineChronicles.Mods.Athena.Managers;
using NineChronicles.Mods.Athena.Models;
using NineChronicles.Modules.BlockSimulation.ActionSimulators;
using UnityEngine;


namespace NineChronicles.Mods.Athena
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class AthenaPlugin : BaseUnityPlugin
    {
        private const string PluginGUID = "org.ninechronicles.mods.athena";
        private const string PluginName = "Athena";
        private const string PluginVersion = "0.1.4";

        private const string PluginInstallationKey = PluginName + "_Installation";
        private const string PluginLastDayOfUseKey = PluginName + "_Last_Day_Of_Use";
        private const string PluginDailyOpenKey = PluginName + "_Daily_Open";

        internal static AthenaPlugin Instance { get; private set; }

        private Harmony _harmony;

        private ModInventoryManager _modInventoryManager;

        private AbilityRankingResponse _abilityRankingResponse = null;

        private Camera _mainCamera;
        private Color _mainCameraBackgroundColor;
        private int _mainCameraCullingMask;
        private EventSystem _eventSystem;

        // NOTE: Please add your GUIs here as alphabetical order.
        private AdventureGUI _adventureGUI;
        private ArenaGUI _arenaGUI;
        private EnhancementGUI _enhancementGUI;
        private ItemCreationGUI _itemCreationGUI;
        private InventoryGUI _inventoryGUI;
        private NotificationGUI _notificationGUI;
        private IGUI _tabGUI;

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
                throw new InvalidOperationException($"{nameof(AthenaPlugin)} must be only one instance.");
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll(typeof(AthenaPlugin));
            _harmony.PatchAll(typeof(BattlePreparationWidgetPatch));
            _modInventoryManager = new ModInventoryManager("../../mod_inventory.csv");

            Log("Loaded");
        }

        private void Start()
        {
            TrackInstallation();
            _eventSystem = FindObjectOfType<EventSystem>();
        }

        private async void LoadAbilityRanking()
        {
            var apiClient = Game.instance.ApiClient;

            if (apiClient.IsInitialized && _abilityRankingResponse == null)
            {
                var query =
                    $@"query {{
                            abilityRanking(limit: 1000) {{
                                ranking
                                avatarAddress
                                name
                                avatarLevel
                                armorId
                                titleId
                                cp
                            }}
                        }}";

                var response = await apiClient.GetObjectAsync<AbilityRankingResponse>(query);
                if (response is null)
                {
                    Log($"Failed getting response : {nameof(AbilityRankingResponse)}");
                }

                _abilityRankingResponse = response;
                Log($"AbilityRankingRequest Success {response.AbilityRanking.Count}");
            }
        }

        private async void TrackInstallation()
        {
            if (Analyzer.Instance is null)
            {
                await UniTask.WaitUntil(() => Analyzer.Instance is not null);
            }

            if (PlayerPrefs.GetInt(PluginInstallationKey, 0) == 0)
            {
                Analyzer.Instance.Track(PluginInstallationKey);
                PlayerPrefs.SetInt(PluginInstallationKey, 1);
            }
        }

        private async void TrackDailyOpen()
        {
            if (Analyzer.Instance is null)
            {
                await UniTask.WaitUntil(() => Analyzer.Instance is not null);
            }

            var days = (DateTime.Now - new DateTime(2019, 3, 11)).Days;
            if (days > PlayerPrefs.GetInt(PluginLastDayOfUseKey, 0))
            {
                Analyzer.Instance.Track(PluginDailyOpenKey);
                PlayerPrefs.SetInt(PluginLastDayOfUseKey, days);
            }
        }

        private void DisableModeGUI()
        {
            _adventureGUI = null;
            _arenaGUI = null;
            _enhancementGUI = null;
            _inventoryGUI = null;
            _itemCreationGUI = null;
            _notificationGUI = null;
            _tabGUI = null;
            EnableEventSystem();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Log("Escape key pressed.");
                DisableModeGUI();
            }

            if (_tabGUI is not null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Log("space key pressed.");

                var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Adventure];

                foreach (var equipmentId in itemSlotState.Equipments)
                {
                    var inventory = States.Instance.CurrentAvatarState?.inventory;

                    if (inventory.TryGetNonFungibleItem<Equipment>(equipmentId, out var equipment))
                    {
                        switch (equipment.ItemSubType)
                        {
                            case ItemSubType.Weapon:
                                _modInventoryManager.SelectedWeapon = equipment;
                                break;
                            case ItemSubType.Armor:
                                _modInventoryManager.SelectedArmor = equipment;
                                break;
                            case ItemSubType.Belt:
                                _modInventoryManager.SelectedBelt = equipment;
                                break;
                            case ItemSubType.Necklace:
                                _modInventoryManager.SelectedNecklace = equipment;
                                break;
                            case ItemSubType.Ring:
                                if (_modInventoryManager.SelectedRing1 == null)
                                {
                                    _modInventoryManager.SelectedRing1 = equipment;
                                }
                                else
                                {
                                    _modInventoryManager.SelectedRing2 = equipment;
                                }
                                break;
                            case ItemSubType.Aura:
                                _modInventoryManager.SelectedAura = equipment;
                                break;
                        }
                    }
                }

                _tabGUI = new TabGUI(new List<(string Name, Func<IGUI> UI)>
                {
                    ("Adventure", CreateAdventureGUI),
                    ("Arena", CreateArenaGUI),
                    ("Create", CreateItemCreationGUI),
                    ("Enhancement", CreateEnhancementGUI),
                }, DisableModeGUI);
                _notificationGUI = new NotificationGUI();

                TrackDailyOpen();
                DisableEventSystem();
                LoadAbilityRanking();
            }
        }

        private void DisableEventSystem()
        {
            if (_mainCamera is null)
            {
                _mainCamera = Camera.main;
                _mainCameraBackgroundColor = _mainCamera.backgroundColor;
                _mainCameraCullingMask = _mainCamera.cullingMask;
                _mainCamera.backgroundColor = Color.gray;
                _mainCamera.cullingMask = 0;
            }

            if (_eventSystem != null)
            {
                _eventSystem.enabled = false;
            }
        }

        private void EnableEventSystem()
        {
            if (_mainCamera)
            {
                _mainCamera.backgroundColor = _mainCameraBackgroundColor;
                _mainCamera.cullingMask = _mainCameraCullingMask;
                _mainCamera = null;
            }

            if (_eventSystem == null)
            {
                _eventSystem = FindObjectOfType<EventSystem>();
            }

            if (_eventSystem != null)
            {
                _eventSystem.enabled = true;
            }
        }

        private IGUI CreateArenaGUI()
        {
            return new ArenaGUI(_modInventoryManager, _abilityRankingResponse);
        }

        private IGUI CreateItemCreationGUI()
        {
            var tableSheets = TableSheets.Instance;
            var ui = new ItemCreationGUI(_modInventoryManager);
            ui.SetItemRecipes(
                tableSheets.EquipmentItemSheet,
                tableSheets.EquipmentItemRecipeSheet,
                tableSheets.EquipmentItemSubRecipeSheetV2,
                tableSheets.EquipmentItemOptionSheet);

            return ui;
        }

        private IGUI CreateEnhancementGUI()
        {
            var inventoryGUI = GetOrCreateInventoryGUI();
            return new EnhancementGUI(_modInventoryManager, inventoryGUI);
        }

        private IGUI CreateAdventureGUI()
        {
            return new AdventureGUI(_modInventoryManager);
        }

        private InventoryGUI GetOrCreateInventoryGUI()
        {
            if (_inventoryGUI != null)
            {
                _inventoryGUI.SelectTab(0);
                return _inventoryGUI;
            }

            _inventoryGUI = new InventoryGUI(
                    positionX: 100,
                    positionY: 80,
                    slotCountPerPage: 15,
                    slotCountPerRow: 5);
            _inventoryGUI.OnSlotRemoveClicked += item =>
            {
                if (item is Equipment equipment)
                {
                    _modInventoryManager.DeleteItem(equipment.NonFungibleId);
                }
            };

            var inventory = States.Instance.CurrentAvatarState?.inventory;
            if (inventory != null)
            {
                foreach (var equipment in inventory.Equipments)
                {
                    _inventoryGUI.AddOrReplaceItem(equipment, isExistsInBlockchain: true, isModded: false);
                }
            }

            var removeList = new List<Guid>();
            foreach (var modItem in _modInventoryManager.GetAllItems())
            {
                Equipment equipment;
                if (modItem.ExistsItem)
                {
                    if (inventory.TryGetNonFungibleItem(modItem.Id, out equipment))
                    {
                        equipment = ModItemFactory.ModifyLevel(
                            TableSheets.Instance,
                            equipment,
                            modItem);
                        _inventoryGUI.AddOrReplaceItem(equipment, isExistsInBlockchain: modItem.ExistsItem, isModded: true);
                        continue;
                    }

                    // NOTE: If here, it means that the item in the blockchain is not in the inventory anymore.
                    removeList.Add(modItem.Id);
                    continue;
                }

                equipment = ModItemFactory.CreateEquipmentWithModItem(TableSheets.Instance, modItem);
                if (equipment is null)
                {
                    continue;
                }

                _inventoryGUI.AddOrReplaceItem(equipment, isExistsInBlockchain: modItem.ExistsItem, isModded: true);
            }

            foreach (var id in removeList)
            {
                _modInventoryManager.DeleteItem(id);
            }

            _inventoryGUI.Sort();
            return _inventoryGUI;
        }

        private void OnGUI()
        {
            _adventureGUI?.OnGUI();
            _arenaGUI?.OnGUI();
            _enhancementGUI?.OnGUI();
            _itemCreationGUI?.OnGUI();
            _tabGUI?.OnGUI();
            _notificationGUI?.OnGUI();
        }
    }
}
