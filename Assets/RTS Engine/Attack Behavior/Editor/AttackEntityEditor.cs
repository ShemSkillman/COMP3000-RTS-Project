using UnityEngine;
using UnityEditor;

using RTSEngine.EntityComponent;

[CustomEditor(typeof(UnitAttack))]
public class UnitAttackEditor : Editor
{
    SerializedObject SO;
    UnitAttack attackEntity;

    public void OnEnable()
    {
        attackEntity = (UnitAttack)target;
        SO = new SerializedObject(attackEntity);
    }

    public override void OnInspectorGUI()
    {
        AttackEntityEditor.OnInspectorGUI(SO, attackEntity, true);
    }
}

[CustomEditor(typeof(BuildingAttack))]
public class BuildingAttackEditor : Editor
{
    SerializedObject SO;
    BuildingAttack attackEntity;

    public void OnEnable()
    {
        attackEntity = (BuildingAttack)target;
        SO = new SerializedObject(attackEntity);
    }

    public override void OnInspectorGUI()
    {
        AttackEntityEditor.OnInspectorGUI(SO, attackEntity, false);
    }
}

#if UNITY_EDITOR
public static class AttackEntityEditor
{ 
    private static string[] upperToolbar = new string[] { "General", "Damage", "Weapon", "LOS" };
    private static string[] lowerToolbar = new string[] { "UI", "Audio", "Events" };

    public static void OnInspectorGUI(SerializedObject SO, AttackEntity attackEntity, bool unit)
    {
        SO.Update();

        EditorGUI.BeginChangeCheck();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal(); 
        for (int i = 0; i < upperToolbar.Length; i++)
        {
            GUI.enabled = !(attackEntity.tabID == i);
            if (GUILayout.Button(upperToolbar[i]))
                attackEntity.tabID = i;
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        for (int i = upperToolbar.Length; i < lowerToolbar.Length + upperToolbar.Length; i++)
        {
            GUI.enabled = !(attackEntity.tabID == i);
            if (GUILayout.Button(lowerToolbar[i - upperToolbar.Length]))
                attackEntity.tabID = i;
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        if (EditorGUI.EndChangeCheck())
        {
            SO.ApplyModifiedProperties();
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space();

        switch (attackEntity.tabID)
        {
            case 0:
                OnGeneralInspectorGUI(SO, unit);
                break;
            case 1:
                OnDamageInspectorGUI(SO);
                break;
            case 2:
                OnWeaponInspectorGUI(SO);
                break;
            case 3:
                OnLOSInspectorGUI(SO);
                break;
            case 4:
                OnUIInspectorGUI(SO);
                break;
            case 5:
                OnAudioInspectorGUI(SO);
                break;
            case 6:
                OnEventsInspectorGUI(SO);
                break;
            case 7:
                OnEventsInspectorGUI(SO);
                break;
        }

        SO.ApplyModifiedProperties();
    }

    public static void OnGeneralInspectorGUI(SerializedObject SO, bool unit)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("isLocked"));
        EditorGUILayout.PropertyField(SO.FindProperty("isActive"));
        EditorGUILayout.PropertyField(SO.FindProperty("code"));
        EditorGUILayout.PropertyField(SO.FindProperty("basic"));
        if (unit == true)
            EditorGUILayout.PropertyField(SO.FindProperty("range"), true);

        EditorGUILayout.Space();

        EditorGUILayout.PropertyField(SO.FindProperty("targetPicker"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("engageOnAssign"));
        EditorGUILayout.PropertyField(SO.FindProperty("engageWhenAttacked"));
        EditorGUILayout.PropertyField(SO.FindProperty("engageOnce"));
        EditorGUILayout.PropertyField(SO.FindProperty("engageFriendly"));
        if (unit == true)
            EditorGUILayout.PropertyField(SO.FindProperty("moveOnAttack"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("engageInRange"));
        if (SO.FindProperty("engageInRange").boolValue == true)
        {
            EditorGUILayout.PropertyField(SO.FindProperty("engageInRangeIdleOnly"));
            EditorGUILayout.PropertyField(SO.FindProperty("searchRange"));
            EditorGUILayout.PropertyField(SO.FindProperty("searchReload"));
        }
        if (unit == true)
            EditorGUILayout.PropertyField(SO.FindProperty("followDistance"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("requireTarget"));
        EditorGUILayout.PropertyField(SO.FindProperty("direct"));
        if (SO.FindProperty("direct").boolValue == false)
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackObjectLauncher.launchType"));
            EditorGUILayout.PropertyField(SO.FindProperty("attackObjectLauncher.sources"), true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("delayDuration"));
        EditorGUILayout.PropertyField(SO.FindProperty("delayTriggerEnabled"));
        if (unit == true)
        {
            EditorGUILayout.PropertyField(SO.FindProperty("attackAnimOverrideController"));
            EditorGUILayout.PropertyField(SO.FindProperty("triggerAnimationInDelay"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("useReload"));
        EditorGUILayout.PropertyField(SO.FindProperty("reloadDuration"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("coolDownEnabled"));
        EditorGUILayout.PropertyField(SO.FindProperty("coolDownDuration"));
    }

    public static void OnDamageInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.canDealDamage"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.unitDamage"));
        EditorGUILayout.PropertyField(SO.FindProperty("damage.buildingDamage"));
        EditorGUILayout.PropertyField(SO.FindProperty("damage.customDamages"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.areaAttackEnabled"));
        if (SO.FindProperty("damage.areaAttackEnabled").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("damage.attackRanges"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.dotEnabled"));
        if (SO.FindProperty("damage.dotEnabled").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("damage.dotAttributes"), new GUIContent("DoT Attributes"), true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("damage.effect"));
        EditorGUILayout.PropertyField(SO.FindProperty("damage.effectLifeTime"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("reloadDealtDamage"));
    }

    public static void OnWeaponInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.weaponObject"));
        if (SO.FindProperty("weapon.weaponObject").objectReferenceValue == null)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.rotateInRangeOnly"), new GUIContent("Rotate In Attack Range Only"));
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.smoothRotation"));
        if (SO.FindProperty("weapon.smoothRotation").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.rotationDamping"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationX"));
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationY"));
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.freezeRotationZ"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("weapon.forceIdleRotation"));
        if (SO.FindProperty("weapon.forceIdleRotation").boolValue == true)
            EditorGUILayout.PropertyField(SO.FindProperty("weapon.idleAngles"));
    }

    public static void OnLOSInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.enable"));
        if (SO.FindProperty("lineOfSight.enable").boolValue == false)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.useWeaponObject"));
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.LOSAngle"));
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.obstacleLayerMask"));

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationX"));
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationY"));
        EditorGUILayout.PropertyField(SO.FindProperty("lineOfSight.ignoreRotationZ"));
    }

    public static void OnUIInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("attackTaskUI"), true);
        EditorGUILayout.PropertyField(SO.FindProperty("switchTaskUI"), new GUIContent("Switch Attack Task UI"));
    }

    public static void OnAudioInspectorGUI(SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("orderAudio"), true);
        EditorGUILayout.PropertyField(SO.FindProperty("attackAudio"), true);
    }

    public static void OnEventsInspectorGUI (SerializedObject SO)
    {
        EditorGUILayout.PropertyField(SO.FindProperty("attackerInRangeEvent"));
        EditorGUILayout.PropertyField(SO.FindProperty("targetLockedEvent"));
        EditorGUILayout.PropertyField(SO.FindProperty("attackPerformedEvent"));
        EditorGUILayout.PropertyField(SO.FindProperty("attackDamageDealtEvent"));
    }
}
#endif
