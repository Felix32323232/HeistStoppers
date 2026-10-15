using Unity.Netcode;
using StarterAssets;
using UnityEngine;
using UnityEngine.InputSystem;
namespace NetcodeDemo
{
    public class ClientPlayerMove : NetworkBehaviour
    {
        [SerializeField]
        CharacterController m_CharacterController;
        [SerializeField]
        ThirdPersonController m_ThirdPersonController;
        [SerializeField]
        PlayerInput m_PlayerInput;
        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        private const string GameSceneName = "MainScene";

        private void Awake()
        {
            m_PlayerInput.enabled = false;
            m_ThirdPersonController.enabled = false;
            m_CharacterController.enabled = false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            enabled = IsClient; // Enable if this is a client.
            if (!IsOwner)
            {
                // Disable if this is not the owner
                SetControllers(false);
                return;
            }

            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent; //Suscribe to scene events

            ConfigureLocalCamera();

            // Enable if this is an owner
            SetControllers(true);
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnSceneEvent -= OnSceneEvent;
            }
            base.OnNetworkDespawn();
        }

        private void OnSceneEvent(SceneEvent sceneEvent)
        {
            // When local client finishes loading the scene
            if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                if (sceneEvent.SceneName == GameSceneName)
                {
                    enabled = IsClient; // Enable if this is a client.
                    if (!IsOwner)
                    {
                        // Disable if this is not the owner
                        SetControllers(false);
                        return;
                    }
                    ConfigureLocalCamera();
                    // Enable if this is an owner
                    SetControllers(true);
                }
            }
        }

        private void ConfigureLocalCamera()
        {
            m_ThirdPersonController.FindMainCamera();
            GameObject virtualCameraObject = GameObject.Find("PlayerFollowCamera");
            if (virtualCameraObject != null)
            {
                var vCam3 = virtualCameraObject.GetComponent<Unity.Cinemachine.CinemachineCamera>();
                if (vCam3 != null) vCam3.Follow = CinemachineCameraTarget.transform;
            }
        }

        private void SetControllers(bool enable)
        {
            if (!enable)
            {
                enabled = false;
                m_PlayerInput.enabled = false;
                m_CharacterController.enabled = false;
                m_ThirdPersonController.enabled = false;
            }
            else
            {
                m_PlayerInput.enabled = true;
                m_CharacterController.enabled = true;
                m_ThirdPersonController.enabled = true;
            }
        }
    }
}