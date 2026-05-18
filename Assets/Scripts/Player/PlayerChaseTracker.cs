using System.Collections.Generic;
using UnityEngine;

namespace DarkMazeMinimal
{
    public class PlayerChaseTracker : MonoBehaviour
    {
        private readonly HashSet<EnemyChaser> _chasers = new HashSet<EnemyChaser>();

        public bool IsBeingChased => _chasers.Count > 0;
        public int ChaserCount => _chasers.Count;

        public void RegisterChaser(EnemyChaser enemy)
        {
            if (enemy == null) return;

            _chasers.Add(enemy);
            Debug.Log($"[PlayerChaseTracker] Register: {enemy.name} | Count = {_chasers.Count}");
        }

        public void UnregisterChaser(EnemyChaser enemy)
        {
            if (enemy == null) return;

            _chasers.Remove(enemy);
            Debug.Log($"[PlayerChaseTracker] Unregister: {enemy.name} | Count = {_chasers.Count}");
        }

        public void ClearAll()
        {
            _chasers.Clear();
        }
    }
}