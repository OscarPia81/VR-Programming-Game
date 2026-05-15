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
    private Stack<While> loopStack = new Stack<While>();

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
            
            if (cur is While whileBlock)
            {
                if (whileBlock.Judger?.judge == true)
                {
                    loopStack.Push(whileBlock);
                }
                else
                {
                    cur = FindMatchingWhileEnd(whileBlock)?.next;
                    if (cur == null) break;
                    continue;
                }
            }

            if (cur is WhileEnd)
            {
                if (loopStack.Count == 0) break;

                While loopStart = loopStack.Peek();
                if (loopStart.Judger?.judge == true)
                {
                    loopStart.Judger.ResetState();
                    cur = loopStart.next;
                    continue;
                }
                else
                {
                    loopStack.Pop();
                    cur = cur.next;
                    continue;
                }
            }

            if (cur.next == null && loopStack.Count > 0)
            {
                While loopStart = loopStack.Peek();

                if (loopStart.Judger?.judge == true)
                {
                    if (loopStart.Judger != null)
                    {
                        loopStart.Judger.ResetState();
                    }
                    cur = loopStart.next;
                    continue;
                }
                else
                {
                    loopStack.Pop();
                    cur = FindMatchingWhileEnd(loopStart)?.next;
                    if (cur == null) break;
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

    private WhileEnd FindMatchingWhileEnd(While whileBlock)
    {
        int depth = 0;
        Code cur = whileBlock.next;

        while (cur != null)
        {
            if (cur is While) depth++;
            else if (cur is WhileEnd)
            {
                if (depth == 0) return (WhileEnd)cur;
                depth--;
            }
            cur = cur.next;
        }
        return null;
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