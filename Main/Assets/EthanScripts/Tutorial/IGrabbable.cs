using UnityEngine;

public interface IGrabbable
{
    void OnGrab(Transform grabAnchor);
    void OnRelease();
}
