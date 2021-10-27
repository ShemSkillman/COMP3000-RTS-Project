using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a replacement icon or a color to draw on the original icon of a task in the task panel in case its requirements are missing.
/// </summary>
[System.Serializable]
public struct MissingTaskRequirementData
{
    [SerializeField, Tooltip("If assigned, this icon is displayed instead of the original icon, when requirements to launch the task are missing.")]
    public Sprite icon;

    [SerializeField, Tooltip("Only if the above 'Icon' field is not assigned, this defines the color of the original icon of the task when requirements to launch the task are missing.")]
    public Color color;
}
