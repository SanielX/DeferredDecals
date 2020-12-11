using System.Collections.Generic;

namespace HG.DeferredDecals
{
    [System.Serializable] // For debugging
    public class DeferredDecalSystem
    {
        public DeferredDecalSystem()
        {
            Instance = this;
        }

        public static DeferredDecalSystem Instance;

        internal List<int> availableLayers = new List<int>(50);
        internal Dictionary<int, List<Decal>> layerToDecals = new Dictionary<int, List<Decal>>(50);

        public void AddDecal(Decal d)
        {
            RemoveDecal(d, false);

            if(layerToDecals.TryGetValue(d.Layer, out List<Decal> decals))
            {
                decals.Add(d);
            } 
            else
            {
                List<Decal> newDecalList = new List<Decal>();
                layerToDecals[d.Layer] = newDecalList;
                
                newDecalList.Add(d);

                availableLayers.Add(d.Layer);
                availableLayers.Sort();
            }
        }

        public void RemoveDecal(Decal d, bool removeLayer = true)
        {
            foreach(var decalsPair in layerToDecals)
            {
                var decals = decalsPair.Value;
                decals.Remove(d);

                if(removeLayer && decals.Count == 0)
                {
                    availableLayers.Remove(d.Layer);
                }
            }
        }
    }
}