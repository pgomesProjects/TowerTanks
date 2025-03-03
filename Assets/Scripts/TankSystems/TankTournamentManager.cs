using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TowerTanks.Scripts
{
    public class TankTournamentManager : MonoBehaviour
    {
        [SerializeField] private List<EnemyTankDesign> enemyTankPool = new List<EnemyTankDesign>();
        private TankController currentLeftTank;
        private TankController currentRightTank;
        private Vector2 leftTankSpawnPoint = new Vector2(-92, 16);
        private Vector2 rightTankSpawnPoint = new Vector2(92, 16);

        private IEnumerator Start()
        {
            while (enabled)
            {
                List<TextAsset> tankOrder = RandomTankOrderByTier(); // will be looped back to for a new tank pool every time the tournament is done
                while (tankOrder.Count > 0)
                {
                    if (currentLeftTank == null)
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
                    if (currentRightTank == null)
                    {
                        currentRightTank = TankManager.Instance.SpawnTank(tier: 1,
                                                                          typeToSpawn: TankId.TankType.ENEMY,
                                                                          true,
                                                                          true,
                                                                          tankOrder[0],
                                                                          rightTankSpawnPoint);
                        tankOrder.RemoveAt(0);
                    }
                    yield return new WaitUntil(() => currentLeftTank == null || currentRightTank == null);
                }

                yield return null;
            }
        }
        
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
