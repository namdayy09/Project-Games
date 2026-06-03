using UnityEngine;

public class FoodSpawner : MonoBehaviour
{
    public BoxCollider2D gridArea;

    void Start()
    {
        RandomizePosition();
    }

    public void RandomizePosition()
    {
        Bounds bounds = gridArea.bounds;

        float x = Mathf.Round(Random.Range(bounds.min.x, bounds.max.x));
        float y = Mathf.Round(Random.Range(bounds.min.y, bounds.max.y));

        transform.position = new Vector3(x, y, 0.0f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            SnakeController snake = other.GetComponent<SnakeController>();
            snake.Grow();
            RandomizePosition();
        }
    }
}