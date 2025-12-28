using UnityEngine;

public interface IGrabbable
{
    void OnGrab(Transform holder);
    void MoveTo(Vector3 position);
    void OnRelease();
}
