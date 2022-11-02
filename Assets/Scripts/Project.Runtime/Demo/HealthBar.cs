using UnityEngine;
using UnityEngine.UI;

namespace Project.Runtime
{
    public class HealthBar : MonoBehaviour
    {
        [SerializeField] private Text health;

        public void SetHealth(int i)
        {
            health.text = i.ToString();
        }
    }
}