using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShowRayOnUIHover : MonoBehaviour
{
    [Header("Interactor Source")]
    [SerializeField] private Component rayInteractor;

    [Header("Visuals To Toggle")]
    [SerializeField] private Behaviour[] visualBehaviours;
    [SerializeField] private GameObject[] visualObjects;

    private PropertyInfo isUIHitClosestProperty;
    private MethodInfo tryGetCurrentUIRaycastResultMethod;

    private bool isVisible;

    private void Awake()
    {
        if (rayInteractor == null)
        {
            rayInteractor = GetComponent("XRRayInteractor");
        }

        CacheReflectionMembers();
        SetVisualState(false);
    }

    private void Update()
    {
        bool shouldShow = IsHoveringUI();
        SetVisualState(shouldShow);
    }

    private bool IsHoveringUI()
    {
        if (rayInteractor != null)
        {
            if (isUIHitClosestProperty != null)
            {
                object value = isUIHitClosestProperty.GetValue(rayInteractor);
                if (value is bool boolValue)
                {
                    return boolValue;
                }
            }

            if (tryGetCurrentUIRaycastResultMethod != null)
            {
                try
                {
                    ParameterInfo[] parameters = tryGetCurrentUIRaycastResultMethod.GetParameters();
                    object[] args = new object[parameters.Length];

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType.IsByRef)
                        {
                            Type elementType = parameterType.GetElementType();
                            args[i] = elementType != null && elementType.IsValueType
                                ? Activator.CreateInstance(elementType)
                                : null;
                        }
                        else
                        {
                            args[i] = null;
                        }
                    }

                    object result = tryGetCurrentUIRaycastResultMethod.Invoke(rayInteractor, args);
                    if (result is bool boolResult)
                    {
                        return boolResult;
                    }
                }
                catch
                {
                }
            }
        }

        if (EventSystem.current != null)
        {
            return EventSystem.current.IsPointerOverGameObject();
        }

        return false;
    }

    private void CacheReflectionMembers()
    {
        if (rayInteractor == null)
        {
            return;
        }

        Type interactorType = rayInteractor.GetType();

        isUIHitClosestProperty = interactorType.GetProperty(
            "isUIHitClosest",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        tryGetCurrentUIRaycastResultMethod = interactorType.GetMethod(
            "TryGetCurrentUIRaycastResult",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
    }

    private void SetVisualState(bool visible)
    {
        if (isVisible == visible)
        {
            return;
        }

        isVisible = visible;

        if (visualBehaviours != null)
        {
            for (int i = 0; i < visualBehaviours.Length; i++)
            {
                if (visualBehaviours[i] != null)
                {
                    visualBehaviours[i].enabled = visible;
                }
            }
        }

        if (visualObjects != null)
        {
            for (int i = 0; i < visualObjects.Length; i++)
            {
                if (visualObjects[i] != null)
                {
                    visualObjects[i].SetActive(visible);
                }
            }
        }
    }
}
