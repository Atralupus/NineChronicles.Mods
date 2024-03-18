﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Blockchain;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.Module;
using Nekoyume.State;
using Nekoyume.TableData;
using Debug = UnityEngine.Debug;

namespace NineChronicles.Mods.PVEHelper.BlockSimulation
{
    public static class WorldFactory
    {
        public static IWorld CreateWorld() => new World(new MockWorldState());

        public static async UniTask<IWorld> CreateWorldAsync(IAgent agent)
        {
            Debug.Log("CreateWorldAsync Start");
            var addresses = new[]
            {
                Addresses.Admin,
                Addresses.GoldCurrency,
            };
            var states = await agent.GetStateBulkAsync(ReservedAddresses.LegacyAccount, addresses);
            var world = CreateWorld()
                .WithAdmin(new AdminState((Dictionary)states[Addresses.Admin]))
                .WithGoldCurrency(new GoldCurrencyState((Dictionary)states[Addresses.GoldCurrency]));
            Debug.Log("CreateWorldAsync End");
            return world;
        }

        public static IWorld CreateWorld2(States states)
        {
            return CreateWorld()
                .WithAgent(states.AgentState)
                .WithAvatarState(states.CurrentAvatarState);
        }

        public static IWorld WithAdmin(this IWorld world, AdminState state) =>
            world.SetLegacyState(Addresses.Admin, state.Serialize());

        public static IWorld WithAdmin(this IWorld world, Address address) =>
            world.WithAdmin(new AdminState(address, long.MaxValue));

        public static IWorld WithAdmin(this IWorld world) =>
            world.WithAdmin(new PrivateKey().Address);

        public static IWorld WithGoldCurrency(this IWorld world, GoldCurrencyState state) =>
            world.SetLegacyState(state.address, state.Serialize());

        public static IWorld WithGoldCurrency(this IWorld world, Address[] minters, long amount)
        {
            var context = new ActionContext();
            var ncg = Currency.Legacy("NCG", 2, minters?.ToImmutableHashSet());
            var state = new GoldCurrencyState(ncg);
            return world
                .WithGoldCurrency(state)
                .MintAsset(context, state.address, state.Currency * amount);
        }

        public static IWorld WithGoldCurrency(this IWorld world) => world.WithGoldCurrency(null, 1_000_000_000L);

        public static IWorld WithSheets(this IWorld world, Dictionary<string, string> sheets, Dictionary<string, string> sheetsOverride)
        {
            foreach (var (key, value) in sheets)
            {
                var address = Addresses.TableSheet.Derive(key);
                world = world.SetLegacyState(address, value.Serialize());
            }

            return world;
        }

        public static IWorld WithSheetsInLocalCSV(this IWorld world, Dictionary<string, string> sheetsOverride)
        {
            var sheets = TableSheetsHelper.ImportSheets();
            if (sheetsOverride != null)
            {
                foreach (var (key, value) in sheetsOverride)
                {
                    sheets[key] = value;
                }
            }

            return world.WithSheets(sheets, null);
        }

        public static IWorld WithGameConfig(this IWorld world, GameConfigState gameConfigState)
        {
            return world.SetLegacyState(gameConfigState.address, gameConfigState.Serialize());
        }

        public static IWorld WithGameConfig(this IWorld world)
        {
            var gameConfigSheet = world.GetSheetCsv<GameConfigSheet>();
            var gameConfigState = new GameConfigState(gameConfigSheet);
            return world.WithGameConfig(gameConfigState);
        }

        public static IWorld WithAgent(this IWorld world, AgentState state) =>
            world.SetAgentState(state.address, state);

        public static IWorld WithAvatarState(this IWorld world, AvatarState state) =>
            world.SetAvatarState(state.address, state, true, true, true, true);
    }
}
