using UnityEngine;

public class StarFrontCode : BoolCode
{
    public override void work()
    {
        judge = CheckStarInDirection(CodeManager.RobotTarget.forward);
        Complete();
    }

    private static bool CheckStarInDirection(Vector3 dir)
    {
        Vector3 checkPos = CodeManager.RobotTarget.position + dir * 1f;
        foreach (var star in Object.FindObjectsOfType<Star>())
        {
            if (!star.collected && star.orderIndex == LevelManager.Instance.nextStarIndex)
            {
                float dist = Vector3.Distance(star.transform.position, checkPos);
                if (dist < 0.4f) return true;
            }
        }
        return false;
    }

    public static bool CheckStar(Vector3 dir)
    {
        Vector3 checkPos = CodeManager.RobotTarget.position + dir * 1f;
        foreach (var star in Object.FindObjectsOfType<Star>())
        {
            if (!star.collected && star.orderIndex == LevelManager.Instance.nextStarIndex)
            {
                float dist = Vector3.Distance(star.transform.position, checkPos);
                if (dist < 0.4f) return true;
            }
        }
        return false;
    }
}
