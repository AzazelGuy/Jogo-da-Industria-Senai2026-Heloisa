using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// Anexe este script nos GameObjects das Peças ou Parafusos que precisam de Zoom.
    /// Crie um arquivo chamado EXACTAMENTE "FocusPoint.cs" na Unity.
    /// </summary>
   
    
    public class FocusPoint : MonoBehaviour
    {
        [Header("Configurações de Foco")]
        [Tooltip("Offset relativo à peça para onde a câmera deve ir.")]
        public Vector3 cameraOffset = new Vector3(0, 0.15f, -0.25f);

        [Tooltip("Rotação desejada da câmera ao focar nesta peça.")]
        public Vector3 targetEulerAngles = new Vector3(30f, 0f, 0f);

        [Tooltip("Campo de visão (FOV) para aproximar a imagem no parafuso/peça.")]
        public float focusedFOV = 35f;

        /// <summary>
        /// Calcula a posição final do mundo onde a câmera ficará posicionada.
        /// </summary>
        public Vector3 GetWorldTargetPosition()
        {
            return transform.position + transform.TransformDirection(cameraOffset);
        }

        /// <summary>
        /// Obtém a rotação no espaço global baseada no Euler definido.
        /// </summary>
        public Quaternion GetWorldTargetRotation()
        {
            return Quaternion.Euler(targetEulerAngles);
        }

        // Desenha uma esfera no Editor da Unity indicando para onde a câmera vai olhar
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 worldPos = GetWorldTargetPosition();
            Gizmos.DrawWireSphere(worldPos, 0.03f);
            Gizmos.DrawLine(transform.position, worldPos);
        }
    }