using UnityEngine;

public interface IGrabbable
{
    void OnGrab(Transform holder);
    void OnRelease();
}
