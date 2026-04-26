using UnityEngine;

[CreateAssetMenu(fileName = "NewCampaign", menuName = "Campaign/BackgroundSet")]
public class BackgroundSet : ScriptableObject
{
    public string campaignName;

    [System.Serializable]
    public class LevelRange
    {
        public int startLevel;
        public int endLevel;
        public GameObject[] prefabs;
    }

    public LevelRange[] levelsRanges;

    public GameObject GetRandomPrefab(int level)
    {
        foreach (var range in levelsRanges)
        {
            if (level >= range.startLevel && level <= range.endLevel)
            {
                if (range.prefabs.Length == 0)
                {
                    return null;
                }

                return range.prefabs[Random.Range(0, range.prefabs.Length)];
            }
        }

        return null;
    }
}
