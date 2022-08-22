using UnityEngine;

namespace Tuntenfisch.Lighting2D.Internal
{
    public interface IEntity
    {
        public EntityProperties GetProperties();

        public void SetMaterialProperties(MaterialPropertyBlock properties);
    }
}