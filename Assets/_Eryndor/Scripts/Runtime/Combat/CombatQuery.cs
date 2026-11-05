// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using UnityEngine;

namespace Eryndor.Runtime.Combat
{
    /// <summary>Vuruş hedeflerini bulan yardımcı. Fizik sorgusu tek yerde.</summary>
    public static class CombatQuery
    {
        private static readonly Collider[] Buffer = new Collider[32];

        /// <summary>
        /// origin'den range içinde, forward'a göre halfAngle derece açıdaki canlı rakipleri bulur.
        /// halfAngle 180 ise her yön (alan saldırısı).
        /// </summary>
        public static void FindTargets(Vector3 origin, Vector3 forward, float range, float halfAngle,
                                       Team attackerTeam, List<Combatant> results, int layerMask = ~0)
        {
            results.Clear();
            int count = Physics.OverlapSphereNonAlloc(origin, range, Buffer, layerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var c = Buffer[i].GetComponentInParent<Combatant>();
                if (c == null || !c.IsAlive || c.Team == attackerTeam || results.Contains(c)) continue;

                Vector3 to = c.transform.position - origin;
                to.y = 0f;
                if (halfAngle < 180f && to.sqrMagnitude > 0.01f &&
                    Vector3.Angle(new Vector3(forward.x, 0f, forward.z), to) > halfAngle) continue;

                results.Add(c);
            }
        }

        /// <summary>İki nokta arasında yatay mesafe.</summary>
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
