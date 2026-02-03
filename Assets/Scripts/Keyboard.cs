using UnityEngine;
using TMPro;

public class Keyboard : MonoBehaviour
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard keyboard;
    private bool keyboardActive = false;

    void Awake()
    {
        inputField = GetComponent<TMP_InputField>();

        // Listen for selection (ray click / controller trigger)
        inputField.onSelect.AddListener(OnSelected);
        inputField.onDeselect.AddListener(OnDeselected);
    }

    void OnDestroy()
    {
        inputField.onSelect.RemoveListener(OnSelected);
        inputField.onDeselect.RemoveListener(OnDeselected);
    }

    private void OnSelected(string _)
    {
        Debug.Log("Input field selected, opening keyboard.");
        OpenKeyboard();
    }

    private void OnDeselected(string _)
    {
        Debug.Log("Input field deselected.");
        keyboardActive = false;
    }

    private void OpenKeyboard()
    {
        // For Meta Quest, we need to use TouchScreenKeyboard with specific settings
        keyboard = TouchScreenKeyboard.Open(
            inputField.text,
            TouchScreenKeyboardType.Default,
            false,  // autocorrection
            false,  // multiline
            false,  // secure (password)
            false,  // alert
            "",     // placeholder
            0       // characterLimit
        );
        keyboardActive = true;
    }

    void Update()
    {
        if (keyboardActive && keyboard != null)
        {
            // Update input field with keyboard text
            inputField.text = keyboard.text;

            // Check if keyboard is done or canceled
            if (keyboard.status == TouchScreenKeyboard.Status.Done || 
                keyboard.status == TouchScreenKeyboard.Status.Canceled)
            {
                keyboardActive = false;
                keyboard = null;
            }
        }
    }
}
