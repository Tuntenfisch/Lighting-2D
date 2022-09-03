namespace Tuntenfisch.Lighting2D.Internal
{
    public interface ILight
    {
        public LightProperties GetLightProperties(bool update = false);
    }
}