using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TowerTanks.Scripts
{
    public interface ITankGridItem
    {
        Vector2[] gridSlots { get; set; }
        void AddToGrid(Vector2 gridPos);

        /* NOTES: TankController needs to have data and functions for maintaining the grid.
         * GridSlots represents dimensions of item relative to its transform pivot
         * Every object that implements ITankGridItem needs to add itself to the relevant slots in its TankController grid
         * TankController needs to have a method to check if any given position is taken up by a grid item, then return that item if so
         * TankController needs a method for 
         */
    }

    public interface IDamageable
    {
        float Damage(Projectile projectile, Vector2 position);
        void Damage(float damage, bool triggerHitEffect = false);
    }
    public interface IBurnable
    {
        void Ignite();
        void BurnTick(float deltaTime);
    }
    public interface IImpactable
    {
        void HandleImpact(Vector2 force, Vector2 point);
    }
    public interface IMagnetizable
    {
        void ApplyMagnetForce(Vector2 force, Vector2 hitPoint);
    }
}
