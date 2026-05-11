using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class CodeManager : MonoBehaviour
{
    public Transform First = null;
    public GameObject robot = null;

    public bool IsExecuting => playRoutine != null;

    private Coroutine playRoutine = null;
    private Stack<WhileCode> loopStack = new Stack<WhileCode>();

    private bool wasLeftTriggerPressed = false;

    public InputActionAsset inputActions;
    private InputAction leftTriggerAction;

    public static GameObject Robot { get; private set; }
    public static Animator RobotAnimator { get; private set; }
    public static Transform RobotTarget { get; private set; }

    private void Awake()
    {
        if (robot != null)
        {
            Robot = robot;
            RobotTarget = robot.transform;
            RobotAnimator = robot.GetComponent<Animator>();
        }

        if (inputActions != null)
        {
            leftTriggerAction = inputActions.FindAction("Activate");
            if (leftTriggerAction != null)
            {
                leftTriggerAction.Enable();
            }
        }
    }

    private void OnDestroy()
    {
        if (leftTriggerAction != null)
        {
            leftTriggerAction.Disable();
        }
    }

    private IEnumerator PlayCoroutine()
    {
        if (RobotAnimator != null)
        {
            RobotAnimator.SetBool("Open_Anim", true);
            yield return new WaitForSeconds(4.8f);
        }
        else
        {
            Debug.Log("[PlayCoroutine] RobotAnimator is null!");
        }

        Code cur = First?.GetComponent<Code>();
        
        while (cur != null)
        {
            bool completed = false;
            
            cur.OnComplete += () => completed = true;

            cur.SetHighlight(true);

            Debug.Log($"执行【{cur.GetType().Name}】");

            cur.work();

            yield return new WaitUntil(() => completed);

            cur.SetHighlight(false);

            cur.OnComplete -= () => completed = true;
            
            if (cur is WhileCode whileCode)
            {
                if (whileCode.Judger?.judge == true)
                {
                    loopStack.Push(whileCode);
                }
                else
                {
                    if (loopStack.Count > 0)
                    {
                        loopStack.Pop();
                    }
                }
            }
            
            if (cur.next == null && loopStack.Count > 0)
            {
                WhileCode loopStart = loopStack.Peek();
                
                if (loopStart.Judger?.judge == true)
                {
                    if (loopStart.Judger != null)
                    {
                        loopStart.Judger.ResetState();
                    }
                    cur = loopStart.LoopNext;
                    continue;
                }
                else
                {
                    loopStack.Pop();
                    cur = loopStart.next;
                    continue;
                }
            }
            
            cur = cur.next;
        }
        
        if (RobotAnimator != null)
        {
            RobotAnimator.SetBool("Walk_Anim", false);
            RobotAnimator.SetBool("Open_Anim", false);
        }

        playRoutine = null;
    }

    void Update()
    {
        CheckLeftTrigger();
    }

    private void CheckLeftTrigger()
    {
        if (leftTriggerAction == null)
        {
            if (inputActions != null)
            {
                leftTriggerAction = inputActions.FindAction("Activate");
                if (leftTriggerAction != null)
                {
                    leftTriggerAction.Enable();
                }
            }
            return;
        }
        
        bool pressed = leftTriggerAction.IsPressed();

        if (pressed && !wasLeftTriggerPressed)
        {
            ToggleCodeExecution();
        }

        wasLeftTriggerPressed = pressed;
    }

    private void ToggleCodeExecution()
    {
        if (playRoutine == null)
        {
            ResetAllBlocks();
            playRoutine = StartCoroutine(PlayCoroutine());
        }
        else
        {
            StopAllCoroutines();
            playRoutine = null;
            if (RobotAnimator != null)
            {
                RobotAnimator.SetBool("Walk_Anim", false);
                RobotAnimator.SetBool("Open_Anim", false);
            }
            ResetAllBlocks();
        }
    }

    private void ResetAllBlocks()
    {
        if (First == null) return;
        
        Code cur = First.GetComponent<Code>();
        while (cur != null)
        {
            cur.ResetState();
            cur = cur.next;
        }
    }
}