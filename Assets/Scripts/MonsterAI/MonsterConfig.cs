using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MonsterAI
{
    // ─────────────────────────────────────────────────────────────────
    // VisionConfig — ระยะการมองเห็น แสดงเป็นกรวย (Gizmos สีตาม state)
    // ─────────────────────────────────────────────────────────────────
    [Serializable]
    public class VisionConfig
    {
        [Tooltip("กว้าง(X) × สูง(Y) ของ cone")]
        public Vector2 Size   = new Vector2(5f, 3f);

        [Tooltip("offset จาก pivot (X คูณ facingDir อัตโนมัติ)")]
        public Vector2 Offset = Vector2.zero;

        [Tooltip("หมุน cone (องศา) จากแนวนอน — บวก = ขึ้น, ลบ = ลง")]
        public float Angle = 0f;

        // ── Helper: หมุน vector2 ตาม angle (radians) ─────────────────
        static Vector2 Rotate(Vector2 v, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        public bool Contains(Vector2 origin, Vector2 target, int facingDir)
        {
            Vector2 apex  = origin + new Vector2(Offset.x * facingDir, Offset.y);
            Vector2 delta = target - apex;

            // หมุน delta เข้า local space ของ cone (inverse rotation)
            // คูณ facingDir เพื่อ mirror angle ตามทิศ
            float rad    = Angle * Mathf.Deg2Rad * facingDir;
            Vector2 local = Rotate(delta, -rad);

            if (local.x * facingDir < 0f)           return false;   // อยู่ด้านหลัง
            if (Mathf.Abs(local.x) > Size.x)        return false;
            if (Mathf.Abs(local.y) > Size.y * 0.5f) return false;
            return true;
        }

#if UNITY_EDITOR
        public void DrawGizmos(Vector2 origin, int facingDir, Color color)
        {
            Vector2 apex = origin + new Vector2(Offset.x * facingDir, Offset.y);
            float   fx   = facingDir;
            float   rad  = Angle * Mathf.Deg2Rad * facingDir;

            // หมุน far corners ตาม angle
            Vector2 farT = apex + Rotate(new Vector2(Size.x * fx,  Size.y * 0.5f), rad);
            Vector2 farB = apex + Rotate(new Vector2(Size.x * fx, -Size.y * 0.5f), rad);

            // Fill
            Handles.color = new Color(color.r, color.g, color.b, 0.1f);
            Handles.DrawAAConvexPolygon(
                new Vector3(apex.x, apex.y, 0f),
                new Vector3(farT.x, farT.y, 0f),
                new Vector3(farB.x, farB.y, 0f));

            // Outline
            Handles.color = color;
            Handles.DrawLine(apex, farT);
            Handles.DrawLine(apex, farB);
            Handles.DrawLine(farT, farB);
        }
#endif
    }

    // ─────────────────────────────────────────────────────────────────
    // PatrolConfig
    // ─────────────────────────────────────────────────────────────────
    [Serializable]
    public class PatrolConfig
    {
        [Tooltip("ขนาด random range XY จาก pivot")]
        public Vector2 Range    = new Vector2(8f, 1f);

        [Tooltip("เวลารอ ณ จุดปลายทาง (วินาที)")]
        public float   WaitTime = 1.5f;

        [Tooltip("ความเร็ว Patrol  — ใช้เป็น baseSpeed ของ animator sync ด้วย")]
        public float   Speed    = 2f;
    }

    // ─────────────────────────────────────────────────────────────────
    // AttackRangeConfig — กล่องสี่เหลี่ยม (Gizmos เส้นประตรงกลาง)
    // ─────────────────────────────────────────────────────────────────
    [Serializable]
    public class AttackRangeConfig
    {
        [Tooltip("offset จาก pivot ของมอน (X คูณ facingDir)")]
        public Vector2 Offset = new Vector2(1f, 0f);

        [Tooltip("ขนาด XY ของ range")]
        public Vector2 Size   = new Vector2(2f, 1.5f);

        public bool Contains(Vector2 origin, Vector2 target, int facingDir)
        {
            Vector2 center = origin + new Vector2(Offset.x * facingDir, Offset.y);
            Vector2 delta  = target - center;
            return Mathf.Abs(delta.x) <= Size.x * 0.5f &&
                   Mathf.Abs(delta.y) <= Size.y * 0.5f;
        }

#if UNITY_EDITOR
        public void DrawGizmos(Vector2 origin, int facingDir, Color color)
        {
            Vector2 center = origin + new Vector2(Offset.x * facingDir, Offset.y);
            Rect    rect   = new Rect(
                center.x - Size.x * 0.5f,
                center.y - Size.y * 0.5f,
                Size.x, Size.y);

            // Fill + border
            Handles.color = new Color(color.r, color.g, color.b, 0.06f);
            Handles.DrawSolidRectangleWithOutline(rect,
                new Color(color.r, color.g, color.b, 0.06f), color);

            // Dashed center cross
            Vector3 c = new Vector3(center.x, center.y, 0f);
            Handles.color = new Color(color.r, color.g, color.b, 0.55f);
            Handles.DrawDottedLine(c - new Vector3(Size.x * 0.5f, 0f), c + new Vector3(Size.x * 0.5f, 0f), 4f);
            Handles.DrawDottedLine(c - new Vector3(0f, Size.y * 0.5f), c + new Vector3(0f, Size.y * 0.5f), 4f);
        }
#endif
    }
}
