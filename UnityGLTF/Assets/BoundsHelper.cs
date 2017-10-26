using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class BoundsHelper
{
    public static Bounds GetGameObjectHierarchyBounds(GameObject go, Vector3 center)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();

        return EncapsulateBounds(renderers, center);
    }

    public static Bounds GetGameObjectListBounds(List<GameObject> gameObjects, Vector3 center)
    {
        Renderer[] renderers = gameObjects.Select(x => x.GetComponent<Renderer>()).ToArray();

        return EncapsulateBounds(renderers, center);
    }

    private static Bounds EncapsulateBounds(Renderer[] renderers, Vector3 center)
    {
        Bounds bounds = new Bounds(center, Vector3.one);

        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }
}
