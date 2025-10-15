using System;
using Bloodcraft.Services;

namespace Bloodcraft.Tests.Utilities;

/// <summary>
/// Provides disposable scopes for clearing player progression dictionaries while
/// suppressing persistence side effects during unit tests.
/// </summary>
public static class PlayerDataReset
{
    /// <summary>
    /// Clears leveling-related dictionaries alongside prestige tracking state.
    /// </summary>
    public static PlayerDataResetScope ForLeveling()
    {
        return new PlayerDataResetScope(resetLeveling: true, resetExpertise: false, resetBlood: false, resetPrestige: true);
    }

    /// <summary>
    /// Clears weapon expertise dictionaries alongside prestige tracking state.
    /// </summary>
    public static PlayerDataResetScope ForExpertise()
    {
        return new PlayerDataResetScope(resetLeveling: false, resetExpertise: true, resetBlood: false, resetPrestige: true);
    }

    /// <summary>
    /// Clears blood legacy dictionaries alongside prestige tracking state.
    /// </summary>
    public static PlayerDataResetScope ForBlood()
    {
        return new PlayerDataResetScope(resetLeveling: false, resetExpertise: false, resetBlood: true, resetPrestige: true);
    }

    /// <summary>
    /// Disposable scope that resets targeted player data collections before and after
    /// running a test while ensuring persistence is suppressed throughout the scope.
    /// </summary>
    public sealed class PlayerDataResetScope : IDisposable
    {
        readonly bool resetLeveling;
        readonly bool resetExpertise;
        readonly bool resetBlood;
        readonly bool resetPrestige;
        readonly IDisposable persistenceScope;

        internal PlayerDataResetScope(bool resetLeveling, bool resetExpertise, bool resetBlood, bool resetPrestige)
        {
            this.resetLeveling = resetLeveling;
            this.resetExpertise = resetExpertise;
            this.resetBlood = resetBlood;
            this.resetPrestige = resetPrestige;
            persistenceScope = DataService.SuppressPersistence();
            Reset();
        }

        public void Dispose()
        {
            Reset();
            persistenceScope.Dispose();
        }

        void Reset()
        {
            if (resetLeveling)
            {
                DataService.PlayerDictionaries._playerExperience.Clear();
                DataService.PlayerDictionaries._playerRestedXP.Clear();
            }

            if (resetExpertise)
            {
                DataService.PlayerDictionaries._playerSwordExpertise.Clear();
                DataService.PlayerDictionaries._playerAxeExpertise.Clear();
                DataService.PlayerDictionaries._playerMaceExpertise.Clear();
                DataService.PlayerDictionaries._playerSpearExpertise.Clear();
                DataService.PlayerDictionaries._playerCrossbowExpertise.Clear();
                DataService.PlayerDictionaries._playerGreatSwordExpertise.Clear();
                DataService.PlayerDictionaries._playerSlashersExpertise.Clear();
                DataService.PlayerDictionaries._playerPistolsExpertise.Clear();
                DataService.PlayerDictionaries._playerReaperExpertise.Clear();
                DataService.PlayerDictionaries._playerLongbowExpertise.Clear();
                DataService.PlayerDictionaries._playerWhipExpertise.Clear();
                DataService.PlayerDictionaries._playerFishingPoleExpertise.Clear();
                DataService.PlayerDictionaries._playerUnarmedExpertise.Clear();
                DataService.PlayerDictionaries._playerTwinBladesExpertise.Clear();
                DataService.PlayerDictionaries._playerDaggersExpertise.Clear();
                DataService.PlayerDictionaries._playerClawsExpertise.Clear();
                DataService.PlayerDictionaries._playerWeaponStats.Clear();
            }

            if (resetBlood)
            {
                DataService.PlayerDictionaries._playerWorkerLegacy.Clear();
                DataService.PlayerDictionaries._playerWarriorLegacy.Clear();
                DataService.PlayerDictionaries._playerScholarLegacy.Clear();
                DataService.PlayerDictionaries._playerRogueLegacy.Clear();
                DataService.PlayerDictionaries._playerMutantLegacy.Clear();
                DataService.PlayerDictionaries._playerVBloodLegacy.Clear();
                DataService.PlayerDictionaries._playerDraculinLegacy.Clear();
                DataService.PlayerDictionaries._playerImmortalLegacy.Clear();
                DataService.PlayerDictionaries._playerCreatureLegacy.Clear();
                DataService.PlayerDictionaries._playerBruteLegacy.Clear();
                DataService.PlayerDictionaries._playerCorruptionLegacy.Clear();
                DataService.PlayerDictionaries._playerBloodStats.Clear();
            }

            if (resetPrestige)
            {
                DataService.PlayerDictionaries._playerPrestiges.Clear();
            }
        }
    }
}
