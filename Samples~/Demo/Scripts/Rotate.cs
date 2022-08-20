using Unity.Mathematics;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    #region Inspector Fields
    [SerializeField]
    private float m_rotationSpeed = 30.0f;
    [SerializeField]
    private float2 m_offset;
    #endregion

    #region Private Fields
    private float3 m_position;
    #endregion

    #region Unity Events
    private void Start()
    {
        m_position = transform.position;
    }

    private void Update()
    {
        transform.RotateAround(m_position + new float3(m_offset, 0.0f), new float3(0.0f, 0.0f, 1.0f), m_rotationSpeed * Time.deltaTime);
    }
    #endregion
}
