using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickToReveal : MonoBehaviour, FreeMovement.Hoverable, FreeMovement.Clickable
{
    public void Hover() { GetComponent<LayeredOutline>().AddLayer("can-click"); }
    public void Unhover() { GetComponent<LayeredOutline>().SubtractLayer("can-click"); }
    public GameObject GetGameObject() { return gameObject; }

    [SerializeField] public GameObject visiblePillar;
    public int index;
    public static bool revealPairs = false;
    PillarList GetPillars() { return transform.parent.parent.gameObject.GetComponent<PillarList>(); }
    public void Click()
    {
        GetPillars().Click(this.index);
    }

    public void Reveal()
    {
        if (gameObject.activeSelf)
        {
            GetPillars().Increment();
            gameObject.SetActive(false);
            visiblePillar.SetActive(true);
        }
    }
}
