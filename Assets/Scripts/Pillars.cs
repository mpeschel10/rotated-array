using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Pillars : MonoBehaviour, PillarList
{
    float[] pillarHeights;
    public ClickToReveal[] hiddenPillars;
    public float pillarScale = 4;
    [SerializeField] Material winMaterial;

    TMP_Text costText;
    GameObject fireworkBattery;
    int cost;
    
    [SerializeField] int pillarCount = 20;
    // Start is called before the first frame update
    void Start()
    {
        GameObject canonPillar = transform.GetChild(0).gameObject;
        costText = transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<TMP_Text>();
        fireworkBattery = transform.GetChild(2).gameObject;
        
        pillarHeights = new float[pillarCount];
        for (int i = 0; i < pillarCount; i++) pillarHeights[i] = float.NaN;
        
        cost = 0; won = false;
        UpdateCostText();
        
        canonPillar.SetActive(false);
        Vector3 ft = canonPillar.transform.position;
        Quaternion fr = canonPillar.transform.rotation;
        Vector3 pillarOffset = new Vector3(0, 0, -1);

        hiddenPillars = new ClickToReveal[pillarHeights.Length];
        for (int i = 0; i < pillarHeights.Length; i++)
        {
            GameObject newObject = Object.Instantiate(canonPillar, ft + pillarOffset * i, fr, transform);
            
            TMP_Text text = newObject.GetComponentInChildren<TMP_Text>();
            text.text = i.ToString();

            ClickToReveal hiddenPillar = newObject.GetComponentInChildren<ClickToReveal>();
            hiddenPillars[i] = hiddenPillar;
            hiddenPillar.index = i;
            
            newObject.SetActive(true);
        }
    }

    public void Increment() {
        cost++;
        UpdateCostText();
    }
    public void UpdateCostText() { costText.text = "Cost: " + cost; }

    bool InBounds(int i) { return i >= 0 && i < hiddenPillars.Length; }
    bool Revealed(int i) { return !hiddenPillars[i].gameObject.activeSelf; }

    bool won;
    public void CheckWin() {
        if (won) return;

        int[] winningCells = new int[] {-1, -1};
        
        if (Revealed(hiddenPillars.Length - 1) && Revealed(0) &&
            pillarHeights[0] < pillarHeights[pillarHeights.Length - 1])
        {
            winningCells[0] = 0;
            winningCells[1] = pillarHeights.Length - 1;
        } else {
            for (int i = 0; i < pillarHeights.Length - 1; i += 1)
            {
                if (!Revealed(i) || !Revealed(i + 1))
                    continue;
                if (pillarHeights[i] > pillarHeights[i + 1])
                {
                    winningCells[0] = i;
                    winningCells[1] = i + 1;
                    break;
                }
            }
        }
        
        if (winningCells[0] == -1)
        {
            if (cost == pillarHeights.Length)
            {
                winningCells = new int[pillarHeights.Length];
                for (int i = 0; i < winningCells.Length; i++)
                {
                    winningCells[i] = i;
                }
            } else {
                return;
            }
        }

        Debug.Log("A winner is you");
        won = true;
        foreach (int i in winningCells)
        {
            ClickToReveal hiddenPillar = hiddenPillars[i];
            hiddenPillar.visiblePillar.GetComponent<Renderer>().material = winMaterial;
        }

        ClickToReveal winningPillar = hiddenPillars[lastClick];
        Vector3 old = fireworkBattery.transform.localPosition;
        Vector3 new_ = new Vector3(winningPillar.transform.parent.localPosition.x, old.y, old.z);
        fireworkBattery.transform.localPosition = new_;
        SetFireworks(true);
        Invoke(nameof(StopFireworks), 7);
    }

    void StopFireworks() { Debug.Log("Stopping fireworks."); SetFireworks(false); }
    void SetFireworks(bool enabled)
    {
        foreach(ParticleSystem p in fireworkBattery.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.EmissionModule em = p.emission;
            em.enabled = enabled; // Why is this not a one-liner???
        }
    }

    bool hintPairs = false;
    public void SetHintPairs(bool active) { hintPairs = active; }
    int lastClick = -1;
    public void Click(int index)
    {
        lastClick = index;
        Collapse(index);
            
        if (hintPairs)
        {
            if (index > 0 &&
                hiddenPillars[index - 1].gameObject.activeSelf) // This check makes sure if left neighbor already selected, choose right instead.
            {
                Collapse(index - 1);
            }
            else if (index + 1 < hiddenPillars.Length)
            {
                Collapse(index - 1);
            }
        }
        CheckWin();
    }

    int Wrap(int index) { return (index % pillarCount + pillarCount) % pillarCount; }

    int SeekCollapsed(int index, int direction)
    {
        int startIndex = index;
        while (pillarHeights[Wrap(index)].Equals(float.NaN))
        {
            index += direction;
            if (Wrap(index) == startIndex)
            throw new System.Exception("pillarHeights appears to be entirely float.NaN. At least one pillar must be collapsed before calling SeekCollapsed.");
        }
        return index;
    }

    float InterpolateHeight(int leftRegionBoundary, int index, int rightRegionBoundary)
    {
        int horizontalSpan = rightRegionBoundary - leftRegionBoundary;
        float ratio = (index - leftRegionBoundary) / (float) horizontalSpan;
        float bottom = pillarHeights[Wrap(leftRegionBoundary)];
        float verticalSpan = pillarHeights[Wrap(rightRegionBoundary)] - bottom;
        return bottom + verticalSpan * ratio;
    }

    static float MIN_HEIGHT = 0.001f;
    static float MAX_HEIGHT = 1f;
    public void Collapse(int index)
    {
        float height = 1;
        if (cost == 0)
        {
            height = 0.5f;
        } else
        {
            int leftRegionBoundary = SeekCollapsed(index, -1);
            int rightRegionBoundary = SeekCollapsed(index, 1);
            float left = pillarHeights[Wrap(leftRegionBoundary)];
            float right = pillarHeights[Wrap(rightRegionBoundary)];
            
            if (left < right)
            {
                // We are not in the rotated part of the array; just a smooth upward tilt.
                // Make it look nice.
                height = InterpolateHeight(leftRegionBoundary, index, rightRegionBoundary);
            } else
            {
                int leftSpan = index - leftRegionBoundary;
                int rightSpan = rightRegionBoundary - index;
                if (leftSpan > rightSpan)
                {
                    Debug.Log("imbalance selecting left");
                    float ratio = (leftSpan - 1) / (rightRegionBoundary - leftRegionBoundary - 1f);
                    float verticalSpan = right - MIN_HEIGHT;
                    height = MIN_HEIGHT + verticalSpan * ratio;
                } else
                {
                    Debug.Log("imbalance selecting right");
                    Debug.Log("Region size: " + (rightRegionBoundary - leftRegionBoundary));
                    float ratio = (leftSpan) / (rightRegionBoundary - leftRegionBoundary - 1f);
                    float verticalSpan = MAX_HEIGHT - left;
                    height = left + verticalSpan * ratio;
                }
            }

        }
        
        Transform visibleTransform = hiddenPillars[index].visiblePillar.transform;
        Vector3 s = visibleTransform.localScale;
        visibleTransform.localScale = new Vector3(s.x, height * pillarScale, s.z);
        visibleTransform.localPosition = new Vector3(0.5f, height * pillarScale / 2, 0.5f);
        
        pillarHeights[index] = height;
        hiddenPillars[index].Reveal();
    }

    public void Reset()
    {
        foreach (ClickToReveal hiddenPillar in hiddenPillars)
        {
            Destroy(hiddenPillar.transform.parent.gameObject);
        }
        StopFireworks();

        Start();
    }
}
