using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CarControllerManager : MonoBehaviour
{
    [Header("List of Cars")]
    public List<Car_script> cars; // Gán trong Editor nếu dùng SelectCarByIndex
    private Car_script currentCar;

    [Header("Control Buttons")]
    public Button forwardButton, backwardButton, leftButton, rightButton, brakeButton, boostButton;

    private bool isForward, isBackward, isLeft, isRight, isBraking, isBoosting;
    private bool carReady = false;

    void Start()
    {
        // Đợi 1 frame để xe được Active xong
        StartCoroutine(WaitForCarSetup());
        SetupButtonEvents();
    }

    IEnumerator WaitForCarSetup()
    {
        yield return null; // chờ 1 frame

        GameObject playerCarGO = GameObject.FindGameObjectWithTag("Player");
        if (playerCarGO != null)
        {
            currentCar = playerCarGO.GetComponent<Car_script>();
            if (currentCar == null)
            {
                Debug.LogError("Xe được chọn không có Car_script!");
                yield break;
            }

            // Gán kiểu điều khiển phù hợp
            currentCar.control = Application.isMobilePlatform
                ? Car_script.ControlMode.Button
                : Car_script.ControlMode.Keyboard;

            carReady = true;
        }
    }

    void SetupButtonEvents()
    {
        AddEvent(forwardButton, () => isForward = true, () => isForward = false);
        AddEvent(backwardButton, () => isBackward = true, () => isBackward = false);
        AddEvent(leftButton, () => isLeft = true, () => isLeft = false);
        AddEvent(rightButton, () => isRight = true, () => isRight = false);
        AddEvent(brakeButton, () => isBraking = true, () => isBraking = false);
        AddEvent(boostButton, () => isBoosting = true, () => isBoosting = false);
    }

    void AddEvent(Button button, UnityEngine.Events.UnityAction onDown, UnityEngine.Events.UnityAction onUp)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entryDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entryDown.callback.AddListener((_) => onDown());
        trigger.triggers.Add(entryDown);

        EventTrigger.Entry entryUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        entryUp.callback.AddListener((_) => onUp());
        trigger.triggers.Add(entryUp);
    }

    void Update()
    {
        if (!carReady || currentCar == null) return;

        currentCar.SendMessage("SetButtonInputs", new CarInputData
        {
            verticall = isForward ? 1f : (isBackward ? -1f : 0f),
            horizontall = isLeft ? -1f : (isRight ? 1f : 0f),
            brake = isBraking,
            boost = isBoosting
        });
    }

    public void SelectCarByIndex(int index)
    {
        if (index >= 0 && index < cars.Count)
        {
            currentCar = cars[index];
            currentCar.control = Application.isMobilePlatform
                ? Car_script.ControlMode.Button
                : Car_script.ControlMode.Keyboard;
        }
    }
}
