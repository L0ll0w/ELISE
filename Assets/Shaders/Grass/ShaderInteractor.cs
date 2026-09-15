using System.Collections.Generic;
using UnityEngine;
 
public class ShaderInteractor : MonoBehaviour
{
    public static readonly List<ShaderInteractor> ActiveInteractors = new List<ShaderInteractor>();

    public float radius = 1f;

    private void OnEnable()
    {
        if (!ActiveInteractors.Contains(this))
        {
            ActiveInteractors.Add(this);
        }
    }

    private void OnDisable()
    {
        ActiveInteractors.Remove(this);
    }
}