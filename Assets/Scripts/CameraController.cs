using UnityEngine;

public class CameraController : MonoBehaviour
{
    public new Camera camera;
    public float moveSpeed = 10f;
    public float zoomSpeed = 10f;
    public float minSize = 1f;
    public float maxSize = 100f;

    void Start()
    {
        camera = GetComponent<Camera>();
    }

    void Update()
    {
        float moveHorizontal = 0f;
        float moveVertical = 0f;

        if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
        {
            moveVertical = 1f;
        }
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            moveVertical = -1f;
        }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            moveHorizontal = -1f;
        }
        if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            moveHorizontal = 1f;
        }

        Vector3 movement = moveSpeed * (camera.orthographicSize * 50f / maxSize) * Time.deltaTime * new Vector3(moveHorizontal, moveVertical, 0.0f);
        transform.Translate(movement, Space.World);

        if (camera.orthographic)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll != 0)
            {
                Vector3 mouseWorldPositionBeforeZoom = camera.ScreenToWorldPoint(Input.mousePosition);

                camera.orthographicSize -= scroll * zoomSpeed;
                camera.orthographicSize = Mathf.Clamp(camera.orthographicSize, minSize, maxSize);

                Vector3 mouseWorldPositionAfterZoom = camera.ScreenToWorldPoint(Input.mousePosition);

                Vector3 difference = mouseWorldPositionBeforeZoom - mouseWorldPositionAfterZoom;
                camera.transform.position += difference;
            }
        }
    }
}
