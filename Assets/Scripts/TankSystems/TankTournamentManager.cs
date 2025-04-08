using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TowerTanks.Scripts
{
    public class TankTournamentManager : MonoBehaviour
    {
        [SerializeField] private List<EnemyTankDesign> enemyTankPool = new List<EnemyTankDesign>();
        [Tooltip("The amount of seconds after a tank is destroyed to spawn a new one")]
        [SerializeField] private float spawnTankCooldown;
        private TankController currentLeftTank;
        private TankController currentRightTank;
        
        private Vector2 leftTankSpawnPoint = new (-85, 16);
        private Vector2 rightTankSpawnPoint = new (85, 16);

        private IEnumerator Start()
        {
            while (enabled)
            {
                List<TextAsset> tankOrder = RandomTankOrderByTier(); // will be looped back to for a new tank pool every time the tournament is done
                while (tankOrder.Count > 0)
                {
                    bool bothTanksAreDuds = BothTanksSurrendered(); //caches the value of both tanks surrendered,
                    //in order to spawn 2 more if both are surrendered. otherwise we could only spawn 1, because when
                    //we spawn the new left side tank, the current left side tank would no longer be surrendered
                    if (currentLeftTank == null || bothTanksAreDuds)
                    {
                        currentLeftTank = TankManager.Instance.SpawnTank(tier: 1, //it doesnt matter what is put for tier, because spawntank only uses tier for spawning tanks in the game scene anyways
                                                                         typeToSpawn:
                                                                         TankId.TankType.ENEMY,
                                                                         true,
                                                                         true,
                                                                         tankOrder[0],
                                                                         leftTankSpawnPoint);
                        tankOrder.RemoveAt(0);
                        yield return new WaitForSeconds(.1f);
                        currentLeftTank.FlipTankDesign();
                    }
                    if (currentRightTank == null || bothTanksAreDuds)
                    {
                        currentRightTank = TankManager.Instance.SpawnTank(tier: 1,
                                                                          typeToSpawn: TankId.TankType.ENEMY,
                                                                          true,
                                                                          true,
                                                                          tankOrder[0],
                                                                          rightTankSpawnPoint);
                        tankOrder.RemoveAt(0);
                    }
                    yield return new WaitUntil(() => currentLeftTank == null || currentRightTank == null ||
                    BothTanksSurrendered());
                    //waits until a new tank needs to be spawned. happens if either tank is destroyed,
                    //or if both tanks end up in a surrendered state
                    
                    yield return new WaitForSeconds(spawnTankCooldown);
                }

                yield return null;
            }
            
        }
        
        bool BothTanksSurrendered() =>
            currentLeftTank != null && currentRightTank != null &&
            currentLeftTank._thisTankAI?.fsm?._currentState.GetType() == typeof(TankSurrenderState) &&
            currentRightTank._thisTankAI?.fsm?._currentState.GetType() == typeof(TankSurrenderState);
        
        private List<T> ShuffleList<T>(List<T> array)
        {
            System.Random random = new System.Random();
            return array.OrderBy(x => random.Next()).ToList();
        }

        /// <summary>
        /// Returns a list of tank designs in a random order, sorted by tier. (4 random tier 1s, 4 random tier 2s, etc.)
        /// </summary>
        /// <returns></returns>
        private List<TextAsset> RandomTankOrderByTier()
        {
            List<TextAsset> tanksInOrder = new List<TextAsset>();
            foreach (var tankList in enemyTankPool)
            {
                var randomTanks = ShuffleList(tankList.designs);
                foreach (var tank in randomTanks)
                {
                    tanksInOrder.Add(tank);
                }
            }
            return tanksInOrder;
        }
    }
}
