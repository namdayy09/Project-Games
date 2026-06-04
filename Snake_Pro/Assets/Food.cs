using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    public BoxCollider2D gridArea;
    public FoodType currentType = FoodType.Normal;

    public enum FoodType
    {
        Normal,
        SpeedBoost,
        Slow
    }

    public void Prepare(Sprite normalSprite, Sprite fallbackSprite)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = normalSprite != null ? normalSprite : fallbackSprite;
        sr.sortingOrder = 7;
        transform.localScale = Vector3.one * 0.82f;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        if (col == null) col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
    }

    public void SetRandomType(Sprite normalSprite, Sprite fallbackSprite)
    {
        float r = Random.value;
        if (r < 0.70f) currentType = FoodType.Normal;
        else if (r < 0.86f) currentType = FoodType.SpeedBoost;
        else currentType = FoodType.Slow;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = normalSprite != null ? normalSprite : fallbackSprite;
        sr.sortingOrder = 7;

        if (currentType == FoodType.Normal)
        {
            sr.color = new Color(1f, 0.12f, 0.16f, 1f);
            transform.localScale = Vector3.one * 0.82f;
        }
        else if (currentType == FoodType.SpeedBoost)
        {
            sr.color = new Color(1f, 0.86f, 0.10f, 1f);
            transform.localScale = Vector3.one * 0.92f;
        }
        else
        {
            sr.color = new Color(0.15f, 0.55f, 1f, 1f);
            transform.localScale = Vector3.one * 0.88f;
        }
    }

    public void RandomizePosition()
    {
        if (gridArea == null) return;
        Bounds bounds = gridArea.bounds;
        float x = Mathf.Round(Random.Range(bounds.min.x + 1, bounds.max.x - 1));
        float y = Mathf.Round(Random.Range(bounds.min.y + 1, bounds.max.y - 1));
        transform.position = new Vector3(x, y, 0.0f);
    }
}
