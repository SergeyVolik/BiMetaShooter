using System;
using UnityEngine;

namespace MFPS.PlayerController
{
    [Serializable]
    public class MouseLook
    {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;
        public bool clampVerticalRotation = true;
        public float MinimumX = -90F;
        public float MaximumX = 90F;
        public bool smooth;
        public float smoothTime = 5f;

        private Quaternion m_CharacterTargetRot;
        private Quaternion m_CameraTargetRot;
        private bl_GunManager GunManager;
        private bool InvertVertical;
        private bool InvertHorizontal;
        private Quaternion verticalOffset = Quaternion.identity;
        public bool onlyCameraTransform { get; set; } = false;
        private float verticalRotation, horizontalRotation;
        private Vector3 defaultCameraRotation;
        private Transform m_CameraTransform, m_CharacterBody;

        /// <summary>
        /// 
        /// </summary>
        public void Init(Transform character, Transform camera, bl_GunManager gm)
        {
            m_CameraTransform = camera;
            m_CharacterBody = character;
            m_CharacterTargetRot = character.localRotation;
            m_CameraTargetRot = camera.localRotation;
            GunManager = gm;
            XSensitivity = bl_RoomMenu.Instance.m_sensitive;
            YSensitivity = bl_RoomMenu.Instance.m_sensitive;
        }

        /// <summary>
        /// 
        /// </summary>
        public void LookRotation(Transform character, Transform camera, Transform ladder = null)
        {
#if MFPSM
            if (bl_UtilityHelper.isMobile)
            {
                CalculateSensitivity();
                CameraRotation(character, camera);
                return;
            }
#endif

            if (!bl_RoomMenu.Instance.isCursorLocked)
                return;

            CalculateSensitivity();

            if (ladder == null)
            {
                horizontalRotation = Input.GetAxis("Mouse X") * XSensitivity;
                horizontalRotation = (InvertHorizontal) ? (horizontalRotation * -1f) : horizontalRotation;
                m_CharacterTargetRot *= Quaternion.Euler(0f, horizontalRotation, 0f);
            }
            else
            {
                Vector3 direction = ladder.forward;
                direction.y = 0;
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                m_CharacterTargetRot = Quaternion.Slerp(m_CharacterTargetRot, lookRotation, Time.deltaTime * 5);
            }

            verticalRotation = Input.GetAxis("Mouse Y") * YSensitivity;
            verticalRotation = (InvertVertical) ? (verticalRotation * -1f) : verticalRotation;

            if (!onlyCameraTransform)
                m_CameraTargetRot *= Quaternion.Euler(-verticalRotation, 0f, 0f);
            else
                m_CameraTargetRot *= Quaternion.Euler(-verticalRotation, horizontalRotation, 0f);

            if (clampVerticalRotation)
            {
                m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);
            }

            if (smooth)
            {
                if (character != null && !onlyCameraTransform) { character.localRotation = Quaternion.Slerp(character.localRotation, m_CharacterTargetRot, smoothTime * Time.deltaTime); }
                if (camera != null) { camera.localRotation = Quaternion.Slerp(camera.localRotation, m_CameraTargetRot * verticalOffset, smoothTime * Time.deltaTime); }
            }
            else
            {
                if (!onlyCameraTransform) { character.localRotation = m_CharacterTargetRot; }
                camera.localRotation = m_CameraTargetRot * verticalOffset;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetVerticalOffset(float amount)
        {
            verticalOffset = Quaternion.Euler(amount, 0, 0);
        }

        /// <summary>
        /// 
        /// </summary>
        public void CombineVerticalOffset()
        {
            m_CameraTargetRot *= verticalOffset;
            verticalOffset = Quaternion.identity;
            if (clampVerticalRotation)
            {
                m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);
            }
        }

        /// <summary>
        /// Don't rotate the character body but only the Camera/Head
        /// </summary>
        public void UseOnlyCameraRotation()
        {
            onlyCameraTransform = true;
            defaultCameraRotation = m_CameraTransform.localEulerAngles;
        }

        /// <summary>
        /// Port the Current Camera Rotation to separate the vertical and horizontal rotation in the body and head
        /// horizontal rotation for the body and vertical for the camera/head
        /// That should only be called when OnlyCameraRotation was used before.
        /// </summary>
        public void PortBodyOrientationToCamera()
        {
            onlyCameraTransform = false;
            Vector3 direction = Vector3.zero;
            direction.y = m_CameraTransform.eulerAngles.y;
            m_CharacterBody.rotation = Quaternion.Euler(direction);

            direction = Vector3.zero;
            direction.x = m_CameraTransform.localEulerAngles.x;
            m_CameraTransform.localRotation = Quaternion.Euler(direction);
            m_CharacterTargetRot = m_CharacterBody.localRotation;
            m_CameraTargetRot = m_CameraTransform.localRotation;
        }

        /// <summary>
        /// 
        /// </summary>
        void CalculateSensitivity()
        {
            if (GunManager != null && GunManager.GetCurrentWeapon() != null)
            {
                if (!GunManager.GetCurrentWeapon().isAiming)
                {
                    XSensitivity = bl_RoomMenu.Instance.m_sensitive;
                    YSensitivity = bl_RoomMenu.Instance.m_sensitive;
                }
                else
                {
                    XSensitivity = bl_RoomMenu.Instance.SensitivityAim;
                    YSensitivity = bl_RoomMenu.Instance.SensitivityAim;
                }
            }
            InvertHorizontal = bl_RoomMenu.Instance.SetIMH;
            InvertVertical = bl_RoomMenu.Instance.SetIMV;
        }

#if MFPSM
        void CameraRotation(Transform character, Transform camera)
        {
            Vector2 input = bl_TouchPad.Instance.GetInput(XSensitivity);
            input.x = !InvertHorizontal ? input.x : (input.x * -1f);
            input.y = !InvertVertical ? (input.y * -1f) : input.y;

            m_CharacterTargetRot *= Quaternion.Euler(0f, input.x, 0f);
            m_CameraTargetRot *= Quaternion.Euler(input.y, 0f, 0f);

            if (clampVerticalRotation)
                m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);

            camera.localRotation = m_CameraTargetRot;
            character.localRotation = m_CharacterTargetRot;
        }
#endif

        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

            angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);

            q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }
    }

}