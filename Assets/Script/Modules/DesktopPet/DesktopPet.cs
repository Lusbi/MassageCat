using UnityEngine;

public class DesktopPet : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float moveInterval = 2f; // 每次行動的間隔時間
    private Vector3 targetPosition;
    private float moveTimer;

    void Start()
    {
        SetRandomTargetPosition();
        moveTimer = moveInterval;
    }

    void Update()
    {
        moveTimer -= Time.deltaTime;
        if (moveTimer <= 0f)
        {
            SetRandomTargetPosition();
            moveTimer = moveInterval;
        }

        MoveTowardsTarget();
    }

    void SetRandomTargetPosition()
    {
        float x = Random.Range(-5f, 5f);
        targetPosition = new Vector3(x, transform.position.y, 0);
    }

    void MoveTowardsTarget()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            SetRandomTargetPosition();
        }
    }

    void OnMouseDown()
    {
        // 當點擊寵物時，執行一些互動行為
        Debug.Log("寵物被點擊了！");
    }
}
