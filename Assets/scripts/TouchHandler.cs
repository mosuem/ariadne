using System;
using UnityEngine;

public class TouchHandler
{
    internal int tapCount;
    private bool hasTouchedTouch;
    private bool hasTouchedMouse;

    internal bool hasTouched(int v = 1)
    {
        if (v == 2)
        {
            return tapCount == 2 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(1).fingerId);
        }
        tapCount = Input.touchCount;
        hasTouchedTouch = tapCount == 1 && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        hasTouchedMouse = (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0)) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        return hasTouchedTouch || hasTouchedMouse;
    }

    internal Touch GetTouch()
    {
        Touch touch1;
        if (hasTouchedTouch)
        {
            touch1 = Input.GetTouch(0);
        }
        else
        {
            touch1 = new Touch();
            if (Input.GetMouseButtonDown(0))
            {
                touch1.phase = TouchPhase.Began;
            }
            else if (Input.GetMouseButton(0))
            {
                touch1.phase = TouchPhase.Moved;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                touch1.phase = TouchPhase.Ended;
            }
            touch1.position = Input.mousePosition;
        }
        return touch1;
    }
}