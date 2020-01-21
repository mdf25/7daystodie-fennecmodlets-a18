using UnityEngine;
using System.Reflection;
using DMT;
using Harmony;

/**
 * Harmony patch that allows the reading of more than the default tile entity types. 
 * Credit to SphereII for the amazing pacth.
 * Make sure to load this with the patch script else it will have an unknown type error when compiling.
 */

[HarmonyPatch(typeof(TileEntity))]
[HarmonyPatch("Instantiate")]
public class TileEntityInstantiator : IHarmony
{
    public void Start()
    {
        Debug.Log(" Loading Patch: " + GetType().ToString());
        var harmony = HarmonyInstance.Create(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    static TileEntity Postfix(TileEntity __result, TileEntityType type, Chunk _chunk)
    {
        if (__result == null)
        {
            switch (type)
            {
                case global::TileEntityType.BlockTransformer:
                    return new global::TileEntityBlockTransformer(_chunk);
            }
        }
        return __result;
    }
}