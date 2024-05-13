using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.TableData;

namespace NineChronicles.Mods.AutoArena
{
    public class Main
    {
        public Main()
        {
            var roundData = GetRoundData();

            AutoArenaPlugin.Log(
                $"[AutoArena] RoundData {roundData.ArenaType} {roundData.ChampionshipId} {roundData.Round}"
            );

            UpdateArenaRankings();
        }

        public ArenaSheet.RoundData GetRoundData()
        {
            return TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(
                Game.instance.Agent.BlockIndex
            );
        }

        public async void UpdateArenaRankings()
        {
            await RxProps.ArenaInformationOrderedWithScore.UpdateAsync();

            AutoArenaPlugin.Log(
                $"[AutoArena] Loading Complete. {RxProps.ArenaInformationOrderedWithScore.Value}"
            );
        }
    }
}
