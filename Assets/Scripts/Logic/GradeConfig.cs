using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GradeConfig", menuName = "Lucky/GradeConfig")]
public class GradeConfig : ScriptableObject
{
    [Serializable]
    public struct GradeVisual
    {
        public HeroGrade Grade;
        public Sprite MainIcon;
        public Sprite SubIcon;
        public Color Color;
        public Color TrailColor;
    }

    [SerializeField] private GradeVisual[] _gradeVisuals;

    public bool TryGetGradeVisual(HeroGrade grade, out GradeVisual visual)
    {
        foreach (var gradeVisual in _gradeVisuals)
        {
            if (gradeVisual.Grade == grade)
            {
                visual = gradeVisual;
                return true;
            }
        }

        visual = default;
        return false;
    }
}
