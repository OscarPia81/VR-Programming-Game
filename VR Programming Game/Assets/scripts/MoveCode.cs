using UnityEngine;

public class MoveCode : Code
{
    public float speed = 2f;
    private bool isMoving = false;
    private float movedDistance = 0f;
    public Vector3 direction = Vector3.right;
    public float targetDistance = 10f;

    override public void work()
    {
        if (isMoving) return;
        isMoving = true;
        movedDistance = 0f;
        
        if (CodeManager.RobotAnimator != null)
        {
            CodeManager.RobotAnimator.SetBool("Walk_Anim", true);
        }
    }

    private void Update()
    {
        if (!isMoving) return;
        if (CodeManager.RobotTarget == null)
        {
            isMoving = false;
            Complete();
            return;
        }

        direction = direction.normalized;
        float step = speed * Time.deltaTime;
        step = Mathf.Min(step, targetDistance - movedDistance);
        CodeManager.RobotTarget.Translate(direction * step, Space.World);
        movedDistance += step;

        if (movedDistance >= targetDistance)
        {
            isMoving = false;

            if (CodeManager.RobotAnimator != null)
            {
                CodeManager.RobotAnimator.SetBool("Walk_Anim", false);
            }

            Complete();
        }
    }

    private void OnDestroy()
    {
        if (isMoving && CodeManager.RobotAnimator != null)
        {
            CodeManager.RobotAnimator.SetBool("Walk_Anim", false);
        }
    }
}
