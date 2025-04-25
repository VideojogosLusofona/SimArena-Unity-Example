using SimToolAI.Core;
using SimToolAI.Core.Entities;
using SimToolAI.Core.Rendering.RenderStrategies;
using SimToolAI.Utilities;
using UnityEngine;

namespace Examples.Unity.Cosmetic
{
    public class RenderableBridge: MonoBehaviour
    {
        [SerializeField] private Transform transformToMove;
        [SerializeField] private Transform avatar;
        [SerializeField] private bool destroyable;

        public UnityEntityRenderable GetRenderable(Simulation simulation, Entity entity)
        {
            Data settings = new Data();
            settings.Set("transform", transformToMove);
            settings.Set("avatar", avatar);
            settings.Set("destroyable", destroyable);
            settings.Set("grid", FindAnyObjectByType<Grid>());
            settings.Set("scene", simulation.Scene);
            settings.Set("entity", entity);
            
            return new UnityEntityRenderable(settings);
        }
    }
}