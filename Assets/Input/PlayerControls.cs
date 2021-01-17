// GENERATED AUTOMATICALLY FROM 'Assets/Input/PlayerControls.inputactions'

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public class @PlayerControls : IInputActionCollection, IDisposable
{
    public InputActionAsset asset { get; }
    public @PlayerControls()
    {
        asset = InputActionAsset.FromJson(@"{
    ""name"": ""PlayerControls"",
    ""maps"": [
        {
            ""name"": ""Standard"",
            ""id"": ""feae2fb5-4164-4e53-a292-ed75df380d96"",
            ""actions"": [
                {
                    ""name"": ""Move"",
                    ""type"": ""Value"",
                    ""id"": ""d0ae6597-3f4d-4611-8ff8-628335783cb6"",
                    ""expectedControlType"": ""Stick"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""RotateCamera"",
                    ""type"": ""Value"",
                    ""id"": ""02a1f773-76d6-4d16-8664-3ff057773b56"",
                    ""expectedControlType"": ""Stick"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Sneak"",
                    ""type"": ""Button"",
                    ""id"": ""7ce2cf4d-e44b-45a1-85ab-517fc34371d4"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                },
                {
                    ""name"": ""Interact"",
                    ""type"": ""Button"",
                    ""id"": ""183b6ddd-8aae-408f-842d-f4e91ad384b5"",
                    ""expectedControlType"": ""Button"",
                    ""processors"": """",
                    ""interactions"": """"
                }
            ],
            ""bindings"": [
                {
                    ""name"": """",
                    ""id"": ""383a55ce-f2f0-4285-bc70-52a2756e90ba"",
                    ""path"": ""<Gamepad>/leftStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""Move"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""d95fce42-3863-4cad-b6ed-276f6c20923f"",
                    ""path"": ""<Gamepad>/rightStick"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""RotateCamera"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""b5a227b9-c801-4653-a16b-c6d0a74a23db"",
                    ""path"": ""<Gamepad>/buttonEast"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""Sneak"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                },
                {
                    ""name"": """",
                    ""id"": ""3a33334f-3a9c-498a-8141-b456f1b4c4eb"",
                    ""path"": ""<Gamepad>/buttonWest"",
                    ""interactions"": """",
                    ""processors"": """",
                    ""groups"": ""Gamepad"",
                    ""action"": ""Interact"",
                    ""isComposite"": false,
                    ""isPartOfComposite"": false
                }
            ]
        }
    ],
    ""controlSchemes"": [
        {
            ""name"": ""Gamepad"",
            ""bindingGroup"": ""Gamepad"",
            ""devices"": [
                {
                    ""devicePath"": ""<Gamepad>"",
                    ""isOptional"": true,
                    ""isOR"": false
                }
            ]
        }
    ]
}");
        // Standard
        m_Standard = asset.FindActionMap("Standard", throwIfNotFound: true);
        m_Standard_Move = m_Standard.FindAction("Move", throwIfNotFound: true);
        m_Standard_RotateCamera = m_Standard.FindAction("RotateCamera", throwIfNotFound: true);
        m_Standard_Sneak = m_Standard.FindAction("Sneak", throwIfNotFound: true);
        m_Standard_Interact = m_Standard.FindAction("Interact", throwIfNotFound: true);
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(asset);
    }

    public InputBinding? bindingMask
    {
        get => asset.bindingMask;
        set => asset.bindingMask = value;
    }

    public ReadOnlyArray<InputDevice>? devices
    {
        get => asset.devices;
        set => asset.devices = value;
    }

    public ReadOnlyArray<InputControlScheme> controlSchemes => asset.controlSchemes;

    public bool Contains(InputAction action)
    {
        return asset.Contains(action);
    }

    public IEnumerator<InputAction> GetEnumerator()
    {
        return asset.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Enable()
    {
        asset.Enable();
    }

    public void Disable()
    {
        asset.Disable();
    }

    // Standard
    private readonly InputActionMap m_Standard;
    private IStandardActions m_StandardActionsCallbackInterface;
    private readonly InputAction m_Standard_Move;
    private readonly InputAction m_Standard_RotateCamera;
    private readonly InputAction m_Standard_Sneak;
    private readonly InputAction m_Standard_Interact;
    public struct StandardActions
    {
        private @PlayerControls m_Wrapper;
        public StandardActions(@PlayerControls wrapper) { m_Wrapper = wrapper; }
        public InputAction @Move => m_Wrapper.m_Standard_Move;
        public InputAction @RotateCamera => m_Wrapper.m_Standard_RotateCamera;
        public InputAction @Sneak => m_Wrapper.m_Standard_Sneak;
        public InputAction @Interact => m_Wrapper.m_Standard_Interact;
        public InputActionMap Get() { return m_Wrapper.m_Standard; }
        public void Enable() { Get().Enable(); }
        public void Disable() { Get().Disable(); }
        public bool enabled => Get().enabled;
        public static implicit operator InputActionMap(StandardActions set) { return set.Get(); }
        public void SetCallbacks(IStandardActions instance)
        {
            if (m_Wrapper.m_StandardActionsCallbackInterface != null)
            {
                @Move.started -= m_Wrapper.m_StandardActionsCallbackInterface.OnMove;
                @Move.performed -= m_Wrapper.m_StandardActionsCallbackInterface.OnMove;
                @Move.canceled -= m_Wrapper.m_StandardActionsCallbackInterface.OnMove;
                @RotateCamera.started -= m_Wrapper.m_StandardActionsCallbackInterface.OnRotateCamera;
                @RotateCamera.performed -= m_Wrapper.m_StandardActionsCallbackInterface.OnRotateCamera;
                @RotateCamera.canceled -= m_Wrapper.m_StandardActionsCallbackInterface.OnRotateCamera;
                @Sneak.started -= m_Wrapper.m_StandardActionsCallbackInterface.OnSneak;
                @Sneak.performed -= m_Wrapper.m_StandardActionsCallbackInterface.OnSneak;
                @Sneak.canceled -= m_Wrapper.m_StandardActionsCallbackInterface.OnSneak;
                @Interact.started -= m_Wrapper.m_StandardActionsCallbackInterface.OnInteract;
                @Interact.performed -= m_Wrapper.m_StandardActionsCallbackInterface.OnInteract;
                @Interact.canceled -= m_Wrapper.m_StandardActionsCallbackInterface.OnInteract;
            }
            m_Wrapper.m_StandardActionsCallbackInterface = instance;
            if (instance != null)
            {
                @Move.started += instance.OnMove;
                @Move.performed += instance.OnMove;
                @Move.canceled += instance.OnMove;
                @RotateCamera.started += instance.OnRotateCamera;
                @RotateCamera.performed += instance.OnRotateCamera;
                @RotateCamera.canceled += instance.OnRotateCamera;
                @Sneak.started += instance.OnSneak;
                @Sneak.performed += instance.OnSneak;
                @Sneak.canceled += instance.OnSneak;
                @Interact.started += instance.OnInteract;
                @Interact.performed += instance.OnInteract;
                @Interact.canceled += instance.OnInteract;
            }
        }
    }
    public StandardActions @Standard => new StandardActions(this);
    private int m_GamepadSchemeIndex = -1;
    public InputControlScheme GamepadScheme
    {
        get
        {
            if (m_GamepadSchemeIndex == -1) m_GamepadSchemeIndex = asset.FindControlSchemeIndex("Gamepad");
            return asset.controlSchemes[m_GamepadSchemeIndex];
        }
    }
    public interface IStandardActions
    {
        void OnMove(InputAction.CallbackContext context);
        void OnRotateCamera(InputAction.CallbackContext context);
        void OnSneak(InputAction.CallbackContext context);
        void OnInteract(InputAction.CallbackContext context);
    }
}
