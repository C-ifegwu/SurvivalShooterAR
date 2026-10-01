using UnityEngine;
namespace UnityEngine.XR.Templates.AR {
    public class ARTemplateMenuManager : MonoBehaviour {
        void Awake() {
            // Disabled to allow custom UIManager to take over the screen completely.
            Destroy(gameObject);
        }
    }
}