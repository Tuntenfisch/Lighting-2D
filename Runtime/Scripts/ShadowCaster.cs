using Tuntenfisch.Lighting2D.Internal;
using UnityEngine;

namespace Tuntenfisch.Lighting2D
{
    [ExecuteAlways]
    [RequireComponent(typeof(SpriteRenderer))]
    public class ShadowCaster : MonoBehaviour
    {
        #region Public Properties
        public SpriteRenderer Renderer => m_renderer;
        #endregion

        #region Private Fields
        private SpriteRenderer m_renderer;
        #endregion

        #region Unity Events
        private void OnValidate()
        {
            Initialize();
        }

        private void OnEnable()
        {
            Initialize();
            ShadowCasterManager.Add(this);
        }

        private void OnDisable()
        {
            ShadowCasterManager.Remove(this);
        }
        #endregion

        #region Private Methods
        private void Initialize()
        {
            if (m_renderer == null)
            {
                m_renderer = GetComponent<SpriteRenderer>();
            }
        }
        #endregion
    }
}